/*
    001_CreateSchema.sql
    ZentavioCRM — Foundation + Leads milestone. This is a TENANT database schema (one per
    customer company) — see 003_CreatePlatformDatabase.sql for the separate shared Platform
    (master) database that just tracks which tenant databases exist.

    Creates every table added for the Company/Department/Role/Permission/User foundation,
    the Customer master (with Contacts and Addresses), the Lead pipeline, and the generic
    Activity timeline. Column types, lengths, defaults, and keys mirror the EF Core
    configurations in StaffingManagementSystem.Infrastructure/Persistence/Configurations
    exactly, so this script and the C# model stay in lockstep.

    Safe to re-run: every CREATE TABLE is guarded with an existence check.
    Run this before 002_SeedData.sql.

    Target database: whatever "ConnectionStrings:StaffingManagementSystemDb" in
    appsettings.json points to. Adjust the USE statement below if your local DB name differs.

    New tenants no longer need this run by hand: POST /api/platform/tenants
    (TenantProvisioningService) creates a fresh tenant database and applies this same schema
    automatically, from an embedded copy at
    StaffingManagementSystem.Infrastructure/Persistence/Scripts/TenantSchema.sql.
    Keep this file and that embedded copy in sync when the schema changes — this file is the
    human-readable reference and the one to use for manually building a one-off dev database;
    the embedded copy is what actually runs in code.
*/

USE [StaffingManagementSystemDb];
GO

-- ============================================================================
-- Companies
-- ============================================================================
IF OBJECT_ID(N'dbo.Companies', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Companies
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Companies_Id DEFAULT NEWID(),
        Name            NVARCHAR(200)    NOT NULL,
        LegalName       NVARCHAR(200)    NULL,
        Industry        NVARCHAR(100)    NULL,
        Website         NVARCHAR(300)    NULL,
        Email           NVARCHAR(256)    NULL,
        Phone           NVARCHAR(30)     NULL,
        TaxNumber       NVARCHAR(50)     NULL,
        AddressLine1    NVARCHAR(200)    NULL,
        AddressLine2    NVARCHAR(200)    NULL,
        City            NVARCHAR(100)    NULL,
        State           NVARCHAR(100)    NULL,
        Country         NVARCHAR(100)    NULL,
        PostalCode      NVARCHAR(20)     NULL,
        DefaultCurrency NVARCHAR(10)     NOT NULL,
        TimeZone        NVARCHAR(100)    NOT NULL,
        LogoUrl         NVARCHAR(500)    NULL,
        CreatedAtUtc    DATETIME2        NOT NULL,
        UpdatedAtUtc    DATETIME2        NULL,
        CONSTRAINT PK_Companies PRIMARY KEY CLUSTERED (Id)
    );
END
GO

-- ============================================================================
-- Permissions
-- ============================================================================
IF OBJECT_ID(N'dbo.Permissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Permissions
    (
        Id     UNIQUEIDENTIFIER NOT NULL,
        Code   NVARCHAR(100)    NOT NULL,
        Name   NVARCHAR(150)    NOT NULL,
        Module NVARCHAR(100)    NOT NULL,
        CONSTRAINT PK_Permissions PRIMARY KEY CLUSTERED (Id)
    );

    CREATE UNIQUE INDEX IX_Permissions_Code ON dbo.Permissions (Code);
END
GO

-- ============================================================================
-- Roles
-- ============================================================================
IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        Id           UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Roles_Id DEFAULT NEWID(),
        Name         NVARCHAR(100)    NOT NULL,
        Description  NVARCHAR(500)    NULL,
        IsSystemRole BIT              NOT NULL,
        CreatedAtUtc DATETIME2        NOT NULL,
        CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (Id)
    );

    CREATE UNIQUE INDEX IX_Roles_Name ON dbo.Roles (Name);
END
GO

