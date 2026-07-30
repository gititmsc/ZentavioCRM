/*
    001_CreateSchema.sql
    ZentavioCRM — Foundation + Leads milestone. This is a TENANT database schema (one per
    customer company) — see 003_CreatePlatformDatabase.sql for the separate shared Platform
    (master) database that just tracks which tenant databases exist.

    Creates every table added for the Company/Department/Role/Permission/User foundation,
    the Customer master (with Contacts and Addresses), the Lead pipeline, the Opportunity
    pipeline (SRS Phase 6, section 4 "Opportunity Management"), and the generic Activity
    timeline. Column types, lengths, defaults, and keys mirror the EF Core configurations in
    StaffingManagementSystem.Infrastructure/Persistence/Configurations exactly, so this script
    and the C# model stay in lockstep.

    Safe to re-run: every CREATE TABLE is guarded with an existence check.
    Run this before 002_SeedData.sql.

    Target database: whatever "ConnectionStrings:StaffingManagementSystemDb" in
    appsettings.json points to. Adjust the USE statement below if your local DB name differs.

    New tenants no longer need this run by hand: POST /api/platform/tenants
    (TenantProvisioningService) creates a fresh tenant database and applies this same schema
    automatically, from an embedded copy at
    StaffingManagementSystem.Infrastructure/Persistence/Scripts/TenantSchema.sql.
    Keep this file and that embedded copy in sync when the schema changes — this file is the
    human-readable reference and the one to use for manually building a one-off dev database;
    the embedded copy is what actually runs in code.
*/

USE [StaffingManagementSystemDb];
GO

-- ============================================================================
-- Companies
-- ============================================================================
IF OBJECT_ID(N'dbo.Companies', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Companies
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Companies_Id DEFAULT NEWID(),
        Name            NVARCHAR(200)    NOT NULL,
        LegalName       NVARCHAR(200)    NULL,
        Industry        NVARCHAR(100)    NULL,
        Website         NVARCHAR(300)    NULL,
        Email           NVARCHAR(256)    NULL,
        Phone           NVARCHAR(30)     NULL,
        TaxNumber       NVARCHAR(50)     NULL,
        AddressLine1    NVARCHAR(200)    NULL,
        AddressLine2    NVARCHAR(200)    NULL,
        City            NVARCHAR(100)    NULL,
        State           NVARCHAR(100)    NULL,
        Country         NVARCHAR(100)    NULL,
        PostalCode      NVARCHAR(20)     NULL,
        DefaultCurrency NVARCHAR(10)     NOT NULL,
        TimeZone        NVARCHAR(100)    NOT NULL,
        LogoUrl         NVARCHAR(500)    NULL,
        CreatedAtUtc    DATETIME2        NOT NULL,
        UpdatedAtUtc    DATETIME2        NULL,
        CONSTRAINT PK_Companies PRIMARY KEY CLUSTERED (Id)
    );
END
GO

-- ============================================================================
-- Permissions
-- ============================================================================
IF OBJECT_ID(N'dbo.Permissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Permissions
    (
        Id     UNIQUEIDENTIFIER NOT NULL,
        Code   NVARCHAR(100)    NOT NULL,
        Name   NVARCHAR(150)    NOT NULL,
        Module NVARCHAR(100)    NOT NULL,
        CONSTRAINT PK_Permissions PRIMARY KEY CLUSTERED (Id)
    );

    CREATE UNIQUE INDEX IX_Permissions_Code ON dbo.Permissions (Code);
END
GO

-- ============================================================================
-- Roles
-- ============================================================================
IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        Id           UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Roles_Id DEFAULT NEWID(),
        Name         NVARCHAR(100)    NOT NULL,
        Description  NVARCHAR(500)    NULL,
        IsSystemRole BIT              NOT NULL,
        CreatedAtUtc DATETIME2        NOT NULL,
        CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (Id)
    );

    CREATE UNIQUE INDEX IX_Roles_Name ON dbo.Roles (Name);
END
GO

-- Roles.VisibilityScope — Own/Team/All record-level visibility for Leads/Customers/Opportunities.
-- Defaults to 'All' so every pre-existing role keeps today's unrestricted behavior until an admin narrows it.
IF OBJECT_ID(N'dbo.Roles', N'U') IS NOT NULL AND COL_LENGTH('dbo.Roles', 'VisibilityScope') IS NULL
BEGIN
    ALTER TABLE dbo.Roles ADD VisibilityScope NVARCHAR(20) NOT NULL CONSTRAINT DF_Roles_VisibilityScope DEFAULT ('All');
END
GO

