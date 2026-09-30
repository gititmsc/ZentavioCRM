/*
    001_CreatePlatformDatabase.sql
    ZentavioCRM — Platform (master) database.

    This is the ONE shared database for the whole SaaS deployment — the tenant registry.
    It is NEVER used for application data (no Users, Leads, Customers, etc. live here).

    Run this against a database named in ConnectionStrings:PlatformDb (appsettings.json),
    e.g. "ZentavioCRM_Platform". Create that database first if it doesn't already exist —
    unlike the tenant databases (which the app creates for you via the provisioning API),
    the Platform database itself is a one-time manual setup step.

    Relationship to the "Tenant" folder scripts (SQL Changes/Tenant/):
      - Tenant/001_CreateSchema.sql / Tenant/002_SeedData.sql describe a TENANT database (one per
        customer company) — Companies, Users, Leads, Customers, etc.
      - This script describes the PLATFORM database — just the Tenants table below, which
        records which tenant databases exist and how to reach them.
      - New tenants are no longer expected to be created by manually re-running the Tenant/
        scripts by hand; POST /api/platform/tenants (TenantProvisioningService) does that
        automatically: it creates a new tenant database, applies the same schema + RBAC seed,
        creates the tenant's Company and first Admin user, and inserts the corresponding row
        here. The Tenant/ scripts remain useful as a human-readable reference and for manually
        building a one-off local dev tenant database.

    Folder layout: this "Tenant Admin" folder holds scripts that target the single shared
    Platform database. "Tenant" (sibling folder) holds scripts that target an individual
    tenant's own database. Each folder is numbered independently in safe, guarded run order —
    running every numbered script in a folder in order, on any database in any state that
    folder's scripts apply to, should never error.
*/

-- Run these two lines against the actual SQL Server "master" system database if the Platform
-- database itself doesn't exist yet. Adjust the name if you configured a different one.
-- IF DB_ID(N'ZentavioCRM_Platform') IS NULL
-- BEGIN
--     CREATE DATABASE [ZentavioCRM_Platform];
-- END
-- GO

USE [ZentavioCRM_Platform];
GO

