/*
    TenantRbacSeed.sql — embedded resource, executed by TenantProvisioningService right after
    TenantSchema.sql against the same freshly created tenant database.

    Seeds only the reference data that's identical for every tenant: the 21 Permissions and the
    4 built-in Roles + their grants (same fixed GUIDs as ZentavioCRM.Core.Common.SeedIds — safe to
    reuse across tenants since each lives in its own physical database). The Company profile and
    the first Admin user are NOT here — those are tenant-specific and inserted by
    TenantProvisioningService itself via EF Core, using the real values from ProvisionTenantRequest
    and IPasswordHasher for the admin's actual password.

    >>> If you change roles/permissions, update BOTH this file and SQL Changes/002_SeedData.sql. <<<
*/

-- ============================================================================
-- Permissions (33 total, grouped by module — matches Core.Common.PermissionCodes)
-- ============================================================================
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
    ('10000000-0000-0000-0000-000000000010', N'Leads.Convert',       N'Convert', N'Leads'),
    ('10000000-0000-0000-0000-000000000011', N'Opportunities.View',    N'View',    N'Opportunities'),
    ('10000000-0000-0000-0000-000000000012', N'Opportunities.Create',  N'Create',  N'Opportunities'),
    ('10000000-0000-0000-0000-000000000013', N'Opportunities.Edit',    N'Edit',    N'Opportunities'),
    ('10000000-0000-0000-0000-000000000014', N'Opportunities.Delete',  N'Delete',  N'Opportunities'),
    ('10000000-0000-0000-0000-000000000015', N'Opportunities.Assign',  N'Assign',  N'Opportunities'),
    ('10000000-0000-0000-0000-000000000016', N'Quotations.View',       N'View',    N'Quotations'),
    ('10000000-0000-0000-0000-000000000017', N'Quotations.Create',     N'Create',  N'Quotations'),
    ('10000000-0000-0000-0000-000000000018', N'Quotations.Edit',       N'Edit',    N'Quotations'),
    ('10000000-0000-0000-0000-000000000019', N'Quotations.Delete',     N'Delete',  N'Quotations'),
    ('10000000-0000-0000-0000-00000000001a', N'Quotations.Assign',     N'Assign',  N'Quotations'),
    ('10000000-0000-0000-0000-00000000001b', N'SalesOrders.View',      N'View',    N'SalesOrders'),
    ('10000000-0000-0000-0000-00000000001c', N'SalesOrders.Create',    N'Create',  N'SalesOrders'),
    ('10000000-0000-0000-0000-00000000001d', N'SalesOrders.Edit',      N'Edit',    N'SalesOrders'),
    ('10000000-0000-0000-0000-00000000001f', N'SalesOrders.Assign',    N'Assign',  N'SalesOrders');
-- No SalesOrders.Delete: there is no delete feature for Sales Orders (Cancel is the
-- "this order is void" action instead) — see Core.Common.PermissionCodes for the rationale.
GO

-- ============================================================================
-- Roles
-- ============================================================================
INSERT INTO dbo.Roles (Id, Name, Description, IsSystemRole, CreatedAtUtc) VALUES
    ('20000000-0000-0000-0000-000000000001', N'Administrator',   N'Full access to every module and setting.',                  1, SYSUTCDATETIME()),
    ('20000000-0000-0000-0000-000000000002', N'Sales Manager',   N'Manages the sales team''s customers and lead pipeline.',    1, SYSUTCDATETIME()),
    ('20000000-0000-0000-0000-000000000003', N'Sales Executive', N'Works leads and customers assigned to them.',               1, SYSUTCDATETIME()),
    ('20000000-0000-0000-0000-000000000004', N'Support Agent',   N'Read-only access to customers and leads.',                  1, SYSUTCDATETIME());
GO

-- ============================================================================
-- RolePermissions
-- ============================================================================
-- Administrator: every permission in the system.
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT '20000000-0000-0000-0000-000000000001', Id FROM dbo.Permissions;

-- Sales Manager: full Customers/Leads/Opportunities/Quotations/SalesOrders, plus visibility into Departments/Users.
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT '20000000-0000-0000-0000-000000000002', Id FROM dbo.Permissions
WHERE Code IN (
    N'Departments.View', N'Users.View',
    N'Customers.View', N'Customers.Create', N'Customers.Edit', N'Customers.Delete',
    N'Leads.View', N'Leads.Create', N'Leads.Edit', N'Leads.Delete', N'Leads.Assign', N'Leads.Convert',
    N'Opportunities.View', N'Opportunities.Create', N'Opportunities.Edit', N'Opportunities.Delete', N'Opportunities.Assign',
    N'Quotations.View', N'Quotations.Create', N'Quotations.Edit', N'Quotations.Delete', N'Quotations.Assign',
    N'SalesOrders.View', N'SalesOrders.Create', N'SalesOrders.Edit', N'SalesOrders.Assign'
);

-- Sales Executive: day-to-day CRUD, no deletes.
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT '20000000-0000-0000-0000-000000000003', Id FROM dbo.Permissions
WHERE Code IN (
    N'Customers.View', N'Customers.Create', N'Customers.Edit',
    N'Leads.View', N'Leads.Create', N'Leads.Edit', N'Leads.Assign', N'Leads.Convert',
    N'Opportunities.View', N'Opportunities.Create', N'Opportunities.Edit', N'Opportunities.Assign',
    N'Quotations.View', N'Quotations.Create', N'Quotations.Edit', N'Quotations.Assign',
    N'SalesOrders.View', N'SalesOrders.Create', N'SalesOrders.Edit', N'SalesOrders.Assign'
);

-- Support Agent: read-only.
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT '20000000-0000-0000-0000-000000000004', Id FROM dbo.Permissions
WHERE Code IN (N'Customers.View', N'Leads.View', N'Opportunities.View', N'Quotations.View', N'SalesOrders.View');
GO