-- ============================================================================
-- Departments (self-referencing hierarchy under Companies)
-- ============================================================================
IF OBJECT_ID(N'dbo.Departments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Departments
    (
        Id                 UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Departments_Id DEFAULT NEWID(),
        CompanyId          UNIQUEIDENTIFIER NOT NULL,
        Name               NVARCHAR(150)    NOT NULL,
        ParentDepartmentId UNIQUEIDENTIFIER NULL,
        IsActive           BIT              NOT NULL CONSTRAINT DF_Departments_IsActive DEFAULT (1),
        CreatedAtUtc       DATETIME2        NOT NULL,
        UpdatedAtUtc       DATETIME2        NULL,
        CONSTRAINT PK_Departments PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Departments_Companies FOREIGN KEY (CompanyId) REFERENCES dbo.Companies (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Departments_ParentDepartment FOREIGN KEY (ParentDepartmentId) REFERENCES dbo.Departments (Id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_Departments_CompanyId_Name ON dbo.Departments (CompanyId, Name);
    CREATE INDEX IX_Departments_ParentDepartmentId ON dbo.Departments (ParentDepartmentId);
END
GO

-- ============================================================================
-- Territories (self-referencing hierarchy — structured sales/service territory,
-- superseding the free-text label on Leads.Territory going forward; same pattern as Departments above)
-- ============================================================================
IF OBJECT_ID(N'dbo.Territories', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Territories
    (
        Id                UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Territories_Id DEFAULT NEWID(),
        Name              NVARCHAR(150)    NOT NULL,
        ParentTerritoryId UNIQUEIDENTIFIER NULL,
        IsActive          BIT              NOT NULL CONSTRAINT DF_Territories_IsActive DEFAULT (1),
        CreatedAtUtc      DATETIME2        NOT NULL,
        UpdatedAtUtc      DATETIME2        NULL,
        CONSTRAINT PK_Territories PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Territories_ParentTerritory FOREIGN KEY (ParentTerritoryId) REFERENCES dbo.Territories (Id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_Territories_Name ON dbo.Territories (Name);
    CREATE INDEX IX_Territories_ParentTerritoryId ON dbo.Territories (ParentTerritoryId);
END
GO

-- ============================================================================
-- Users
-- ============================================================================
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Users_Id DEFAULT NEWID(),
        EmployeeCode        NVARCHAR(50)     NOT NULL,
        FirstName           NVARCHAR(100)    NOT NULL,
        LastName            NVARCHAR(100)    NOT NULL,
        Email               NVARCHAR(256)    NOT NULL,
        Mobile              NVARCHAR(30)     NULL,
        PasswordHash        NVARCHAR(512)    NOT NULL,
        RoleId              UNIQUEIDENTIFIER NOT NULL,
        DepartmentId        UNIQUEIDENTIFIER NULL,
        ReportingManagerId  UNIQUEIDENTIFIER NULL,
        TerritoryId         UNIQUEIDENTIFIER NULL,
        ProfilePhotoContent VARBINARY(MAX)   NULL,
        ProfilePhotoContentType NVARCHAR(100) NULL,
        IsActive            BIT              NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        CreatedAtUtc        DATETIME2        NOT NULL,
        UpdatedAtUtc        DATETIME2        NULL,
        LastLoginAtUtc      DATETIME2        NULL,
        CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Users_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Users_ReportingManager FOREIGN KEY (ReportingManagerId) REFERENCES dbo.Users (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Users_Territories FOREIGN KEY (TerritoryId) REFERENCES dbo.Territories (Id) ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX IX_Users_EmployeeCode ON dbo.Users (EmployeeCode);
    CREATE UNIQUE INDEX IX_Users_Email ON dbo.Users (Email);
    CREATE INDEX IX_Users_RoleId ON dbo.Users (RoleId);
    CREATE INDEX IX_Users_DepartmentId ON dbo.Users (DepartmentId);
    CREATE INDEX IX_Users_ReportingManagerId ON dbo.Users (ReportingManagerId);
    CREATE INDEX IX_Users_TerritoryId ON dbo.Users (TerritoryId);
END
GO

-- Users.TerritoryId — added post-launch; guarded ALTER for a Users table that already existed
-- from an earlier run of this script (fresh installs get the column via the CREATE TABLE above).
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL AND COL_LENGTH('dbo.Users', 'TerritoryId') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD TerritoryId UNIQUEIDENTIFIER NULL;
END
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.Users', 'TerritoryId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Users_Territories')
BEGIN
    ALTER TABLE dbo.Users ADD CONSTRAINT FK_Users_Territories FOREIGN KEY (TerritoryId) REFERENCES dbo.Territories (Id) ON DELETE NO ACTION;
END
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Users_TerritoryId' AND object_id = OBJECT_ID(N'dbo.Users'))
BEGIN
    CREATE INDEX IX_Users_TerritoryId ON dbo.Users (TerritoryId);
END
GO

-- Users.ProfilePhotoContent / ProfilePhotoContentType — avatar image, stored directly on Users
-- (not via the generic Documents table) to avoid an extra lookup per avatar when rendering lists.
IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL AND COL_LENGTH('dbo.Users', 'ProfilePhotoContent') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD ProfilePhotoContent VARBINARY(MAX) NULL;
END
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL AND COL_LENGTH('dbo.Users', 'ProfilePhotoContentType') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD ProfilePhotoContentType NVARCHAR(100) NULL;
END
GO

-- ============================================================================
-- RolePermissions (join table — Role <-> Permission)
-- ============================================================================
IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolePermissions
    (
        RoleId       UNIQUEIDENTIFIER NOT NULL,
        PermissionId UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_RolePermissions PRIMARY KEY CLUSTERED (RoleId, PermissionId),
        CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id) ON DELETE CASCADE,
        CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES dbo.Permissions (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_RolePermissions_PermissionId ON dbo.RolePermissions (PermissionId);
END
GO

-- ============================================================================
-- Customers (Customer Master)
-- ============================================================================
IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        Id               UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Customers_Id DEFAULT NEWID(),
        CustomerNumber   NVARCHAR(30)     NOT NULL,
        Type             NVARCHAR(30)     NOT NULL,
        LegalName        NVARCHAR(200)    NOT NULL,
        DisplayName      NVARCHAR(200)    NOT NULL,
        Industry         NVARCHAR(100)    NULL,
        Website          NVARCHAR(300)    NULL,
        Email            NVARCHAR(256)    NULL,
        Phone            NVARCHAR(30)     NULL,
        TaxNumber        NVARCHAR(50)     NULL,
        EmployeesCount   INT              NULL,
        AnnualRevenue    DECIMAL(18, 2)   NULL,
        CurrencyCode     NVARCHAR(10)     NOT NULL,
        PaymentTermsDays INT              NULL,
        CreditLimit      DECIMAL(18, 2)   NULL,
        Rating           NVARCHAR(20)     NULL,
        Tags             NVARCHAR(500)    NULL,
        AcquisitionSource NVARCHAR(30)    NULL,
        HealthStatus     NVARCHAR(20)     NULL,
        AssignedToUserId UNIQUEIDENTIFIER NULL,
        IsActive         BIT              NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT (1),
        CreatedAtUtc     DATETIME2        NOT NULL,
        UpdatedAtUtc     DATETIME2        NULL,
        CONSTRAINT PK_Customers PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Customers_AssignedToUser FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users (Id) ON DELETE SET NULL
    );

    CREATE UNIQUE INDEX IX_Customers_CustomerNumber ON dbo.Customers (CustomerNumber);
    CREATE INDEX IX_Customers_DisplayName ON dbo.Customers (DisplayName);
    CREATE INDEX IX_Customers_AssignedToUserId ON dbo.Customers (AssignedToUserId);
END
GO

-- Customers.Tags / AcquisitionSource — added post-launch; guarded ALTER for a Customers table
-- that already existed from an earlier run of this script, same pattern as Opportunities.NextStep above.
IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL AND COL_LENGTH('dbo.Customers', 'Tags') IS NULL
BEGIN
    ALTER TABLE dbo.Customers ADD Tags NVARCHAR(500) NULL;
END
GO

IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL AND COL_LENGTH('dbo.Customers', 'AcquisitionSource') IS NULL
BEGIN
    ALTER TABLE dbo.Customers ADD AcquisitionSource NVARCHAR(30) NULL;
END
GO

-- Customers.HealthStatus — manually-set relationship health/engagement indicator (Hot/Warm/Cold/AtRisk).
IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL AND COL_LENGTH('dbo.Customers', 'HealthStatus') IS NULL
BEGIN
    ALTER TABLE dbo.Customers ADD HealthStatus NVARCHAR(20) NULL;
END
GO

-- Customers.CreatedByUserId — audit-trail scalar only (no FK), same convention as Lead/Opportunity's CreatedByUserId.
-- Used by the record-visibility (Own/Team/All) feature so newly-created, not-yet-assigned customers remain visible to their creator.
IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL AND COL_LENGTH('dbo.Customers', 'CreatedByUserId') IS NULL
BEGIN
    ALTER TABLE dbo.Customers ADD CreatedByUserId UNIQUEIDENTIFIER NULL;
END
GO

-- ============================================================================
-- ContactPersons (unlimited contacts per Customer)
-- ============================================================================
IF OBJECT_ID(N'dbo.ContactPersons', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContactPersons
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ContactPersons_Id DEFAULT NEWID(),
        CustomerId      UNIQUEIDENTIFIER NOT NULL,
        FirstName       NVARCHAR(100)    NOT NULL,
        LastName        NVARCHAR(100)    NOT NULL,
        Designation     NVARCHAR(100)    NULL,
        Department      NVARCHAR(100)    NULL,
        Email           NVARCHAR(256)    NULL,
        Mobile          NVARCHAR(30)     NULL,
        WhatsApp        NVARCHAR(30)     NULL,
        LinkedIn        NVARCHAR(300)    NULL,
        IsPrimary       BIT              NOT NULL,
        IsDecisionMaker BIT              NOT NULL,
        PreferredContactMethod      NVARCHAR(20) NULL,
        DateOfBirth                 DATETIME2 NULL,
        AnniversaryDate             DATETIME2 NULL,
        BirthdayReminderSentYear    INT       NULL,
        AnniversaryReminderSentYear INT       NULL,
        Notes           NVARCHAR(MAX)    NULL,
        CreatedAtUtc    DATETIME2        NOT NULL,
        CONSTRAINT PK_ContactPersons PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ContactPersons_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_ContactPersons_CustomerId ON dbo.ContactPersons (CustomerId);
END
GO

-- ContactPersons.PreferredContactMethod / DateOfBirth / AnniversaryDate / *ReminderSentYear — added
-- post-launch; guarded ALTERs for a ContactPersons table that already existed from an earlier run.
IF OBJECT_ID(N'dbo.ContactPersons', N'U') IS NOT NULL AND COL_LENGTH('dbo.ContactPersons', 'PreferredContactMethod') IS NULL
BEGIN
    ALTER TABLE dbo.ContactPersons ADD PreferredContactMethod NVARCHAR(20) NULL;
END
GO

IF OBJECT_ID(N'dbo.ContactPersons', N'U') IS NOT NULL AND COL_LENGTH('dbo.ContactPersons', 'DateOfBirth') IS NULL
BEGIN
    ALTER TABLE dbo.ContactPersons ADD DateOfBirth DATETIME2 NULL;
END
GO

IF OBJECT_ID(N'dbo.ContactPersons', N'U') IS NOT NULL AND COL_LENGTH('dbo.ContactPersons', 'AnniversaryDate') IS NULL
BEGIN
    ALTER TABLE dbo.ContactPersons ADD AnniversaryDate DATETIME2 NULL;
END
GO

IF OBJECT_ID(N'dbo.ContactPersons', N'U') IS NOT NULL AND COL_LENGTH('dbo.ContactPersons', 'BirthdayReminderSentYear') IS NULL
BEGIN
    ALTER TABLE dbo.ContactPersons ADD BirthdayReminderSentYear INT NULL;
END
GO

IF OBJECT_ID(N'dbo.ContactPersons', N'U') IS NOT NULL AND COL_LENGTH('dbo.ContactPersons', 'AnniversaryReminderSentYear') IS NULL
BEGIN
    ALTER TABLE dbo.ContactPersons ADD AnniversaryReminderSentYear INT NULL;
END
GO

-- ============================================================================
-- CustomerAddresses (multiple addresses per Customer)
-- ============================================================================
IF OBJECT_ID(N'dbo.CustomerAddresses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerAddresses
    (
        Id         UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_CustomerAddresses_Id DEFAULT NEWID(),
        CustomerId UNIQUEIDENTIFIER NOT NULL,
        Type       NVARCHAR(30)     NOT NULL,
        Line1      NVARCHAR(200)    NOT NULL,
        Line2      NVARCHAR(200)    NULL,
        City       NVARCHAR(100)    NULL,
        State      NVARCHAR(100)    NULL,
        Country    NVARCHAR(100)    NULL,
        PostalCode NVARCHAR(20)     NULL,
        IsPrimary  BIT              NOT NULL,
        CONSTRAINT PK_CustomerAddresses PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_CustomerAddresses_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_CustomerAddresses_CustomerId ON dbo.CustomerAddresses (CustomerId);
END
GO

-- ============================================================================
-- Leads
-- ============================================================================
IF OBJECT_ID(N'dbo.Leads', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Leads
    (
        Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Leads_Id DEFAULT NEWID(),
        LeadNumber          NVARCHAR(30)     NOT NULL,
        CompanyName         NVARCHAR(200)    NOT NULL,
        ContactName         NVARCHAR(200)    NOT NULL,
        Email               NVARCHAR(256)    NULL,
        Mobile              NVARCHAR(30)     NULL,
        Industry            NVARCHAR(100)    NULL,
        Source              NVARCHAR(30)     NOT NULL,
        Campaign            NVARCHAR(150)    NULL,
        UtmSource           NVARCHAR(150)    NULL,
        UtmMedium           NVARCHAR(150)    NULL,
        UtmCampaign         NVARCHAR(150)    NULL,
        UtmTerm             NVARCHAR(150)    NULL,
        UtmContent          NVARCHAR(150)    NULL,
        Budget              DECIMAL(18, 2)   NULL,
        Timeline            NVARCHAR(100)    NULL,
        ExpectedValue       DECIMAL(18, 2)   NULL,
        AssignedToUserId    UNIQUEIDENTIFIER NULL,
        Territory           NVARCHAR(100)    NULL,
        Status              NVARCHAR(30)     NOT NULL,
        LeadScore           INT              NULL,
        AiScore             INT              NULL,
        Notes               NVARCHAR(MAX)    NULL,
        NextFollowUpDate    DATETIME2        NULL,
        FollowUpReminderSentAtUtc DATETIME2  NULL,
        LostReason          NVARCHAR(300)    NULL,
        ConvertedCustomerId UNIQUEIDENTIFIER NULL,
        ConvertedAtUtc      DATETIME2        NULL,
        -- No FK: CreatedByUserId is an audit-trail scalar only (no navigation property on Lead),
        -- matching the EF Core model — intentionally does not cascade or restrict on user deletion.
        CreatedByUserId     UNIQUEIDENTIFIER NULL,
        CreatedAtUtc        DATETIME2        NOT NULL,
        UpdatedAtUtc        DATETIME2        NULL,
        CONSTRAINT PK_Leads PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Leads_AssignedToUser FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users (Id) ON DELETE SET NULL,
        CONSTRAINT FK_Leads_ConvertedCustomer FOREIGN KEY (ConvertedCustomerId) REFERENCES dbo.Customers (Id) ON DELETE SET NULL
    );

    CREATE UNIQUE INDEX IX_Leads_LeadNumber ON dbo.Leads (LeadNumber);
    CREATE INDEX IX_Leads_Status ON dbo.Leads (Status);
    CREATE INDEX IX_Leads_AssignedToUserId ON dbo.Leads (AssignedToUserId);
    CREATE INDEX IX_Leads_ConvertedCustomerId ON dbo.Leads (ConvertedCustomerId);
END
GO

-- Leads.NextFollowUpDate / FollowUpReminderSentAtUtc — added post-launch; guarded ALTER for a
-- Leads table that already existed from an earlier run of this script.
IF OBJECT_ID(N'dbo.Leads', N'U') IS NOT NULL AND COL_LENGTH('dbo.Leads', 'NextFollowUpDate') IS NULL
BEGIN
    ALTER TABLE dbo.Leads ADD NextFollowUpDate DATETIME2 NULL;
END
GO

IF OBJECT_ID(N'dbo.Leads', N'U') IS NOT NULL AND COL_LENGTH('dbo.Leads', 'FollowUpReminderSentAtUtc') IS NULL
BEGIN
    ALTER TABLE dbo.Leads ADD FollowUpReminderSentAtUtc DATETIME2 NULL;
END
GO

-- Leads.Utm* — structured UTM tracking parameters, added post-launch alongside the pre-existing
-- freeform Campaign label; guarded ALTERs for a Leads table that already existed from an earlier run.
IF OBJECT_ID(N'dbo.Leads', N'U') IS NOT NULL AND COL_LENGTH('dbo.Leads', 'UtmSource') IS NULL
BEGIN
    ALTER TABLE dbo.Leads ADD UtmSource NVARCHAR(150) NULL;
END
GO

IF OBJECT_ID(N'dbo.Leads', N'U') IS NOT NULL AND COL_LENGTH('dbo.Leads', 'UtmMedium') IS NULL
BEGIN
    ALTER TABLE dbo.Leads ADD UtmMedium NVARCHAR(150) NULL;
END
GO

IF OBJECT_ID(N'dbo.Leads', N'U') IS NOT NULL AND COL_LENGTH('dbo.Leads', 'UtmCampaign') IS NULL
BEGIN
    ALTER TABLE dbo.Leads ADD UtmCampaign NVARCHAR(150) NULL;
END
GO

IF OBJECT_ID(N'dbo.Leads', N'U') IS NOT NULL AND COL_LENGTH('dbo.Leads', 'UtmTerm') IS NULL
BEGIN
    ALTER TABLE dbo.Leads ADD UtmTerm NVARCHAR(150) NULL;
END
GO

IF OBJECT_ID(N'dbo.Leads', N'U') IS NOT NULL AND COL_LENGTH('dbo.Leads', 'UtmContent') IS NULL
BEGIN
    ALTER TABLE dbo.Leads ADD UtmContent NVARCHAR(150) NULL;
END
GO

-- Leads.TerritoryId — structured territory reference (see Territories table above), superseding
-- the legacy free-text Leads.Territory column going forward. That column stays as-is (no removal).
IF OBJECT_ID(N'dbo.Leads', N'U') IS NOT NULL AND COL_LENGTH('dbo.Leads', 'TerritoryId') IS NULL
BEGIN
    ALTER TABLE dbo.Leads ADD TerritoryId UNIQUEIDENTIFIER NULL;
END
GO

IF OBJECT_ID(N'dbo.Leads', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.Leads', 'TerritoryId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Leads_Territories')
BEGIN
    ALTER TABLE dbo.Leads ADD CONSTRAINT FK_Leads_Territories FOREIGN KEY (TerritoryId) REFERENCES dbo.Territories (Id) ON DELETE SET NULL;
END
GO

IF OBJECT_ID(N'dbo.Leads', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Leads_TerritoryId' AND object_id = OBJECT_ID(N'dbo.Leads'))
BEGIN
    CREATE INDEX IX_Leads_TerritoryId ON dbo.Leads (TerritoryId);
END
GO

-- ============================================================================
-- Opportunities (deal pipeline — sits between Customers and the future Quotation/Sales
-- Order modules in the Lead-to-Customer journey; see CRM_SRS Phase 6, section 4)
-- ============================================================================
IF OBJECT_ID(N'dbo.Opportunities', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Opportunities
    (
        Id                UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Opportunities_Id DEFAULT NEWID(),
        OpportunityNumber NVARCHAR(30)     NOT NULL,
        Name              NVARCHAR(200)    NOT NULL,
        CustomerId        UNIQUEIDENTIFIER NOT NULL,
        Value             DECIMAL(18, 2)   NULL,
        CurrencyCode      NVARCHAR(10)     NOT NULL CONSTRAINT DF_Opportunities_CurrencyCode DEFAULT ('USD'),
        Probability       INT              NULL,
        Products          NVARCHAR(1000)   NULL,
        Competitors       NVARCHAR(500)    NULL,
        ExpectedCloseDate DATETIME2        NULL,
        Stage             NVARCHAR(30)     NOT NULL,
        AssignedToUserId  UNIQUEIDENTIFIER NULL,
        SourceLeadId      UNIQUEIDENTIFIER NULL,
        Notes             NVARCHAR(MAX)    NULL,
        LostReason        NVARCHAR(300)    NULL,
        ClosedAtUtc       DATETIME2        NULL,
        -- No FK: CreatedByUserId is an audit-trail scalar only (no navigation property), same
        -- convention as Leads.CreatedByUserId — intentionally does not cascade/restrict on user deletion.
        CreatedByUserId   UNIQUEIDENTIFIER NULL,
        CreatedAtUtc      DATETIME2        NOT NULL,
        UpdatedAtUtc      DATETIME2        NULL,
        CONSTRAINT PK_Opportunities PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Opportunities_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Opportunities_AssignedToUser FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users (Id) ON DELETE SET NULL,
        CONSTRAINT FK_Opportunities_SourceLead FOREIGN KEY (SourceLeadId) REFERENCES dbo.Leads (Id) ON DELETE SET NULL
    );

    CREATE UNIQUE INDEX IX_Opportunities_OpportunityNumber ON dbo.Opportunities (OpportunityNumber);
    CREATE INDEX IX_Opportunities_CustomerId ON dbo.Opportunities (CustomerId);
    CREATE INDEX IX_Opportunities_Stage ON dbo.Opportunities (Stage);
    CREATE INDEX IX_Opportunities_AssignedToUserId ON dbo.Opportunities (AssignedToUserId);
END
GO

-- ============================================================================
-- Opportunities.NextStep / NextStepDate (added post-launch — the Opportunities table
-- above may already exist from an earlier run of this script without these columns,
-- so this is a separate idempotent ALTER guarded by column existence, not table existence).
-- ============================================================================
IF OBJECT_ID(N'dbo.Opportunities', N'U') IS NOT NULL AND COL_LENGTH('dbo.Opportunities', 'NextStep') IS NULL
BEGIN
    ALTER TABLE dbo.Opportunities ADD NextStep NVARCHAR(300) NULL;
END
GO

IF OBJECT_ID(N'dbo.Opportunities', N'U') IS NOT NULL AND COL_LENGTH('dbo.Opportunities', 'NextStepDate') IS NULL
BEGIN
    ALTER TABLE dbo.Opportunities ADD NextStepDate DATETIME2 NULL;
END
GO

-- Opportunities.CurrencyCode — added post-launch; guarded ALTER for an Opportunities table that
-- already existed from an earlier run of this script. Existing rows default to 'USD'.
IF OBJECT_ID(N'dbo.Opportunities', N'U') IS NOT NULL AND COL_LENGTH('dbo.Opportunities', 'CurrencyCode') IS NULL
BEGIN
    ALTER TABLE dbo.Opportunities ADD CurrencyCode NVARCHAR(10) NOT NULL CONSTRAINT DF_Opportunities_CurrencyCode DEFAULT ('USD');
END
GO

-- ============================================================================
-- OpportunityLineItems (optional priced line items on an Opportunity; when present,
-- Opportunities.Value is server-computed as the sum of line totals)
-- ============================================================================
IF OBJECT_ID(N'dbo.OpportunityLineItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OpportunityLineItems
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_OpportunityLineItems_Id DEFAULT NEWID(),
        OpportunityId   UNIQUEIDENTIFIER NOT NULL,
        ProductName     NVARCHAR(200)    NOT NULL,
        Quantity        DECIMAL(18, 2)   NOT NULL,
        UnitPrice       DECIMAL(18, 2)   NOT NULL,
        DiscountPercent DECIMAL(5, 2)    NULL,
        CONSTRAINT PK_OpportunityLineItems PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_OpportunityLineItems_Opportunities FOREIGN KEY (OpportunityId) REFERENCES dbo.Opportunities (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_OpportunityLineItems_OpportunityId ON dbo.OpportunityLineItems (OpportunityId);
END
GO

-- ============================================================================
-- OpportunityContacts (buying committee — Champion/Economic Buyer/Blocker/etc. per deal)
-- ============================================================================
IF OBJECT_ID(N'dbo.OpportunityContacts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OpportunityContacts
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_OpportunityContacts_Id DEFAULT NEWID(),
        OpportunityId   UNIQUEIDENTIFIER NOT NULL,
        ContactPersonId UNIQUEIDENTIFIER NOT NULL,
        Role            NVARCHAR(30)     NOT NULL,
        Notes           NVARCHAR(500)    NULL,
        CONSTRAINT PK_OpportunityContacts PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_OpportunityContacts_Opportunities FOREIGN KEY (OpportunityId) REFERENCES dbo.Opportunities (Id) ON DELETE CASCADE,
        CONSTRAINT FK_OpportunityContacts_ContactPersons FOREIGN KEY (ContactPersonId) REFERENCES dbo.ContactPersons (Id) ON DELETE CASCADE
    );

    CREATE UNIQUE INDEX IX_OpportunityContacts_OpportunityId_ContactPersonId ON dbo.OpportunityContacts (OpportunityId, ContactPersonId);
    CREATE INDEX IX_OpportunityContacts_ContactPersonId ON dbo.OpportunityContacts (ContactPersonId);
END
GO

-- ============================================================================
-- Quotations (priced proposals against an Opportunity — CRM SRS Phase 6, section 5)
-- ============================================================================
IF OBJECT_ID(N'dbo.Quotations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Quotations
    (
        Id                 UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Quotations_Id DEFAULT NEWID(),
        QuotationNumber    NVARCHAR(30)     NOT NULL,
        Version            INT              NOT NULL CONSTRAINT DF_Quotations_Version DEFAULT (1),
        OpportunityId      UNIQUEIDENTIFIER NOT NULL,
        CustomerId         UNIQUEIDENTIFIER NOT NULL,
        Status             NVARCHAR(30)     NOT NULL,
        ValidUntil         DATETIME2        NULL,
        TermsAndConditions NVARCHAR(4000)   NULL,
        Notes              NVARCHAR(MAX)    NULL,
        Subtotal           DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_Quotations_Subtotal DEFAULT (0),
        TaxTotal           DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_Quotations_TaxTotal DEFAULT (0),
        GrandTotal         DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_Quotations_GrandTotal DEFAULT (0),
        AssignedToUserId   UNIQUEIDENTIFIER NULL,
        CreatedByUserId    UNIQUEIDENTIFIER NULL,
        CreatedAtUtc       DATETIME2        NOT NULL,
        UpdatedAtUtc       DATETIME2        NULL,
        CONSTRAINT PK_Quotations PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Quotations_Opportunities FOREIGN KEY (OpportunityId) REFERENCES dbo.Opportunities (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Quotations_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Quotations_AssignedToUser FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users (Id) ON DELETE SET NULL
    );

    CREATE UNIQUE INDEX IX_Quotations_QuotationNumber_Version ON dbo.Quotations (QuotationNumber, Version);
    CREATE INDEX IX_Quotations_OpportunityId ON dbo.Quotations (OpportunityId);
    CREATE INDEX IX_Quotations_CustomerId ON dbo.Quotations (CustomerId);
    CREATE INDEX IX_Quotations_Status ON dbo.Quotations (Status);
    CREATE INDEX IX_Quotations_AssignedToUserId ON dbo.Quotations (AssignedToUserId);
END
GO

-- ============================================================================
-- QuotationLineItems
-- ============================================================================
IF OBJECT_ID(N'dbo.QuotationLineItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QuotationLineItems
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_QuotationLineItems_Id DEFAULT NEWID(),
        QuotationId     UNIQUEIDENTIFIER NOT NULL,
        ProductName     NVARCHAR(200)    NOT NULL,
        Quantity        DECIMAL(18, 2)   NOT NULL,
        UnitPrice       DECIMAL(18, 2)   NOT NULL,
        DiscountPercent DECIMAL(5, 2)    NULL,
        TaxPercent      DECIMAL(5, 2)    NULL,
        CONSTRAINT PK_QuotationLineItems PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_QuotationLineItems_Quotations FOREIGN KEY (QuotationId) REFERENCES dbo.Quotations (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_QuotationLineItems_QuotationId ON dbo.QuotationLineItems (QuotationId);
END
GO

-- ============================================================================
-- SalesOrders (converted from an Accepted Quotation — CRM SRS Phase 6, section 6)
-- ============================================================================
IF OBJECT_ID(N'dbo.SalesOrders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesOrders
    (
        Id                    UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_SalesOrders_Id DEFAULT NEWID(),
        SalesOrderNumber      NVARCHAR(30)     NOT NULL,
        QuotationId           UNIQUEIDENTIFIER NOT NULL,
        CustomerId            UNIQUEIDENTIFIER NOT NULL,
        Status                NVARCHAR(30)     NOT NULL,
        OrderDate             DATETIME2        NOT NULL,
        ExpectedDeliveryDate  DATETIME2        NULL,
        Notes                 NVARCHAR(MAX)    NULL,
        Subtotal              DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_SalesOrders_Subtotal DEFAULT (0),
        TaxTotal              DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_SalesOrders_TaxTotal DEFAULT (0),
        GrandTotal            DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_SalesOrders_GrandTotal DEFAULT (0),
        AssignedToUserId      UNIQUEIDENTIFIER NULL,
        CreatedByUserId       UNIQUEIDENTIFIER NULL,
        CreatedAtUtc          DATETIME2        NOT NULL,
        UpdatedAtUtc          DATETIME2        NULL,
        CONSTRAINT PK_SalesOrders PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_SalesOrders_Quotations FOREIGN KEY (QuotationId) REFERENCES dbo.Quotations (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_SalesOrders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_SalesOrders_AssignedToUser FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users (Id) ON DELETE SET NULL
    );

    -- One quotation converts to at most one sales order.
    CREATE UNIQUE INDEX IX_SalesOrders_QuotationId ON dbo.SalesOrders (QuotationId);
    CREATE UNIQUE INDEX IX_SalesOrders_SalesOrderNumber ON dbo.SalesOrders (SalesOrderNumber);
    CREATE INDEX IX_SalesOrders_CustomerId ON dbo.SalesOrders (CustomerId);
    CREATE INDEX IX_SalesOrders_Status ON dbo.SalesOrders (Status);
    CREATE INDEX IX_SalesOrders_AssignedToUserId ON dbo.SalesOrders (AssignedToUserId);
END
GO

-- ============================================================================
-- SalesOrderLineItems (DeliveredQuantity tracks partial/split delivery)
-- ============================================================================
IF OBJECT_ID(N'dbo.SalesOrderLineItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesOrderLineItems
    (
        Id                UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_SalesOrderLineItems_Id DEFAULT NEWID(),
        SalesOrderId      UNIQUEIDENTIFIER NOT NULL,
        ProductName       NVARCHAR(200)    NOT NULL,
        Quantity          DECIMAL(18, 2)   NOT NULL,
        UnitPrice         DECIMAL(18, 2)   NOT NULL,
        DiscountPercent   DECIMAL(5, 2)    NULL,
        TaxPercent        DECIMAL(5, 2)    NULL,
        DeliveredQuantity DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_SalesOrderLineItems_DeliveredQuantity DEFAULT (0),
        CONSTRAINT PK_SalesOrderLineItems PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_SalesOrderLineItems_SalesOrders FOREIGN KEY (SalesOrderId) REFERENCES dbo.SalesOrders (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_SalesOrderLineItems_SalesOrderId ON dbo.SalesOrderLineItems (SalesOrderId);
END
GO

-- ============================================================================
-- Activities (generic timeline shared by Leads, Customers, Opportunities, and future modules)
-- ============================================================================
IF OBJECT_ID(N'dbo.Activities', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Activities
    (
        Id               UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Activities_Id DEFAULT NEWID(),
        Type             NVARCHAR(30)     NOT NULL,
        Subject          NVARCHAR(200)    NOT NULL,
        Description      NVARCHAR(2000)   NULL,
        RelatedToType    NVARCHAR(30)     NOT NULL,
        RelatedToId      UNIQUEIDENTIFIER NOT NULL,
        DueAtUtc         DATETIME2        NULL,
        CompletedAtUtc   DATETIME2        NULL,
        ReminderSentAtUtc DATETIME2       NULL,
        RecurrenceRule   NVARCHAR(20)     NULL,
        RecurrenceGroupId UNIQUEIDENTIFIER NULL,
        AssignedToUserId UNIQUEIDENTIFIER NULL,
        CreatedByUserId  UNIQUEIDENTIFIER NULL,
        CreatedAtUtc     DATETIME2        NOT NULL,
        CONSTRAINT PK_Activities PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Activities_AssignedToUser FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users (Id) ON DELETE SET NULL,
        CONSTRAINT FK_Activities_CreatedByUser FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id) ON DELETE SET NULL
    );

    CREATE INDEX IX_Activities_RelatedToType_RelatedToId ON dbo.Activities (RelatedToType, RelatedToId);
    CREATE INDEX IX_Activities_RecurrenceGroupId ON dbo.Activities (RecurrenceGroupId);
END
GO

-- Activities.ReminderSentAtUtc — added post-launch; guarded ALTER for an Activities table that
-- already existed from an earlier run of this script.
IF OBJECT_ID(N'dbo.Activities', N'U') IS NOT NULL AND COL_LENGTH('dbo.Activities', 'ReminderSentAtUtc') IS NULL
BEGIN
    ALTER TABLE dbo.Activities ADD ReminderSentAtUtc DATETIME2 NULL;
END
GO

-- Activities.RecurrenceRule / RecurrenceGroupId — added post-launch; guarded ALTERs for an
-- Activities table that already existed from an earlier run of this script.
IF OBJECT_ID(N'dbo.Activities', N'U') IS NOT NULL AND COL_LENGTH('dbo.Activities', 'RecurrenceRule') IS NULL
BEGIN
    ALTER TABLE dbo.Activities ADD RecurrenceRule NVARCHAR(20) NULL;
END
GO

IF OBJECT_ID(N'dbo.Activities', N'U') IS NOT NULL AND COL_LENGTH('dbo.Activities', 'RecurrenceGroupId') IS NULL
BEGIN
    ALTER TABLE dbo.Activities ADD RecurrenceGroupId UNIQUEIDENTIFIER NULL;
END
GO

IF OBJECT_ID(N'dbo.Activities', N'U') IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_Activities_RecurrenceGroupId' AND object_id = OBJECT_ID(N'dbo.Activities')
)
BEGIN
    CREATE INDEX IX_Activities_RecurrenceGroupId ON dbo.Activities (RecurrenceGroupId);
END
GO

-- ============================================================================
-- AuditLogs (plain-English history entries for Lead/Opportunity/Customer mutations —
-- intentionally not a full field-by-field diff, see AuditLog.cs for rationale)
-- ============================================================================
IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_AuditLogs_Id DEFAULT NEWID(),
        EntityType      NVARCHAR(50)     NOT NULL,
        EntityId        UNIQUEIDENTIFIER NOT NULL,
        Action          NVARCHAR(30)     NOT NULL,
        Summary         NVARCHAR(1000)   NOT NULL,
        PerformedByUserId UNIQUEIDENTIFIER NULL,
        CreatedAtUtc    DATETIME2        NOT NULL,
        CONSTRAINT PK_AuditLogs PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_AuditLogs_PerformedByUser FOREIGN KEY (PerformedByUserId) REFERENCES dbo.Users (Id) ON DELETE SET NULL
    );

    CREATE INDEX IX_AuditLogs_EntityType_EntityId ON dbo.AuditLogs (EntityType, EntityId);
END
GO

-- ============================================================================
-- Notifications (polling-based in-app notifications — no push/SignalR in this milestone)
-- ============================================================================
IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications
    (
        Id                UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Notifications_Id DEFAULT NEWID(),
        RecipientUserId   UNIQUEIDENTIFIER NOT NULL,
        Message           NVARCHAR(500)    NOT NULL,
        RelatedEntityType NVARCHAR(30)     NULL,
        RelatedEntityId   UNIQUEIDENTIFIER NULL,
        IsRead            BIT              NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT (0),
        CreatedAtUtc      DATETIME2        NOT NULL,
        CONSTRAINT PK_Notifications PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Notifications_RecipientUser FOREIGN KEY (RecipientUserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_Notifications_RecipientUserId_IsRead ON dbo.Notifications (RecipientUserId, IsRead);
END
GO

-- ============================================================================
-- UserDelegations (out-of-office: DelegateUser temporarily covers the records and due reminders
-- assigned to DelegatorUser during [StartDateUtc, EndDateUtc])
-- ============================================================================
IF OBJECT_ID(N'dbo.UserDelegations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserDelegations
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_UserDelegations_Id DEFAULT NEWID(),
        DelegatorUserId UNIQUEIDENTIFIER NOT NULL,
        DelegateUserId  UNIQUEIDENTIFIER NOT NULL,
        StartDateUtc    DATETIME2        NOT NULL,
        EndDateUtc      DATETIME2        NOT NULL,
        Notes           NVARCHAR(500)    NULL,
        CreatedAtUtc    DATETIME2        NOT NULL,
        CONSTRAINT PK_UserDelegations PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_UserDelegations_DelegatorUser FOREIGN KEY (DelegatorUserId) REFERENCES dbo.Users (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_UserDelegations_DelegateUser FOREIGN KEY (DelegateUserId) REFERENCES dbo.Users (Id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_UserDelegations_DelegatorUserId ON dbo.UserDelegations (DelegatorUserId);
    CREATE INDEX IX_UserDelegations_DelegateUserId ON dbo.UserDelegations (DelegateUserId);
    CREATE INDEX IX_UserDelegations_StartDateUtc_EndDateUtc ON dbo.UserDelegations (StartDateUtc, EndDateUtc);
END
GO

-- ============================================================================
-- Documents (generic file attachments for any CRM record — content stored as a blob directly
-- in the tenant database; see Document.cs for the storage-location rationale)
-- ============================================================================
IF OBJECT_ID(N'dbo.Documents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Documents
    (
        Id               UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Documents_Id DEFAULT NEWID(),
        EntityType       NVARCHAR(50)     NOT NULL,
        EntityId         UNIQUEIDENTIFIER NOT NULL,
        FileName         NVARCHAR(260)    NOT NULL,
        ContentType      NVARCHAR(150)    NOT NULL,
        SizeBytes        BIGINT           NOT NULL,
        Content          VARBINARY(MAX)   NOT NULL,
        UploadedByUserId UNIQUEIDENTIFIER NULL,
        CreatedAtUtc     DATETIME2        NOT NULL,
        CONSTRAINT PK_Documents PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Documents_UploadedByUser FOREIGN KEY (UploadedByUserId) REFERENCES dbo.Users (Id) ON DELETE SET NULL
    );

    CREATE INDEX IX_Documents_EntityType_EntityId ON dbo.Documents (EntityType, EntityId);
END
GO

-- ============================================================================
-- RefreshTokens (long-lived credential backing silent access-token renewal — see
-- RefreshToken.cs; only a SHA-256 hash of the raw token is ever stored)
-- ============================================================================
IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshTokens
    (
        Id                   UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_RefreshTokens_Id DEFAULT NEWID(),
        UserId               UNIQUEIDENTIFIER NOT NULL,
        TokenHash            NVARCHAR(128)    NOT NULL,
        ExpiresAtUtc         DATETIME2        NOT NULL,
        CreatedAtUtc         DATETIME2        NOT NULL,
        RevokedAtUtc         DATETIME2        NULL,
        ReplacedByTokenHash  NVARCHAR(128)    NULL,
        CONSTRAINT PK_RefreshTokens PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_RefreshTokens_User FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX IX_RefreshTokens_TokenHash ON dbo.RefreshTokens (TokenHash);
    CREATE INDEX IX_RefreshTokens_UserId ON dbo.RefreshTokens (UserId);
END
GO

-- ============================================================================
-- PasswordResetTokens (short-lived, single-use token backing the "Forgot Password?" email
-- flow — see PasswordResetToken.cs; only a SHA-256 hash of the raw token is ever stored)
-- ============================================================================
IF OBJECT_ID(N'dbo.PasswordResetTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PasswordResetTokens
    (
        Id             UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_PasswordResetTokens_Id DEFAULT NEWID(),
        UserId         UNIQUEIDENTIFIER NOT NULL,
        TokenHash      NVARCHAR(128)    NOT NULL,
        ExpiresAtUtc   DATETIME2        NOT NULL,
        CreatedAtUtc   DATETIME2        NOT NULL,
        UsedAtUtc      DATETIME2        NULL,
        CONSTRAINT PK_PasswordResetTokens PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_PasswordResetTokens_User FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX IX_PasswordResetTokens_TokenHash ON dbo.PasswordResetTokens (TokenHash);
    CREATE INDEX IX_PasswordResetTokens_UserId ON dbo.PasswordResetTokens (UserId);
END
GO
