/*
    004_Fix_LeadCustomerLink.sql

    IMPORTANT — run this BEFORE using the app again after this update, not just before
    trying the new feature. The Lead entity now always includes a LinkedCustomerId column
    in every save. Until this script runs, creating OR editing ANY lead will fail with a
    SQL error ("Invalid column name 'LinkedCustomerId'") — this is not limited to the new
    "select existing customer" feature.

    What this adds: a new nullable LinkedCustomerId column on dbo.Leads, so a Lead can be
    linked to a pre-existing Customer (picked via the new "existing customer" matcher on the
    Lead form) instead of always creating a brand-new Customer when later converted.

    This script is safe to run — every statement is guarded (IF ... IS NULL / WHERE NOT EXISTS),
    so it only adds what's missing and won't touch or duplicate anything that already exists.
    It's also already folded into 001_CreateSchema.sql (this same folder) for future reference — this
    file is just a fast, standalone way to apply that one change right now.

    How to run:
      1. Open SSMS, Azure Data Studio, or any SQL Server client from a machine that
         can reach 80.65.208.158 (this sandbox's network is restricted and can't).
      2. Connect using the credentials in
         StaffingManagementSystem.api/appsettings.json -> ConnectionStrings:StaffingManagementSystemDb
         (Server=80.65.208.158; Database=itmuske1_ZentavioCRM; User Id=itmuske1_ZentavioCRM).
      3. Run this entire script against that database.
      4. No permission changes are needed this time (the feature reuses the existing
         Leads.View/Create/Edit permissions) — no log-out/log-in required either.
*/

IF OBJECT_ID(N'dbo.Leads', N'U') IS NOT NULL AND COL_LENGTH('dbo.Leads', 'LinkedCustomerId') IS NULL
BEGIN
    ALTER TABLE dbo.Leads ADD LinkedCustomerId UNIQUEIDENTIFIER NULL;
END
GO

IF OBJECT_ID(N'dbo.Leads', N'U') IS NOT NULL
   AND COL_LENGTH('dbo.Leads', 'LinkedCustomerId') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Leads_LinkedCustomer')
BEGIN
    ALTER TABLE dbo.Leads ADD CONSTRAINT FK_Leads_LinkedCustomer FOREIGN KEY (LinkedCustomerId) REFERENCES dbo.Customers (Id) ON DELETE SET NULL;
END
GO

IF OBJECT_ID(N'dbo.Leads', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Leads_LinkedCustomerId' AND object_id = OBJECT_ID(N'dbo.Leads'))
BEGIN
    CREATE INDEX IX_Leads_LinkedCustomerId ON dbo.Leads (LinkedCustomerId);
END
GO

-- Verify
SELECT COL_LENGTH('dbo.Leads', 'LinkedCustomerId') AS LinkedCustomerId_ColumnExists;
SELECT name FROM sys.foreign_keys WHERE name = 'FK_Leads_LinkedCustomer';
SELECT name FROM sys.indexes WHERE name = 'IX_Leads_LinkedCustomerId';
