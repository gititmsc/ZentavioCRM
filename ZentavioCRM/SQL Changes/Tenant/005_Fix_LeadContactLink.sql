/*
    005_Fix_LeadContactLink.sql

    IMPORTANT — run this BEFORE using the app again after this update, not just before
    trying the new feature. The Lead entity now always includes a LinkedContactId column
    in every save. Until this script runs, creating OR editing ANY lead will fail with a
    SQL error ("Invalid column name 'LinkedContactId'") — this is not limited to the new
    "auto-select the single contact" feature.

    What this adds: a new nullable LinkedContactId column on dbo.Leads, so a Lead can point
    at one specific ContactPerson on its linked Customer (dbo.ContactPersons). When a lead is
    linked to an existing company that has exactly one contact on file, that contact is now
    auto-selected instead of asking the user to pick it. If the user then edits the Contact
    Name/Email/Mobile fields on the Lead form, saving the lead updates that same contact
    record (instead of leaving the edit stranded on the Lead only); if no contact was linked
    yet, saving instead adds a brand-new contact to that company and remembers it here so
    later saves keep updating it rather than creating duplicates.

    NOTE ON THE MISSING FOREIGN KEY — this is deliberate, not an oversight. dbo.ContactPersons
    already cascade-deletes from dbo.Customers, and dbo.Leads.LinkedCustomerId already SET NULLs
    from dbo.Customers directly. Adding a THIRD path — LinkedContactId SET NULL-ing from
    ContactPersons, which itself cascades from Customers — gives SQL Server two different-length
    paths from Customers into Leads, which it refuses to create ("Msg 1785 ... may cause cycles
    or multiple cascade paths"), regardless of whether the app would ever actually hit it.
    Rather than loosen the existing Customer -> ContactPersons cascade (which the Customer-delete
    feature relies on) or switch this new FK to NO ACTION (which would break editing a customer's
    contacts whenever one of them is lead-linked, since that replaces all contacts in one go),
    LinkedContactId is kept as an application-enforced reference only: ZentavioCRM.Services.LeadService's
    SyncLinkedContactAsync already tolerates a missing/stale LinkedContactId gracefully (it just
    creates a fresh contact and re-links), so no database-level constraint is needed for correctness.
    The column and its index are still added below, just not a FOREIGN KEY constraint.

    This script is safe to run — every statement is guarded (IF ... IS NULL / WHERE NOT EXISTS),
    so it only adds what's missing and won't touch or duplicate anything that already exists.
    Safe to re-run even after the earlier "multiple cascade paths" error — that failed statement
    never created anything, and the column/index from before are picked up as already-done.
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

IF OBJECT_ID(N'dbo.Leads', N'U') IS NOT NULL AND COL_LENGTH('dbo.Leads', 'LinkedContactId') IS NULL
BEGIN
    ALTER TABLE dbo.Leads ADD LinkedContactId UNIQUEIDENTIFIER NULL;
END
GO

IF OBJECT_ID(N'dbo.Leads', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Leads_LinkedContactId' AND object_id = OBJECT_ID(N'dbo.Leads'))
BEGIN
    CREATE INDEX IX_Leads_LinkedContactId ON dbo.Leads (LinkedContactId);
END
GO

-- Verify
SELECT COL_LENGTH('dbo.Leads', 'LinkedContactId') AS LinkedContactId_ColumnExists;
SELECT name FROM sys.indexes WHERE name = 'IX_Leads_LinkedContactId';
