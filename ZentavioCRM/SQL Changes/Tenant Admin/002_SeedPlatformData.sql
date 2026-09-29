/*
    002_SeedPlatformData.sql
    ZentavioCRM — Platform (master) database seed.

    Not required for the app to run: the Platform database starts empty, and every tenant
    created through POST /api/platform/tenants (TenantProvisioningService) inserts its own row
    here automatically. Nothing else needs seeding — there are no other reference tables in the
    Platform database yet (no plans/billing — that's a later phase).

    What this script DOES do: registers your existing hand-built tenant database
    ("StaffingManagementSystemDb", created by SQL Changes/Tenant/001_CreateSchema.sql +
    Tenant/002_SeedData.sql) as a real tenant, subdomain "default". Without this row, that
    database only keeps working through the
    Tenancy:DefaultTenantConnectionStringName fallback (bare http://localhost with no tenant
    header resolves straight to it, bypassing the Platform database lookup entirely). Run this if
    you want to actually exercise tenant resolution end-to-end instead of relying on that fallback —
    e.g. to confirm the X-Tenant header / subdomain path works before provisioning real tenants.

    To test it after running this:
      - Frontend: set VITE_TENANT_SUBDOMAIN=default (or browse via http://default.localhost:5173,
        which resolves automatically on modern OS/browsers without editing your hosts file).
      - Direct API call: send header "X-Tenant: default".
    Either way the request should resolve to this row and connect to StaffingManagementSystemDb.

    Run 001_CreatePlatformDatabase.sql first. Safe to re-run — insert is guarded.

    IMPORTANT — DatabaseName below is a placeholder, not a literal value to copy as-is:
    TenantUsageService and TenantResolutionMiddleware both build a live connection string as
    "Database={tenant.DatabaseName}", so this column must hold the ACTUAL SQL Server database name
    (e.g. "ZentavioCRM" locally, "itmuske1_ZentavioCRM" on the itmusketeers host) — never the
    ConnectionStrings:* config KEY. Seeding the literal string "StaffingManagementSystemDb" here
    broke GetUsageAsync in production (tried to connect to a database that doesn't exist) and had
    to be corrected/removed. Edit the VALUES below to your own environment's real database name
    before running this script, and don't run it at all against an environment where a tenant
    already covers that same database (DatabaseName has a unique index — a duplicate will fail,
    or worse, silently shadow the real tenant if the insert is ever changed to an upsert).
*/

USE [ZentavioCRM_Platform];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Tenants WHERE Subdomain = N'default')
BEGIN
    INSERT INTO dbo.Tenants (Name, Subdomain, DatabaseName, Status, AdminEmail, CreatedAtUtc, ActivatedAtUtc)
    VALUES (
        N'My Company',
        N'default',
        N'ZentavioCRM', -- Replace with YOUR SQL Server's actual database name for StaffingManagementSystemDb.
        N'Active',
        N'admin@zentaviocrm.com',
        '2026-01-01T00:00:00',
        '2026-01-01T00:00:00'
    );
END
GO
