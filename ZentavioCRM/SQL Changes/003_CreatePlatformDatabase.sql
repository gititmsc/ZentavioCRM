/*
    003_CreatePlatformDatabase.sql
    ZentavioCRM — Platform (master) database.

    This is the ONE shared database for the whole SaaS deployment — the tenant registry.
    It is NEVER used for application data (no Users, Leads, Customers, etc. live here).

    Run this against a database named in ConnectionStrings:PlatformDb (appsettings.json),
    e.g. "ZentavioCRM_Platform". Create that database first if it doesn't already exist —
    unlike the tenant databases (which the app creates for you via the provisioning API),
    the Platform database itself is a one-time manual setup step.

    Relationship to 001/002:
      - 001_CreateSchema.sql / 002_SeedData.sql describe a TENANT database (one per customer
        company) — Companies, Users, Leads, Customers, etc.
      - This script describes the PLATFORM database — just the Tenants table below, which
        records which tenant databases exist and how to reach them.
      - New tenants are no longer expected to be created by manually re-running 001/002 by hand;
        POST /api/platform/tenants (TenantProvisioningService) does that automatically: it creates
        a new tenant database, applies the same schema + RBAC seed, creates the tenant's Company
        and first Admin user, and inserts the corresponding row here. 001/002 remain useful as a
        human-readable reference and for manually building a one-off local dev tenant database.
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
        -- Provisioning | Active | Suspended | Failed — see ZentavioCRM.Core.Enums.TenantStatus.
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
