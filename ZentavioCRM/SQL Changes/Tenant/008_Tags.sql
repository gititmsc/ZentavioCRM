/*
    008_Tags.sql

    Adds the structured Tag system: a new dbo.Tags table plus dbo.LeadTags/dbo.CustomerTags join
    tables, letting any number of reusable tags be attached to a Lead and/or a Customer — powers
    the new Tag Manager screen and the tag picker on the Lead/Customer forms. Also adds the new
    Tags.View / Tags.Manage permissions.

    This is purely additive and does NOT touch dbo.Customers.Tags, the original freeform
    comma-separated text column — that stays exactly as it was, still read and written by CSV
    import/export, so no existing data or workflow breaks. The two systems coexist: the freeform
    field for quick ad-hoc labels and CSV compatibility, the structured Tag table for a manageable,
    reusable, shared vocabulary of tags across Leads and Customers.

    This script is safe to run and safe to re-run — every statement is guarded (IF OBJECT_ID ... IS
    NULL / IF NOT EXISTS / WHERE NOT EXISTS), so it only adds what's missing.

    It's also already folded into 001_CreateSchema.sql and TenantSchema.sql (this same folder) for
    future reference — this file is just a fast, standalone way to apply the change to an existing
    tenant database right now.

    How to run:
      1. Open SSMS, Azure Data Studio, or any SQL Server client from a machine that
         can reach the tenant's database server.
      2. Connect using that tenant's connection string.
      3. Run this entire script against that database.
      4. Users with the Administrator, Sales Manager, Sales Executive, or Support Agent role get
         the new permissions automatically (granted below) — no log-out/log-in required, their next
         token refresh picks it up.
*/

-- ============================================================================
-- Tags table
-- ============================================================================
IF OBJECT_ID(N'dbo.Tags', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Tags
    (
        Id           UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Tags_Id DEFAULT NEWID(),
        Name         NVARCHAR(100)    NOT NULL,
        Color        NVARCHAR(20)     NULL,
        CreatedAtUtc DATETIME2        NOT NULL,
        CONSTRAINT PK_Tags PRIMARY KEY CLUSTERED (Id)
    );

    CREATE UNIQUE INDEX IX_Tags_Name ON dbo.Tags (Name);
END
GO

-- ============================================================================
-- LeadTags / CustomerTags join tables
-- ============================================================================
IF OBJECT_ID(N'dbo.LeadTags', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LeadTags
    (
        LeadId UNIQUEIDENTIFIER NOT NULL,
        TagId  UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_LeadTags PRIMARY KEY CLUSTERED (LeadId, TagId),
        CONSTRAINT FK_LeadTags_Lead FOREIGN KEY (LeadId) REFERENCES dbo.Leads (Id) ON DELETE CASCADE,
        CONSTRAINT FK_LeadTags_Tag FOREIGN KEY (TagId) REFERENCES dbo.Tags (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_LeadTags_TagId ON dbo.LeadTags (TagId);
END
GO

IF OBJECT_ID(N'dbo.CustomerTags', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerTags
    (
        CustomerId UNIQUEIDENTIFIER NOT NULL,
        TagId      UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_CustomerTags PRIMARY KEY CLUSTERED (CustomerId, TagId),
        CONSTRAINT FK_CustomerTags_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id) ON DELETE CASCADE,
        CONSTRAINT FK_CustomerTags_Tag FOREIGN KEY (TagId) REFERENCES dbo.Tags (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_CustomerTags_TagId ON dbo.CustomerTags (TagId);
END
GO

-- ============================================================================
-- Tags.View / Tags.Manage permissions + grants
-- (Administrator: all; Sales Manager: View+Manage; Sales Executive/Support Agent: View only)
-- ============================================================================
INSERT INTO dbo.Permissions (Id, Code, Name, Module)
SELECT v.Id, v.Code, v.Name, v.Module
FROM (VALUES
    ('10000000-0000-0000-0000-000000000027', N'Tags.View',   N'View',   N'Tags'),
    ('10000000-0000-0000-0000-000000000028', N'Tags.Manage', N'Manage', N'Tags')
) AS v(Id, Code, Name, Module)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Permissions p WHERE p.Id = v.Id);
GO

-- Administrator — both.
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT '20000000-0000-0000-0000-000000000001', p.Id FROM dbo.Permissions p
WHERE p.Code IN (N'Tags.View', N'Tags.Manage')
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId = '20000000-0000-0000-0000-000000000001' AND rp.PermissionId = p.Id);

-- Sales Manager — both.
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT '20000000-0000-0000-0000-000000000002', p.Id FROM dbo.Permissions p
WHERE p.Code IN (N'Tags.View', N'Tags.Manage')
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId = '20000000-0000-0000-0000-000000000002' AND rp.PermissionId = p.Id);

-- Sales Executive — View only.
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT '20000000-0000-0000-0000-000000000003', p.Id FROM dbo.Permissions p
WHERE p.Code = N'Tags.View'
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId = '20000000-0000-0000-0000-000000000003' AND rp.PermissionId = p.Id);

-- Support Agent — View only.
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT '20000000-0000-0000-0000-000000000004', p.Id FROM dbo.Permissions p
WHERE p.Code = N'Tags.View'
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId = '20000000-0000-0000-0000-000000000004' AND rp.PermissionId = p.Id);
GO

-- Verify
SELECT COUNT(*) AS TagsTableExists FROM sys.tables WHERE name IN ('Tags', 'LeadTags', 'CustomerTags');
SELECT Id, Code FROM dbo.Permissions WHERE Code IN (N'Tags.View', N'Tags.Manage');
SELECT RoleId, PermissionId FROM dbo.RolePermissions WHERE PermissionId IN ('10000000-0000-0000-0000-000000000027', '10000000-0000-0000-0000-000000000028');