-- ============================================================================
-- Departments (self-referencing hierarchy under Companies)
-- ============================================================================
IF OBJECT_ID(N'dbo.Departments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Departments
    (
        Id                 UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Departments_Id DEFAULT NEWID(),
        CompanyId          UNIQUEIDENTIFIER NOT NULL,
        Name               NVARCHAR(150)    NOT NULL,
        ParentDepartmentId UNIQUEIDENTIFIER NULL,
        IsActive           BIT              NOT NULL CONSTRAINT DF_Departments_IsActive DEFAULT (1),
        CreatedAtUtc       DATETIME2        NOT NULL,
        UpdatedAtUtc       DATETIME2        NULL,
        CONSTRAINT PK_Departments PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Departments_Companies FOREIGN KEY (CompanyId) REFERENCES dbo.Companies (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Departments_ParentDepartment FOREIGN KEY (ParentDepartmentId) REFERENCES dbo.Departments (Id) ON DELETE NO ACTION
    );

    CREATE INDEX IX_Departments_CompanyId_Name ON dbo.Departments (CompanyId, Name);
    CREATE INDEX IX_Departments_ParentDepartmentId ON dbo.Departments (ParentDepartmentId);
END
GO

-- ============================================================================
-- Users
-- ============================================================================
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Users_Id DEFAULT NEWID(),
        EmployeeCode        NVARCHAR(50)     NOT NULL,
        FirstName           NVARCHAR(100)    NOT NULL,
        LastName            NVARCHAR(100)    NOT NULL,
        Email               NVARCHAR(256)    NOT NULL,
        Mobile              NVARCHAR(30)     NULL,
        PasswordHash        NVARCHAR(512)    NOT NULL,
        RoleId              UNIQUEIDENTIFIER NOT NULL,
        DepartmentId        UNIQUEIDENTIFIER NULL,
        ReportingManagerId  UNIQUEIDENTIFIER NULL,
        IsActive            BIT              NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        CreatedAtUtc        DATETIME2        NOT NULL,
        UpdatedAtUtc        DATETIME2        NULL,
        LastLoginAtUtc      DATETIME2        NULL,
        CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Users_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Users_Departments FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Users_ReportingManager FOREIGN KEY (ReportingManagerId) REFERENCES dbo.Users (Id) ON DELETE NO ACTION
    );

    CREATE UNIQUE INDEX IX_Users_EmployeeCode ON dbo.Users (EmployeeCode);
    CREATE UNIQUE INDEX IX_Users_Email ON dbo.Users (Email);
    CREATE INDEX IX_Users_RoleId ON dbo.Users (RoleId);
    CREATE INDEX IX_Users_DepartmentId ON dbo.Users (DepartmentId);
    CREATE INDEX IX_Users_ReportingManagerId ON dbo.Users (ReportingManagerId);
END
GO

-- ============================================================================
-- RolePermissions (join table — Role <-> Permission)
-- ============================================================================
IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RolePermissions
    (
        RoleId       UNIQUEIDENTIFIER NOT NULL,
        PermissionId UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT PK_RolePermissions PRIMARY KEY CLUSTERED (RoleId, PermissionId),
        CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id) ON DELETE CASCADE,
        CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES dbo.Permissions (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_RolePermissions_PermissionId ON dbo.RolePermissions (PermissionId);
END
GO

-- ============================================================================
-- Customers (Customer Master)
-- ============================================================================
IF OBJECT_ID(N'dbo.Customers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Customers
    (
        Id               UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Customers_Id DEFAULT NEWID(),
        CustomerNumber   NVARCHAR(30)     NOT NULL,
        Type             NVARCHAR(30)     NOT NULL,
        LegalName        NVARCHAR(200)    NOT NULL,
        DisplayName      NVARCHAR(200)    NOT NULL,
        Industry         NVARCHAR(100)    NULL,
        Website          NVARCHAR(300)    NULL,
        Email            NVARCHAR(256)    NULL,
        Phone            NVARCHAR(30)     NULL,
        TaxNumber        NVARCHAR(50)     NULL,
        EmployeesCount   INT              NULL,
        AnnualRevenue    DECIMAL(18, 2)   NULL,
        CurrencyCode     NVARCHAR(10)     NOT NULL,
        PaymentTermsDays INT              NULL,
        CreditLimit      DECIMAL(18, 2)   NULL,
        Rating           NVARCHAR(20)     NULL,
        AssignedToUserId UNIQUEIDENTIFIER NULL,
        IsActive         BIT              NOT NULL CONSTRAINT DF_Customers_IsActive DEFAULT (1),
        CreatedAtUtc     DATETIME2        NOT NULL,
        UpdatedAtUtc     DATETIME2        NULL,
        CONSTRAINT PK_Customers PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Customers_AssignedToUser FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users (Id) ON DELETE SET NULL
    );

    CREATE UNIQUE INDEX IX_Customers_CustomerNumber ON dbo.Customers (CustomerNumber);
    CREATE INDEX IX_Customers_DisplayName ON dbo.Customers (DisplayName);
    CREATE INDEX IX_Customers_AssignedToUserId ON dbo.Customers (AssignedToUserId);
END
GO

-- ============================================================================
-- ContactPersons (unlimited contacts per Customer)
-- ============================================================================
IF OBJECT_ID(N'dbo.ContactPersons', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ContactPersons
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_ContactPersons_Id DEFAULT NEWID(),
        CustomerId      UNIQUEIDENTIFIER NOT NULL,
        FirstName       NVARCHAR(100)    NOT NULL,
        LastName        NVARCHAR(100)    NOT NULL,
        Designation     NVARCHAR(100)    NULL,
        Department      NVARCHAR(100)    NULL,
        Email           NVARCHAR(256)    NULL,
        Mobile          NVARCHAR(30)     NULL,
        WhatsApp        NVARCHAR(30)     NULL,
        LinkedIn        NVARCHAR(300)    NULL,
        IsPrimary       BIT              NOT NULL,
        IsDecisionMaker BIT              NOT NULL,
        Notes           NVARCHAR(MAX)    NULL,
        CreatedAtUtc    DATETIME2        NOT NULL,
        CONSTRAINT PK_ContactPersons PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_ContactPersons_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_ContactPersons_CustomerId ON dbo.ContactPersons (CustomerId);
END
GO

-- ============================================================================
-- CustomerAddresses (multiple addresses per Customer)
-- ============================================================================
IF OBJECT_ID(N'dbo.CustomerAddresses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.CustomerAddresses
    (
        Id         UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_CustomerAddresses_Id DEFAULT NEWID(),
        CustomerId UNIQUEIDENTIFIER NOT NULL,
        Type       NVARCHAR(30)     NOT NULL,
        Line1      NVARCHAR(200)    NOT NULL,
        Line2      NVARCHAR(200)    NULL,
        City       NVARCHAR(100)    NULL,
        State      NVARCHAR(100)    NULL,
        Country    NVARCHAR(100)    NULL,
        PostalCode NVARCHAR(20)     NULL,
        IsPrimary  BIT              NOT NULL,
        CONSTRAINT PK_CustomerAddresses PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_CustomerAddresses_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_CustomerAddresses_CustomerId ON dbo.CustomerAddresses (CustomerId);
END
GO

-- ============================================================================
-- Leads
-- ============================================================================
IF OBJECT_ID(N'dbo.Leads', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Leads
    (
        Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Leads_Id DEFAULT NEWID(),
        LeadNumber          NVARCHAR(30)     NOT NULL,
        CompanyName         NVARCHAR(200)    NOT NULL,
        ContactName         NVARCHAR(200)    NOT NULL,
        Email               NVARCHAR(256)    NULL,
        Mobile              NVARCHAR(30)     NULL,
        Industry            NVARCHAR(100)    NULL,
        Source              NVARCHAR(30)     NOT NULL,
        Campaign            NVARCHAR(150)    NULL,
        Budget              DECIMAL(18, 2)   NULL,
        Timeline            NVARCHAR(100)    NULL,
        ExpectedValue       DECIMAL(18, 2)   NULL,
        AssignedToUserId    UNIQUEIDENTIFIER NULL,
        Territory           NVARCHAR(100)    NULL,
        Status              NVARCHAR(30)     NOT NULL,
        LeadScore           INT              NULL,
        AiScore             INT              NULL,
        Notes               NVARCHAR(MAX)    NULL,
        LostReason          NVARCHAR(300)    NULL,
        ConvertedCustomerId UNIQUEIDENTIFIER NULL,
        ConvertedAtUtc      DATETIME2        NULL,
        -- No FK: CreatedByUserId is an audit-trail scalar only (no navigation property on Lead),
        -- matching the EF Core model — intentionally does not cascade or restrict on user deletion.
        CreatedByUserId     UNIQUEIDENTIFIER NULL,
        CreatedAtUtc        DATETIME2        NOT NULL,
        UpdatedAtUtc        DATETIME2        NULL,
        CONSTRAINT PK_Leads PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Leads_AssignedToUser FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users (Id) ON DELETE SET NULL,
        CONSTRAINT FK_Leads_ConvertedCustomer FOREIGN KEY (ConvertedCustomerId) REFERENCES dbo.Customers (Id) ON DELETE SET NULL
    );

    CREATE UNIQUE INDEX IX_Leads_LeadNumber ON dbo.Leads (LeadNumber);
    CREATE INDEX IX_Leads_Status ON dbo.Leads (Status);
    CREATE INDEX IX_Leads_AssignedToUserId ON dbo.Leads (AssignedToUserId);
    CREATE INDEX IX_Leads_ConvertedCustomerId ON dbo.Leads (ConvertedCustomerId);
END
GO

-- ============================================================================
-- Activities (generic timeline shared by Leads, Customers, and future modules)
-- ============================================================================
IF OBJECT_ID(N'dbo.Activities', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Activities
    (
        Id               UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Activities_Id DEFAULT NEWID(),
        Type             NVARCHAR(30)     NOT NULL,
        Subject          NVARCHAR(200)    NOT NULL,
        Description      NVARCHAR(2000)   NULL,
        RelatedToType    NVARCHAR(30)     NOT NULL,
        RelatedToId      UNIQUEIDENTIFIER NOT NULL,
        DueAtUtc         DATETIME2        NULL,
        CompletedAtUtc   DATETIME2        NULL,
        AssignedToUserId UNIQUEIDENTIFIER NULL,
        CreatedByUserId  UNIQUEIDENTIFIER NULL,
        CreatedAtUtc     DATETIME2        NOT NULL,
        CONSTRAINT PK_Activities PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Activities_AssignedToUser FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users (Id) ON DELETE SET NULL,
        CONSTRAINT FK_Activities_CreatedByUser FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (Id) ON DELETE SET NULL
    );

    CREATE INDEX IX_Activities_RelatedToType_RelatedToId ON dbo.Activities (RelatedToType, RelatedToId);
END
GO
