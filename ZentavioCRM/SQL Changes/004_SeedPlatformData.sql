/*
    004_SeedPlatformData.sql
    ZentavioCRM — Platform (master) database seed.

    Not required for the app to run: the Platform database starts empty, and every tenant
    created through POST /api/platform/tenants (TenantProvisioningService) inserts its own row
    here automatically. Nothing else needs seeding — there are no other reference tables in the
    Platform database yet (no plans/billing — that's a later phase).

    What this script DOES do: registers your existing hand-built tenant database
    ("StaffingManagementSystemDb", created by 001_CreateSchema.sql + 002_SeedData.sql) as a real
    tenant, subdomain "default". Without this row, that database only keeps working through the
    Tenancy:DefaultTenantConnectionStringName fallback (bare http://localhost with no tenant
    header resolves straight to it, bypassing the Platform database lookup entirely). Run this if
    you want to actually exercise tenant resolution end-to-end instead of relying on that fallback —
    e.g. to confirm the X-Tenant header / subdomain path works before provisioning real tenants.

    To test it after running this:
      - Frontend: set VITE_TENANT_SUBDOMAIN=default (or browse via http://default.localhost:5173,
        which resolves automatically on modern OS/browsers without editing your hosts file).
      - Direct API call: send header "X-Tenant: default".
    Either way the request should resolve to this row and connect to StaffingManagementSystemDb.

    Run 003_CreatePlatformDatabase.sql first. Safe to re-run — insert is guarded.
*/

USE [ZentavioCRM_Platform];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Tenants WHERE Subdomain = N'default')
BEGIN
    INSERT INTO dbo.Tenants (Name, Subdomain, DatabaseName, Status, AdminEmail, CreatedAtUtc, ActivatedAtUtc)
    VALUES (
        N'My Company',
        N'default',
        N'StaffingManagementSystemDb',
        N'Active',
        N'admin@zentaviocrm.com',
        '2026-01-01T00:00:00',
        '2026-01-01T00:00:00'
    );
END
GO
