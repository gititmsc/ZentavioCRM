/*
    002_SeedData.sql
    ZentavioCRM — Foundation + Leads milestone.

    Seeds the same rows the EF Core model produces via
    StaffingManagementSystem.Infrastructure/Persistence/Seed/PlatformSeedData.cs:
    the default Company + Department, all 16 Permissions, the 4 built-in Roles,
    their RolePermission grants, and one Admin user.

    All IDs match ZentavioCRM.Core.Common.SeedIds exactly, so if you ever do switch
    to EF migrations later, this data and the C# seeder agree and won't collide.

    Run 001_CreateSchema.sql first. Safe to re-run — every insert is guarded
    with a "not already present" check.

    Default admin login: admin@zentaviocrm.com / Admin@123
    >>> Change this password immediately after first login. <<<

    This is a manual/reference seed for a single hand-built tenant database (useful for local
    dev). Real tenants created via POST /api/platform/tenants get their Company/Department/Admin
    values from the request instead (real company name, real admin email/password) — see
    TenantProvisioningService — while the Permissions/Roles/RolePermissions portion below is
    identical to the embedded StaffingManagementSystem.Infrastructure/Persistence/Scripts/TenantRbacSeed.sql
    every automatically-provisioned tenant gets. Keep those two in sync when roles/permissions change.
*/

USE [StaffingManagementSystemDb];
GO

DECLARE @SeedTimestamp DATETIME2 = '2026-01-01T00:00:00';
DECLARE @CompanyId UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000001';

-- ============================================================================
-- Company
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Id = @CompanyId)
BEGIN
    INSERT INTO dbo.Companies (Id, Name, DefaultCurrency, TimeZone, CreatedAtUtc)
    VALUES (@CompanyId, N'My Company', N'USD', N'UTC', @SeedTimestamp);
END
GO

-- ============================================================================
-- Department
-- ============================================================================
DECLARE @CompanyId2 UNIQUEIDENTIFIER = '30000000-0000-0000-0000-000000000001';
DECLARE @DepartmentId2 UNIQUEIDENTIFIER = '40000000-0000-0000-0000-000000000001';
DECLARE @SeedTimestamp2 DATETIME2 = '2026-01-01T00:00:00';

IF NOT EXISTS (SELECT 1 FROM dbo.Departments WHERE Id = @DepartmentId2)
BEGIN
    INSERT INTO dbo.Departments (Id, CompanyId, Name, IsActive, CreatedAtUtc)
    VALUES (@DepartmentId2, @CompanyId2, N'Sales', 1, @SeedTimestamp2);
END
GO

-- ============================================================================
-- Permissions (16 total, grouped by module — matches Core.Common.PermissionCodes)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.Permissions WHERE Id = '10000000-0000-0000-0000-000000000001')
BEGIN
    INSERT INTO dbo.Permissions (Id, Code, Name, Module) VALUES
        ('10000000-0000-0000-0000-000000000001', N'Departments.View',    N'View',    N'Departments'),
        ('10000000-0000-0000-0000-000000000002', N'Departments.Manage',  N'Manage',  N'Departments'),
        ('10000000-0000-0000-0000-000000000003', N'Users.View',          N'View',    N'Users'),
        ('10000000-0000-0000-0000-000000000004', N'Users.Manage',        N'Manage',  N'Users'),
        ('10000000-0000-0000-0000-000000000005', N'Roles.View',          N'View',    N'Roles'),
        ('10000000-0000-0000-0000-000000000006', N'Roles.Manage',        N'Manage',  N'Roles'),
        ('10000000-0000-0000-0000-000000000007', N'Customers.View',      N'View',    N'Customers'),
        ('10000000-0000-0000-0000-000000000008', N'Customers.Create',    N'Create',  N'Customers'),
        ('10000000-0000-0000-0000-000000000009', N'Customers.Edit',      N'Edit',    N'Customers'),
        ('10000000-0000-0000-0000-00000000000a', N'Customers.Delete',    N'Delete',  N'Customers'),
        ('10000000-0000-0000-0000-00000000000b', N'Leads.View',          N'View',    N'Leads'),
        ('10000000-0000-0000-0000-00000000000c', N'Leads.Create',        N'Create',  N'Leads'),
        ('10000000-0000-0000-0000-00000000000d', N'Leads.Edit',          N'Edit',    N'Leads'),
        ('10000000-0000-0000-0000-00000000000e', N'Leads.Delete',        N'Delete',  N'Leads'),
        ('10000000-0000-0000-0000-00000000000f', N'Leads.Assign',        N'Assign',  N'Leads'),
        ('10000000-0000-0000-0000-000000000010', N'Leads.Convert',       N'Convert', N'Leads');