IF OBJECT_ID(N'dbo.Tenants', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tenants
    (
        Id             UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Tenants_Id DEFAULT NEWID(),
        Name           NVARCHAR(200)    NOT NULL,
        -- Resolves "<Subdomain>.<RootDomain>" (e.g. acme.zentaviocrm.com) to this tenant.
        Subdomain      NVARCHAR(63)     NOT NULL,
        -- Physical database name, e.g. "ZentavioCRM_Tenant_acme". Combined at request time with
        -- Tenancy:SqlServerHostConnectionString — the full connection string is never persisted,
        -- so rotating SQL Server credentials doesn't require touching this table.
        DatabaseName   NVARCHAR(128)    NOT NULL,
        -- Provisioning | Active | Suspended | Failed | Terminated — see ZentavioCRM.Core.Enums.TenantStatus.
        -- Suspended and Terminated are both reversible lock-outs (see PATCH .../reactivate);
        -- neither one touches or drops the tenant's own database.
        Status         NVARCHAR(30)     NOT NULL,
        -- Denormalized for the platform admin list; the tenant's own Users table is the source of truth.
        AdminEmail     NVARCHAR(256)    NOT NULL,
        CreatedAtUtc   DATETIME2        NOT NULL,
        ActivatedAtUtc DATETIME2        NULL,
        CONSTRAINT PK_Tenants PRIMARY KEY CLUSTERED (Id)
    );

    CREATE UNIQUE INDEX IX_Tenants_Subdomain ON dbo.Tenants (Subdomain);
    CREATE UNIQUE INDEX IX_Tenants_DatabaseName ON dbo.Tenants (DatabaseName);
END
GO

-- Plan tier + usage limits — added after Tenants already existed in some deployments, so these
-- are guarded per-column rather than assumed to come in with the CREATE TABLE above. Defaults
-- match ZentavioCRM.Core.Configuration.PlanTierDefaults.For(PlanTier.Trial) so existing tenants
-- land on sensible values instead of zero limits.
IF COL_LENGTH(N'dbo.Tenants', N'PlanTier') IS NULL
BEGIN
    ALTER TABLE dbo.Tenants ADD PlanTier NVARCHAR(30) NOT NULL CONSTRAINT DF_Tenants_PlanTier DEFAULT (N'Trial');
END
GO

IF COL_LENGTH(N'dbo.Tenants', N'MaxUsers') IS NULL
BEGIN
    ALTER TABLE dbo.Tenants ADD MaxUsers INT NOT NULL CONSTRAINT DF_Tenants_MaxUsers DEFAULT (3);
END
GO

IF COL_LENGTH(N'dbo.Tenants', N'MaxStorageMB') IS NULL
BEGIN
    ALTER TABLE dbo.Tenants ADD MaxStorageMB INT NOT NULL CONSTRAINT DF_Tenants_MaxStorageMB DEFAULT (500);
END
GO

IF COL_LENGTH(N'dbo.Tenants', N'MaxRecords') IS NULL
BEGIN
    ALTER TABLE dbo.Tenants ADD MaxRecords INT NOT NULL CONSTRAINT DF_Tenants_MaxRecords DEFAULT (250);
END
GO

-- ============================================================================
-- PlatformAdmins — who can log into the Super Admin panel (provision/suspend/reactivate
-- tenants, manage other platform admins). Entirely separate from any tenant's own Users table:
-- a platform admin has no tenant, and a tenant's Admin role has no platform access.
-- See ZentavioCRM.Core.Entities.Platform.PlatformAdmin / PlatformAdminConfiguration.
-- ============================================================================
IF OBJECT_ID(N'dbo.PlatformAdmins', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PlatformAdmins
    (
        Id             UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_PlatformAdmins_Id DEFAULT NEWID(),
        Email          NVARCHAR(256)    NOT NULL,
        PasswordHash   NVARCHAR(512)    NOT NULL,
        FirstName      NVARCHAR(100)    NOT NULL,
        LastName       NVARCHAR(100)    NOT NULL,
        IsActive       BIT              NOT NULL CONSTRAINT DF_PlatformAdmins_IsActive DEFAULT 1,
        CreatedAtUtc   DATETIME2        NOT NULL,
        LastLoginAtUtc DATETIME2        NULL,
        CONSTRAINT PK_PlatformAdmins PRIMARY KEY CLUSTERED (Id)
    );

    CREATE UNIQUE INDEX IX_PlatformAdmins_Email ON dbo.PlatformAdmins (Email);
END
GO

-- ============================================================================
-- PlatformAuditLogs — history of platform-level actions (admin logins, tenant provisioned/
-- suspended/reactivated/stopped, and future actions like impersonation). Separate from a
-- tenant's own AuditLogs table (which lives in each tenant database, not here).
-- See ZentavioCRM.Core.Entities.Platform.PlatformAuditLog / PlatformAuditLogConfiguration.
-- ============================================================================
IF OBJECT_ID(N'dbo.PlatformAuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PlatformAuditLogs
    (
        Id               UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_PlatformAuditLogs_Id DEFAULT NEWID(),
        -- Null if the admin account was later deleted, or for system-initiated entries.
        PlatformAdminId  UNIQUEIDENTIFIER NULL,
        -- "Login", "TenantProvisioned", "TenantSuspended", "TenantReactivated", "TenantStopped".
        Action           NVARCHAR(30)     NOT NULL,
        -- Set for tenant-scoped actions; null for account-level actions like Login.
        TenantId         UNIQUEIDENTIFIER NULL,
        Summary          NVARCHAR(1000)   NOT NULL,
        CreatedAtUtc     DATETIME2        NOT NULL,
        CONSTRAINT PK_PlatformAuditLogs PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_PlatformAuditLogs_PlatformAdmin FOREIGN KEY (PlatformAdminId)
            REFERENCES dbo.PlatformAdmins (Id) ON DELETE SET NULL,
        CONSTRAINT FK_PlatformAuditLogs_Tenant FOREIGN KEY (TenantId)
            REFERENCES dbo.Tenants (Id) ON DELETE SET NULL
    );

    CREATE INDEX IX_PlatformAuditLogs_TenantId ON dbo.PlatformAuditLogs (TenantId);
    CREATE INDEX IX_PlatformAuditLogs_CreatedAtUtc ON dbo.PlatformAuditLogs (CreatedAtUtc);
END
GO

-- ============================================================================
-- Billing (manual, no payment gateway) — Unpaid/Paid/Overdue status plus optional
-- amount/currency/cycle/next-due-date, all set directly by a platform admin. Marking a tenant
-- Overdue automatically suspends it (see ITenantBillingService.UpdateBillingAsync); nothing here
-- ever talks to a real payment processor.
-- ============================================================================
IF COL_LENGTH(N'dbo.Tenants', N'PaymentStatus') IS NULL
BEGIN
    ALTER TABLE dbo.Tenants ADD PaymentStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_Tenants_PaymentStatus DEFAULT (N'Unpaid');
END
GO

IF COL_LENGTH(N'dbo.Tenants', N'BillingAmount') IS NULL
BEGIN
    ALTER TABLE dbo.Tenants ADD BillingAmount DECIMAL(12,2) NULL;
END
GO

IF COL_LENGTH(N'dbo.Tenants', N'BillingCurrency') IS NULL
BEGIN
    ALTER TABLE dbo.Tenants ADD BillingCurrency NVARCHAR(3) NULL;
END
GO

IF COL_LENGTH(N'dbo.Tenants', N'BillingCycle') IS NULL
BEGIN
    ALTER TABLE dbo.Tenants ADD BillingCycle NVARCHAR(20) NULL;
END
GO

IF COL_LENGTH(N'dbo.Tenants', N'NextDueDateUtc') IS NULL
BEGIN
    ALTER TABLE dbo.Tenants ADD NextDueDateUtc DATETIME2 NULL;
END
GO

-- ============================================================================
-- TenantPayments — append-only ledger of individual payment entries recorded by a platform
-- admin. Recording one of these sets the parent Tenant's PaymentStatus to Paid and advances its
-- NextDueDateUtc by one BillingCycle. See ZentavioCRM.Core.Entities.Platform.TenantPayment.
-- ============================================================================
IF OBJECT_ID(N'dbo.TenantPayments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantPayments
    (
        Id                 UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_TenantPayments_Id DEFAULT NEWID(),
        TenantId           UNIQUEIDENTIFIER NOT NULL,
        Amount             DECIMAL(12,2)    NOT NULL,
        Currency           NVARCHAR(3)      NOT NULL,
        PaidAtUtc          DATETIME2        NOT NULL,
        Note               NVARCHAR(1000)   NULL,
        -- Null if the admin account was later deleted.
        RecordedByAdminId  UNIQUEIDENTIFIER NULL,
        CreatedAtUtc       DATETIME2        NOT NULL,
        CONSTRAINT PK_TenantPayments PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_TenantPayments_Tenant FOREIGN KEY (TenantId)
            REFERENCES dbo.Tenants (Id) ON DELETE CASCADE,
        CONSTRAINT FK_TenantPayments_RecordedByAdmin FOREIGN KEY (RecordedByAdminId)
            REFERENCES dbo.PlatformAdmins (Id) ON DELETE SET NULL
    );

    CREATE INDEX IX_TenantPayments_TenantId ON dbo.TenantPayments (TenantId);
    CREATE INDEX IX_TenantPayments_PaidAtUtc ON dbo.TenantPayments (PaidAtUtc);
END
GO

-- ============================================================================
-- TenantNotes — append-only free-text notes platform admins leave for each other on a tenant.
-- Separate from PlatformAuditLogs (which records actions, not commentary).
-- See ZentavioCRM.Core.Entities.Platform.TenantNote.
-- ============================================================================
IF OBJECT_ID(N'dbo.TenantNotes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TenantNotes
    (
        Id                UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_TenantNotes_Id DEFAULT NEWID(),
        TenantId          UNIQUEIDENTIFIER NOT NULL,
        Note              NVARCHAR(2000)   NOT NULL,
        -- Null if the admin account was later deleted.
        CreatedByAdminId  UNIQUEIDENTIFIER NULL,
        CreatedAtUtc      DATETIME2        NOT NULL,
        CONSTRAINT PK_TenantNotes PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_TenantNotes_Tenant FOREIGN KEY (TenantId)
            REFERENCES dbo.Tenants (Id) ON DELETE CASCADE,
        CONSTRAINT FK_TenantNotes_CreatedByAdmin FOREIGN KEY (CreatedByAdminId)
            REFERENCES dbo.PlatformAdmins (Id) ON DELETE SET NULL
    );

    CREATE INDEX IX_TenantNotes_TenantId ON dbo.TenantNotes (TenantId);
    CREATE INDEX IX_TenantNotes_CreatedAtUtc ON dbo.TenantNotes (CreatedAtUtc);
END
GO

-- ============================================================================
-- Platform admin roles + login lockout — SuperAdmin (full access) vs Support (read-only); every
-- admin created before this existed defaults to SuperAdmin so nobody is silently locked out.
-- FailedLoginAttempts/LockedUntilUtc back a simple lockout after repeated bad passwords.
-- See ZentavioCRM.Core.Entities.Platform.PlatformAdmin / PlatformAdminRole.
-- ============================================================================
IF COL_LENGTH(N'dbo.PlatformAdmins', N'Role') IS NULL
BEGIN
    ALTER TABLE dbo.PlatformAdmins ADD Role NVARCHAR(20) NOT NULL CONSTRAINT DF_PlatformAdmins_Role DEFAULT (N'SuperAdmin');
END
GO

IF COL_LENGTH(N'dbo.PlatformAdmins', N'FailedLoginAttempts') IS NULL
BEGIN
    ALTER TABLE dbo.PlatformAdmins ADD FailedLoginAttempts INT NOT NULL CONSTRAINT DF_PlatformAdmins_FailedLoginAttempts DEFAULT (0);
END
GO

IF COL_LENGTH(N'dbo.PlatformAdmins', N'LockedUntilUtc') IS NULL
BEGIN
    ALTER TABLE dbo.PlatformAdmins ADD LockedUntilUtc DATETIME2 NULL;
END
GO

-- ============================================================================
-- Trial expiration — set at provision time for Trial-tier tenants; TenantResolutionMiddleware
-- reactively suspends an Active Trial tenant once this passes (no background job infrastructure
-- exists in this app, so this mirrors the existing Overdue auto-suspend pattern instead of a
-- scheduled sweep). Null for non-Trial tenants and for Trial tenants provisioned before this
-- existed, both of which are treated as "never expires".
-- ============================================================================
IF COL_LENGTH(N'dbo.Tenants', N'TrialEndsAtUtc') IS NULL
BEGIN
    ALTER TABLE dbo.Tenants ADD TrialEndsAtUtc DATETIME2 NULL;
END
GO
