/*
    009_LeadAssignment.sql

    Adds round-robin / territory-based lead auto-assignment routing:
      - dbo.LeadAssignmentSettings — a single tenant-wide on/off switch (defaults OFF).
      - dbo.LeadAssignmentRules — one routing pool per Territory, plus a single NULL-TerritoryId
        fallback rule for leads with no territory (or no matching territory-specific rule). Tracks
        a round-robin cursor (LastAssignedUserId) per rule.
      - dbo.LeadAssignmentRuleUsers — the eligible-user pool for each rule.
    Also adds the new Leads.ManageAssignment permission (Administrator + Sales Manager only, same
    tier as Leads.ManageScoring).

    This is purely additive. AutoAssignEnabled defaults to 0 (off), so applying this script changes
    no existing behavior until an admin both turns it on and configures at least one rule — Lead
    creation continues to leave AssignedToUserId exactly as it is today until then.

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
      4. Administrator and Sales Manager users get the new permission automatically (granted
         below) — no log-out/log-in required, their next token refresh picks it up.
*/

-- ============================================================================
-- LeadAssignmentSettings table
-- ============================================================================
IF OBJECT_ID(N'dbo.LeadAssignmentSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LeadAssignmentSettings
    (
        Id                UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_LeadAssignmentSettings_Id DEFAULT NEWID(),
        AutoAssignEnabled BIT              NOT NULL CONSTRAINT DF_LeadAssignmentSettings_AutoAssignEnabled DEFAULT (0),
        UpdatedByUserId   UNIQUEIDENTIFIER NULL,
        UpdatedAtUtc      DATETIME2        NULL,
        CONSTRAINT PK_LeadAssignmentSettings PRIMARY KEY CLUSTERED (Id)
    );

    INSERT INTO dbo.LeadAssignmentSettings (Id)
    VALUES ('60000000-0000-0000-0000-000000000002');
END
GO

-- ============================================================================
-- LeadAssignmentRules / LeadAssignmentRuleUsers tables
-- ============================================================================
IF OBJECT_ID(N'dbo.LeadAssignmentRules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LeadAssignmentRules
    (
        Id                 UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_LeadAssignmentRules_Id DEFAULT NEWID(),
        TerritoryId        UNIQUEIDENTIFIER NULL,
        IsActive           BIT              NOT NULL CONSTRAINT DF_LeadAssignmentRules_IsActive DEFAULT (1),
        LastAssignedUserId UNIQUEIDENTIFIER NULL,
        LastAssignedAtUtc  DATETIME2        NULL,
        CreatedByUserId    UNIQUEIDENTIFIER NULL,
        CreatedAtUtc       DATETIME2        NOT NULL,
        UpdatedAtUtc       DATETIME2        NULL,
        CONSTRAINT PK_LeadAssignmentRules PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_LeadAssignmentRules_Territory FOREIGN KEY (TerritoryId) REFERENCES dbo.Territories (Id) ON DELETE SET NULL,
        CONSTRAINT FK_LeadAssignmentRules_LastAssignedUser FOREIGN KEY (LastAssignedUserId) REFERENCES dbo.Users (Id) ON DELETE SET NULL
    );

    CREATE INDEX IX_LeadAssignmentRules_TerritoryId ON dbo.LeadAssignmentRules (TerritoryId);
END
GO

IF OBJECT_ID(N'dbo.LeadAssignmentRuleUsers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LeadAssignmentRuleUsers
    (
        RuleId UNIQUEIDENTIFIER NOT NULL,
        UserId UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_LeadAssignmentRuleUsers PRIMARY KEY CLUSTERED (RuleId, UserId),
        CONSTRAINT FK_LeadAssignmentRuleUsers_Rule FOREIGN KEY (RuleId) REFERENCES dbo.LeadAssignmentRules (Id) ON DELETE CASCADE,
        CONSTRAINT FK_LeadAssignmentRuleUsers_User FOREIGN KEY (UserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_LeadAssignmentRuleUsers_UserId ON dbo.LeadAssignmentRuleUsers (UserId);
END
GO

-- ============================================================================
-- Leads.ManageAssignment permission + grants
-- (Administrator + Sales Manager only — same tier as Leads.ManageScoring)
-- ============================================================================
INSERT INTO dbo.Permissions (Id, Code, Name, Module)
SELECT v.Id, v.Code, v.Name, v.Module
FROM (VALUES
    ('10000000-0000-0000-0000-000000000029', N'Leads.ManageAssignment', N'ManageAssignment', N'Leads')
) AS v(Id, Code, Name, Module)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Permissions p WHERE p.Id = v.Id);
GO

-- Administrator.
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT '20000000-0000-0000-0000-000000000001', p.Id FROM dbo.Permissions p
WHERE p.Code = N'Leads.ManageAssignment'
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId = '20000000-0000-0000-0000-000000000001' AND rp.PermissionId = p.Id);

-- Sales Manager.
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT '20000000-0000-0000-0000-000000000002', p.Id FROM dbo.Permissions p
WHERE p.Code = N'Leads.ManageAssignment'
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId = '20000000-0000-0000-0000-000000000002' AND rp.PermissionId = p.Id);

-- The rule-management screen also needs Territories.View (to populate the territory picker) —
-- top up Sales Manager with it here too, in case this tenant was provisioned before Territories
-- existed or Sales Manager otherwise never got it.
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT '20000000-0000-0000-0000-000000000002', p.Id FROM dbo.Permissions p
WHERE p.Code = N'Territories.View'
  AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId = '20000000-0000-0000-0000-000000000002' AND rp.PermissionId = p.Id);
GO

-- Verify
SELECT COUNT(*) AS LeadAssignmentTablesExist FROM sys.tables WHERE name IN ('LeadAssignmentSettings', 'LeadAssignmentRules', 'LeadAssignmentRuleUsers');
SELECT Id, Code FROM dbo.Permissions WHERE Code = N'Leads.ManageAssignment';
SELECT RoleId, PermissionId FROM dbo.RolePermissions WHERE PermissionId = '10000000-0000-0000-0000-000000000029';