END
GO

-- ============================================================================
-- Roles
-- ============================================================================
DECLARE @AdminRoleId2 UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000001';
DECLARE @SeedTimestamp3 DATETIME2 = '2026-01-01T00:00:00';

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Id = @AdminRoleId2)
BEGIN
    INSERT INTO dbo.Roles (Id, Name, Description, IsSystemRole, CreatedAtUtc) VALUES
        ('20000000-0000-0000-0000-000000000001', N'Administrator',   N'Full access to every module and setting.',                  1, @SeedTimestamp3),
        ('20000000-0000-0000-0000-000000000002', N'Sales Manager',   N'Manages the sales team''s customers and lead pipeline.',    1, @SeedTimestamp3),
        ('20000000-0000-0000-0000-000000000003', N'Sales Executive', N'Works leads and customers assigned to them.',               1, @SeedTimestamp3),
        ('20000000-0000-0000-0000-000000000004', N'Support Agent',   N'Read-only access to customers and leads.',                  1, @SeedTimestamp3);
END
GO

-- ============================================================================
-- RolePermissions
-- ============================================================================
DECLARE @AdminRoleId3 UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000001';
DECLARE @SalesManagerRoleId3 UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000002';
DECLARE @SalesExecutiveRoleId3 UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000003';
DECLARE @SupportAgentRoleId3 UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000004';

IF NOT EXISTS (SELECT 1 FROM dbo.RolePermissions WHERE RoleId = @AdminRoleId3)
BEGIN
    -- Administrator: every permission in the system.
    INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
    SELECT @AdminRoleId3, Id FROM dbo.Permissions;

    -- Sales Manager: full Customers/Leads, plus visibility into Departments/Users.
    INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
    SELECT @SalesManagerRoleId3, Id FROM dbo.Permissions
    WHERE Code IN (
        N'Departments.View', N'Users.View',
        N'Customers.View', N'Customers.Create', N'Customers.Edit', N'Customers.Delete',
        N'Leads.View', N'Leads.Create', N'Leads.Edit', N'Leads.Delete', N'Leads.Assign', N'Leads.Convert'
    );

    -- Sales Executive: day-to-day CRUD, no deletes.
    INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
    SELECT @SalesExecutiveRoleId3, Id FROM dbo.Permissions
    WHERE Code IN (
        N'Customers.View', N'Customers.Create', N'Customers.Edit',
        N'Leads.View', N'Leads.Create', N'Leads.Edit', N'Leads.Assign', N'Leads.Convert'
    );

    -- Support Agent: read-only.
    INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
    SELECT @SupportAgentRoleId3, Id FROM dbo.Permissions
    WHERE Code IN (N'Customers.View', N'Leads.View');
END
GO

-- ============================================================================
-- Default Admin user
-- Login: admin@zentaviocrm.com / Admin@123  (PBKDF2-SHA256, 100,000 iterations —
-- generated the same way ZentavioCRM.Infrastructure.Security.PasswordHasher does)
-- ============================================================================
DECLARE @AdminUserId2 UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000001';
DECLARE @AdminRoleId4 UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000001';
DECLARE @DepartmentId3 UNIQUEIDENTIFIER = '40000000-0000-0000-0000-000000000001';
DECLARE @SeedTimestamp4 DATETIME2 = '2026-01-01T00:00:00';

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @AdminUserId2)
BEGIN
    INSERT INTO dbo.Users (Id, EmployeeCode, FirstName, LastName, Email, PasswordHash, RoleId, DepartmentId, IsActive, CreatedAtUtc)
    VALUES (
        @AdminUserId2,
        N'EMP-0001',
        N'System',
        N'Administrator',
        N'admin@zentaviocrm.com',
        N'100000.gH7ZXrg9PRCjHhBJrL8z0g==.O3iZBLoIKRGP09JaxOc7dNAqvM8orTypo6ti+p3wQOs=',
        @AdminRoleId4,
        @DepartmentId3,
        1,
        @SeedTimestamp4
    );
END
GO
