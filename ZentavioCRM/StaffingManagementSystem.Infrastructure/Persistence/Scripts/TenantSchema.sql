/*
    TenantSchema.sql — embedded resource, executed by TenantProvisioningService against a
    freshly created tenant database via raw ADO.NET (split on "GO" batch separators).

    This is the SAME schema as "SQL Changes/001_CreateSchema.sql" at the repo root, with the
    "USE [StaffingManagementSystemDb];" header removed (the ADO.NET connection string already
    targets the newly created tenant database, so a USE statement here would be wrong — and
    dangerous, since it would silently redirect DDL to whatever database name happened to match).

    >>> If you change the schema, update BOTH this file and SQL Changes/001_CreateSchema.sql. <<<
*/

-- ============================================================================
-- Companies
-- ============================================================================
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
GO

-- ============================================================================
-- Permissions
-- ============================================================================
CREATE TABLE dbo.Permissions
(
    Id     UNIQUEIDENTIFIER NOT NULL,
    Code   NVARCHAR(100)    NOT NULL,
    Name   NVARCHAR(150)    NOT NULL,
    Module NVARCHAR(100)    NOT NULL,
    CONSTRAINT PK_Permissions PRIMARY KEY CLUSTERED (Id)
);

CREATE UNIQUE INDEX IX_Permissions_Code ON dbo.Permissions (Code);
GO

-- ============================================================================
-- Roles
-- ============================================================================
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
GO

-- ============================================================================
-- Departments (self-referencing hierarchy under Companies)
-- ============================================================================
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
GO

-- ============================================================================
-- Users
-- ============================================================================
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
GO

-- ============================================================================
-- RolePermissions (join table — Role <-> Permission)
-- ============================================================================
CREATE TABLE dbo.RolePermissions
(
    RoleId       UNIQUEIDENTIFIER NOT NULL,
    PermissionId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_RolePermissions PRIMARY KEY CLUSTERED (RoleId, PermissionId),
    CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (Id) ON DELETE CASCADE,
    CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES dbo.Permissions (Id) ON DELETE CASCADE
);

CREATE INDEX IX_RolePermissions_PermissionId ON dbo.RolePermissions (PermissionId);
GO

-- ============================================================================
-- Customers (Customer Master)
-- ============================================================================
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
GO

-- ============================================================================
-- ContactPersons (unlimited contacts per Customer)
-- ============================================================================
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
GO

-- ============================================================================
-- CustomerAddresses (multiple addresses per Customer)
-- ============================================================================
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
GO

-- ============================================================================
-- Leads
-- ============================================================================
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
GO

-- ============================================================================
-- Opportunities (deal pipeline — sits between Customers and the future Quotation/Sales
-- Order modules in the Lead-to-Customer journey; see CRM_SRS Phase 6, section 4)
-- ============================================================================
CREATE TABLE dbo.Opportunities
(
    Id                UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Opportunities_Id DEFAULT NEWID(),
    OpportunityNumber NVARCHAR(30)     NOT NULL,
    Name              NVARCHAR(200)    NOT NULL,
    CustomerId        UNIQUEIDENTIFIER NOT NULL,
    Value             DECIMAL(18, 2)   NULL,
    Probability       INT              NULL,
    Products          NVARCHAR(1000)   NULL,
    Competitors       NVARCHAR(500)    NULL,
    ExpectedCloseDate DATETIME2        NULL,
    Stage             NVARCHAR(30)     NOT NULL,
    AssignedToUserId  UNIQUEIDENTIFIER NULL,
    SourceLeadId      UNIQUEIDENTIFIER NULL,
    Notes             NVARCHAR(MAX)    NULL,
    LostReason        NVARCHAR(300)    NULL,
    ClosedAtUtc       DATETIME2        NULL,
    CreatedByUserId   UNIQUEIDENTIFIER NULL,
    CreatedAtUtc      DATETIME2        NOT NULL,
    UpdatedAtUtc      DATETIME2        NULL,
    CONSTRAINT PK_Opportunities PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Opportunities_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Opportunities_AssignedToUser FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users (Id) ON DELETE SET NULL,
    CONSTRAINT FK_Opportunities_SourceLead FOREIGN KEY (SourceLeadId) REFERENCES dbo.Leads (Id) ON DELETE SET NULL
);

CREATE UNIQUE INDEX IX_Opportunities_OpportunityNumber ON dbo.Opportunities (OpportunityNumber);
CREATE INDEX IX_Opportunities_CustomerId ON dbo.Opportunities (CustomerId);
CREATE INDEX IX_Opportunities_Stage ON dbo.Opportunities (Stage);
CREATE INDEX IX_Opportunities_AssignedToUserId ON dbo.Opportunities (AssignedToUserId);
GO

-- ============================================================================
-- Activities (generic timeline shared by Leads, Customers, Opportunities, and future modules)
-- ============================================================================
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
GO
