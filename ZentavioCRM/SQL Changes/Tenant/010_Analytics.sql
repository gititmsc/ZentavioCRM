/*
    010_Analytics.sql

    Adds custom dashboards and the report builder:
      - dbo.SavedAnalyticsItems — user-built Dashboards (Kind = 1) and saved Reports (Kind = 2).
        ConfigJson is the opaque widget layout / report definition owned by the UI; every query it
        describes is re-validated and access-scoped by the analytics engine when it runs. Items are
        private to their owner unless IsShared = 1 (visible to the whole tenant).
    Also adds the new Analytics.ManageShared permission (publish items to the whole team, and edit
    or delete shared items owned by others). Administrator + Sales Manager only. Building and
    running personal dashboards/reports needs no permission beyond the View permission of the
    module being reported on.

    This is purely additive — no existing table or behavior changes.

    This script is safe to run and safe to re-run — every statement is guarded (IF OBJECT_ID ... IS
    NULL / WHERE NOT EXISTS), so it only adds what's missing.

    It's also already folded into 001_CreateSchema.sql and TenantSchema.sql (this same folder) for
    future reference — this file is just a fast, standalone way to apply the change to an existing
    tenant database right now.

    How to run:
      1. Open SSMS, Azure Data Studio, or any SQL Server client from a machine that
         can reach the tenant's database server.
      2. Connect using that tenant's connection string.
      3. Run this entire script against that database.
      4. Administrator and Sales Manager users get the new permission automatically (granted
         below) — no log-out/log-in required, their next token refresh picks it up.
*/

-- ============================================================================
-- SavedAnalyticsItems table
-- ============================================================================
IF OBJECT_ID(N'dbo.SavedAnalyticsItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SavedAnalyticsItems
    (
        Id           UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_SavedAnalyticsItems_Id DEFAULT NEWID(),
        Kind         INT              NOT NULL,
        Name         NVARCHAR(150)    NOT NULL,
        Description  NVARCHAR(500)    NULL,
        OwnerUserId  UNIQUEIDENTIFIER NOT NULL,
        IsShared     BIT              NOT NULL CONSTRAINT DF_SavedAnalyticsItems_IsShared DEFAULT (0),
        ConfigJson   NVARCHAR(MAX)    NOT NULL,
        CreatedAtUtc DATETIME2        NOT NULL,
        UpdatedAtUtc DATETIME2        NULL,
        CONSTRAINT PK_SavedAnalyticsItems PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_SavedAnalyticsItems_OwnerUser FOREIGN KEY (OwnerUserId) REFERENCES dbo.Users (Id)
    );

    CREATE INDEX IX_SavedAnalyticsItems_Kind_OwnerUserId ON dbo.SavedAnalyticsItems (Kind, OwnerUserId);
    CREATE INDEX IX_SavedAnalyticsItems_Kind_IsShared ON dbo.SavedAnalyticsItems (Kind, IsShared);
END
GO

-- ============================================================================
-- Analytics.ManageShared permission + grants
-- (Administrator + Sales Manager only)
-- ============================================================================
INSERT INTO dbo.Permissions (Id, Code, Name, Module)
SELECT v.Id, v.Code, v.Name, v.Module
FROM (VALUES
    ('10000000-0000-0000-0000-000000000030', N'Analytics.ManageShared', N'ManageShared', N'Analytics')
) AS v(Id, Code, Name, Module)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Permissions p WHERE p.Id = v.Id);
GO

-- Administrator.
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT '20000000-0000-0000-0000-000000000001', p.Id FROM dbo.Permissions p
WHERE p.Code = N'Analytics.ManageShared'
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId = '20000000-0000-0000-0000-000000000001' AND rp.PermissionId = p.Id);

-- Sales Manager.
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT '20000000-0000-0000-0000-000000000002', p.Id FROM dbo.Permissions p
WHERE p.Code = N'Analytics.ManageShared'
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId = '20000000-0000-0000-0000-000000000002' AND rp.PermissionId = p.Id);
GO

-- Verify
SELECT COUNT(*) AS SavedAnalyticsTableExists FROM sys.tables WHERE name = 'SavedAnalyticsItems';
SELECT Id, Code FROM dbo.Permissions WHERE Code = N'Analytics.ManageShared';
SELECT RoleId, PermissionId FROM dbo.RolePermissions WHERE PermissionId = '10000000-0000-0000-0000-000000000030';
