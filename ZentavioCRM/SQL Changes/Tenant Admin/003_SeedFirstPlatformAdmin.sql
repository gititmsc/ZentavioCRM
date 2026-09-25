/*
    003_SeedFirstPlatformAdmin.sql
    ZentavioCRM — Platform (master) database seed.

    Seeds exactly one Platform Admin account so there's a way to log into the Super Admin panel
    at all. There's deliberately no self-service registration for platform admins (see
    PlatformAdminsController) — the very first account has to come from here; every account after
    that is created by an already-logged-in platform admin via POST /api/platform/admins.

    Run 001_CreatePlatformDatabase.sql first (creates dbo.PlatformAdmins). Safe to re-run —
    the insert is guarded on email.

    Login: platformadmin@zentaviocrm.com / PlatformAdmin@123
    >>> Change this password immediately after first login (or create your own admin via
        POST /api/platform/admins and deactivate/ignore this one) — this password is sitting in
        source control, exactly like the tenant-side default admin seed in
        SQL Changes/Tenant/002_SeedData.sql. <<<

    The password hash below is PBKDF2-HMAC-SHA256, 100,000 iterations, stored as
    "{iterations}.{saltBase64}.{hashBase64}" — generated the same way
    ZentavioCRM.Infrastructure.Security.PasswordHasher does, and verified against that exact
    algorithm before being committed here.
*/

USE [ZentavioCRM_Platform];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PlatformAdmins WHERE Email = N'platformadmin@zentaviocrm.com')
BEGIN
    INSERT INTO dbo.PlatformAdmins (Email, PasswordHash, FirstName, LastName, IsActive, CreatedAtUtc)
    VALUES (
        N'platformadmin@zentaviocrm.com',
        N'100000.ziFlWcES53f9xnaRj5sh3w==.qikcVS/8IdN3wcpeHEp4R6B6CdZ9ZXiiXaZJ/ZhRLZ4=',
        N'Platform',
        N'Administrator',
        1,
        '2026-01-01T00:00:00'
    );
END
GO
