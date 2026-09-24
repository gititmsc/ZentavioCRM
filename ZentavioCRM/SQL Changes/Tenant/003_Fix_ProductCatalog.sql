/*
    003_Fix_ProductCatalog.sql

    Fixes: "Product Catalog" nav item missing, and no products showing in the
    Quotation/Opportunity line-item picker.

    Root cause: the Products table and Products permissions were added to
    001_CreateSchema.sql and 002_SeedData.sql (this same folder) during
    development, but those scripts were never re-run against the live database
    (Server=80.65.208.158, Database=itmuske1_ZentavioCRM). So the Products
    table doesn't exist yet, and no role — including Administrator — has been
    granted the Products.* permissions, which is why the nav item is hidden
    and the picker comes back empty.

    This script is safe to run — every statement is guarded (IF OBJECT_ID IS NULL /
    WHERE NOT EXISTS), so it only adds what's missing and won't touch or duplicate
    anything that already exists.

    How to run:
      1. Open SSMS, Azure Data Studio, or any SQL Server client from a machine that
         can reach 80.65.208.158 (this sandbox's network is restricted and can't).
      2. Connect using the credentials in
         StaffingManagementSystem.api/appsettings.json -> ConnectionStrings:StaffingManagementSystemDb
         (Server=80.65.208.158; Database=itmuske1_ZentavioCRM; User Id=itmuske1_ZentavioCRM).
      3. Run this entire script against that database.
      4. In the app, log out and log back in (so your JWT picks up the new
         Products.View permission), then refresh the page.
*/

-- ============================================================================
-- 1) Products table
-- ============================================================================
IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Products
    (
        Id               UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Products_Id DEFAULT NEWID(),
        Sku              NVARCHAR(50)     NOT NULL,
        Name             NVARCHAR(200)    NOT NULL,
        Type             INT              NOT NULL,
        Category         NVARCHAR(100)    NULL,
        Brand            NVARCHAR(100)    NULL,
        UnitOfMeasure    NVARCHAR(50)     NULL,
        UnitPrice        DECIMAL(18,2)    NOT NULL,
        Cost             DECIMAL(18,2)    NULL,
        TaxPercent       DECIMAL(5,2)     NULL,
        Description      NVARCHAR(2000)   NULL,
        DurationMinutes  INT              NULL,
        BillingType      NVARCHAR(50)     NULL,
        IsActive         BIT              NOT NULL CONSTRAINT DF_Products_IsActive DEFAULT (1),
        CreatedAtUtc     DATETIME2        NOT NULL,
        UpdatedAtUtc     DATETIME2        NULL,
        CONSTRAINT PK_Products PRIMARY KEY CLUSTERED (Id)
    );

    CREATE UNIQUE INDEX IX_Products_Sku ON dbo.Products (Sku);
    CREATE INDEX IX_Products_Name ON dbo.Products (Name);
END
GO

-- ============================================================================
-- 2) Products permissions
-- ============================================================================
INSERT INTO dbo.Permissions (Id, Code, Name, Module)
SELECT v.Id, v.Code, v.Name, v.Module
FROM (VALUES
    ('10000000-0000-0000-0000-000000000022', N'Products.View',   N'View',   N'Products'),
    ('10000000-0000-0000-0000-000000000023', N'Products.Create', N'Create', N'Products'),
    ('10000000-0000-0000-0000-000000000024', N'Products.Edit',   N'Edit',   N'Products'),
    ('10000000-0000-0000-0000-000000000025', N'Products.Delete', N'Delete', N'Products')
) AS v(Id, Code, Name, Module)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Permissions p WHERE p.Id = v.Id);
GO

-- ============================================================================
-- 3) RolePermissions — grant Products.* to the built-in roles
--    (Administrator gets it automatically via the "every permission" rule below;
--     the other three are scoped the same way as every other module.)
-- ============================================================================
DECLARE @AdminRoleId UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000001';
DECLARE @SalesManagerRoleId UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000002';
DECLARE @SalesExecutiveRoleId UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000003';
DECLARE @SupportAgentRoleId UNIQUEIDENTIFIER = '20000000-0000-0000-0000-000000000004';

-- Administrator: every permission in the system (top-up — only adds rows it doesn't already have).
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT @AdminRoleId, p.Id FROM dbo.Permissions p
WHERE NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId = @AdminRoleId AND rp.PermissionId = p.Id);

-- Sales Manager: full Products CRUD.
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT @SalesManagerRoleId, p.Id FROM dbo.Permissions p
WHERE p.Code IN (N'Products.View', N'Products.Create', N'Products.Edit', N'Products.Delete')
AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId = @SalesManagerRoleId AND rp.PermissionId = p.Id);

-- Sales Executive: View/Create/Edit, no delete.
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT @SalesExecutiveRoleId, p.Id FROM dbo.Permissions p
WHERE p.Code IN (N'Products.View', N'Products.Create', N'Products.Edit')
AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId = @SalesExecutiveRoleId AND rp.PermissionId = p.Id);

-- Support Agent: read-only.
INSERT INTO dbo.RolePermissions (RoleId, PermissionId)
SELECT @SupportAgentRoleId, p.Id FROM dbo.Permissions p
WHERE p.Code = N'Products.View'
AND NOT EXISTS (SELECT 1 FROM dbo.RolePermissions rp WHERE rp.RoleId = @SupportAgentRoleId AND rp.PermissionId = p.Id);
GO

-- ============================================================================
-- Verify
-- ============================================================================
SELECT OBJECT_ID(N'dbo.Products', N'U') AS ProductsTableExists;
SELECT * FROM dbo.Permissions WHERE Module = N'Products';
SELECT r.Name AS RoleName, p.Code AS PermissionCode
FROM dbo.RolePermissions rp
JOIN dbo.Roles r ON r.Id = rp.RoleId
JOIN dbo.Permissions p ON p.Id = rp.PermissionId
WHERE p.Module = N'Products'
ORDER BY r.Name, p.Code;
