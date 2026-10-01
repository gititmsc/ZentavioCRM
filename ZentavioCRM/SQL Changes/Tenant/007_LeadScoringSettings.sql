/*
    007_LeadScoringSettings.sql

    Adds the configurable lead-scoring engine: a new dbo.LeadScoringSettings table (exactly one
    row per tenant) holding tenant-editable weights for LeadService.ComputeLeadScore, plus the new
    Leads.ManageScoring permission that gates editing them. Defaults reproduce the original
    hardcoded formula exactly, so running this script changes nothing about existing lead scores
    until an admin actually edits a weight from the new Lead Scoring Settings screen.

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
      4. Users with the Administrator or Sales Manager role get the new permission automatically
         (granted below) — no log-out/log-in required, their next token refresh picks it up.
*/

-- ============================================================================
-- LeadScoringSettings table + default row
-- ============================================================================
IF OBJECT_ID(N'dbo.LeadScoringSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LeadScoringSettings
    (
        Id                           UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_LeadScoringSettings_Id DEFAULT NEWID(),
        EmailPresentPoints           INT              NOT NULL CONSTRAINT DF_LeadScoringSettings_EmailPresentPoints DEFAULT (15),
        MobilePresentPoints          INT              NOT NULL CONSTRAINT DF_LeadScoringSettings_MobilePresentPoints DEFAULT (15),
        IndustryPresentPoints        INT              NOT NULL CONSTRAINT DF_LeadScoringSettings_IndustryPresentPoints DEFAULT (10),
        AssignedPoints               INT              NOT NULL CONSTRAINT DF_LeadScoringSettings_AssignedPoints DEFAULT (10),
        ExpectedValueHighThreshold   DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_LeadScoringSettings_ExpectedValueHighThreshold DEFAULT (50000),
        ExpectedValueHighPoints      INT              NOT NULL CONSTRAINT DF_LeadScoringSettings_ExpectedValueHighPoints DEFAULT (25),
        ExpectedValueMediumThreshold DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_LeadScoringSettings_ExpectedValueMediumThreshold DEFAULT (10000),
        ExpectedValueMediumPoints    INT              NOT NULL CONSTRAINT DF_LeadScoringSettings_ExpectedValueMediumPoints DEFAULT (15),
        ExpectedValueLowPoints       INT              NOT NULL CONSTRAINT DF_LeadScoringSettings_ExpectedValueLowPoints DEFAULT (5),
        SourceReferralPoints         INT              NOT NULL CONSTRAINT DF_LeadScoringSettings_SourceReferralPoints DEFAULT (20),
        SourceWarmChannelPoints      INT              NOT NULL CONSTRAINT DF_LeadScoringSettings_SourceWarmChannelPoints DEFAULT (10),
        UrgentTimelinePoints         INT              NOT NULL CONSTRAINT DF_LeadScoringSettings_UrgentTimelinePoints DEFAULT (5),
        PointsPerCompletedActivity   INT              NOT NULL CONSTRAINT DF_LeadScoringSettings_PointsPerCompletedActivity DEFAULT (2),
        EngagementMaxPoints          INT              NOT NULL CONSTRAINT DF_LeadScoringSettings_EngagementMaxPoints DEFAULT (10),
        MaxScore                     INT              NOT NULL CONSTRAINT DF_LeadScoringSettings_MaxScore DEFAULT (100),
        UpdatedByUserId              UNIQUEIDENTIFIER NULL,
        UpdatedAtUtc                 DATETIME2        NULL,
        CONSTRAINT PK_LeadScoringSettings PRIMARY KEY CLUSTERED (Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.LeadScoringSettings WHERE Id = '60000000-0000-0000-0000-000000000001')
BEGIN
    INSERT INTO dbo.LeadScoringSettings (Id)
    VALUES ('60000000-0000-0000-0000-000000000001');
END
GO

-- ============================================================================
-- Leads.ManageScoring permission + grants (Administrator, Sales Manager)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Id = '10000000-0000-0000-0000-000000000026')
BEGIN
    INSERT INTO dbo.Permissions (Id, Code, Name, Module)
    VALUES ('10000000-0000-0000-0000-000000000026', N'Leads.ManageScoring', N'ManageScoring', N'Leads');
END
GO

INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT r.Id, '10000000-0000-0000-0000-000000000026'
FROM dbo.Roles r
WHERE r.Id IN ('20000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000002') -- Administrator, Sales Manager
  AND NOT EXISTS (
      SELECT 1 FROM dbo.RolePermissions rp
      WHERE rp.RoleId = r.Id AND rp.PermissionId = '10000000-0000-0000-0000-000000000026'
  );
GO

-- Verify
SELECT COUNT(*) AS LeadScoringSettingsRowCount FROM dbo.LeadScoringSettings;
SELECT Id, Code FROM dbo.Permissions WHERE Code = N'Leads.ManageScoring';
SELECT RoleId, PermissionId FROM dbo.RolePermissions WHERE PermissionId = '10000000-0000-0000-0000-000000000026';
