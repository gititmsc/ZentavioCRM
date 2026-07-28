/*
    001_CreateSchema.sql
    ZentavioCRM — Foundation + Leads milestone. This is a TENANT database schema (one per
    customer company) — see 003_CreatePlatformDatabase.sql for the separate shared Platform
    (master) database that just tracks which tenant databases exist.

    Creates every table added for the Company/Department/Role/Permission/User foundation,
    the Customer master (with Contacts and Addresses), the Lead pipeline, the Opportunity
    pipeline (SRS Phase 6, section 4 "Opportunity Management"), and the generic Activity
    timeline. Column types, lengths, defaults, and keys mirror the EF Core configurations in
    StaffingManagementSystem.Infrastructure/Persistence/Configurations exactly, so this script
    and the C# model stay in lockstep.

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
        Tags             NVARCHAR(500)    NULL,
        AcquisitionSource NVARCHAR(30)    NULL,
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

-- Customers.Tags / AcquisitionSource — added post-launch; guarded ALTER for a Customers table
-- that already existed from an earlier run of this script, same pattern as Opportunities.NextStep above.
IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL AND COL_LENGTH('dbo.Customers', 'Tags') IS NULL
BEGIN
    ALTER TABLE dbo.Customers ADD Tags NVARCHAR(500) NULL;
END
GO

IF OBJECT_ID(N'dbo.Customers', N'U') IS NOT NULL AND COL_LENGTH('dbo.Customers', 'AcquisitionSource') IS NULL
BEGIN
    ALTER TABLE dbo.Customers ADD AcquisitionSource NVARCHAR(30) NULL;
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
        NextFollowUpDate    DATETIME2        NULL,
        FollowUpReminderSentAtUtc DATETIME2  NULL,
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

-- Leads.NextFollowUpDate / FollowUpReminderSentAtUtc — added post-launch; guarded ALTER for a
-- Leads table that already existed from an earlier run of this script.
IF OBJECT_ID(N'dbo.Leads', N'U') IS NOT NULL AND COL_LENGTH('dbo.Leads', 'NextFollowUpDate') IS NULL
BEGIN
    ALTER TABLE dbo.Leads ADD NextFollowUpDate DATETIME2 NULL;
END
GO

IF OBJECT_ID(N'dbo.Leads', N'U') IS NOT NULL AND COL_LENGTH('dbo.Leads', 'FollowUpReminderSentAtUtc') IS NULL
BEGIN
    ALTER TABLE dbo.Leads ADD FollowUpReminderSentAtUtc DATETIME2 NULL;
END
GO

-- ============================================================================
-- Opportunities (deal pipeline — sits between Customers and the future Quotation/Sales
-- Order modules in the Lead-to-Customer journey; see CRM_SRS Phase 6, section 4)
-- ============================================================================
IF OBJECT_ID(N'dbo.Opportunities', N'U') IS NULL
BEGIN
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
        -- No FK: CreatedByUserId is an audit-trail scalar only (no navigation property), same
        -- convention as Leads.CreatedByUserId — intentionally does not cascade/restrict on user deletion.
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
END
GO

-- ============================================================================
-- Opportunities.NextStep / NextStepDate (added post-launch — the Opportunities table
-- above may already exist from an earlier run of this script without these columns,
-- so this is a separate idempotent ALTER guarded by column existence, not table existence).
-- ============================================================================
IF OBJECT_ID(N'dbo.Opportunities', N'U') IS NOT NULL AND COL_LENGTH('dbo.Opportunities', 'NextStep') IS NULL
BEGIN
    ALTER TABLE dbo.Opportunities ADD NextStep NVARCHAR(300) NULL;
END
GO

IF OBJECT_ID(N'dbo.Opportunities', N'U') IS NOT NULL AND COL_LENGTH('dbo.Opportunities', 'NextStepDate') IS NULL
BEGIN
    ALTER TABLE dbo.Opportunities ADD NextStepDate DATETIME2 NULL;
END
GO

-- ============================================================================
-- OpportunityLineItems (optional priced line items on an Opportunity; when present,
-- Opportunities.Value is server-computed as the sum of line totals)
-- ============================================================================
IF OBJECT_ID(N'dbo.OpportunityLineItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.OpportunityLineItems
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_OpportunityLineItems_Id DEFAULT NEWID(),
        OpportunityId   UNIQUEIDENTIFIER NOT NULL,
        ProductName     NVARCHAR(200)    NOT NULL,
        Quantity        DECIMAL(18, 2)   NOT NULL,
        UnitPrice       DECIMAL(18, 2)   NOT NULL,
        DiscountPercent DECIMAL(5, 2)    NULL,
        CONSTRAINT PK_OpportunityLineItems PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_OpportunityLineItems_Opportunities FOREIGN KEY (OpportunityId) REFERENCES dbo.Opportunities (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_OpportunityLineItems_OpportunityId ON dbo.OpportunityLineItems (OpportunityId);
END
GO

-- ============================================================================
-- Quotations (priced proposals against an Opportunity — CRM SRS Phase 6, section 5)
-- ============================================================================
IF OBJECT_ID(N'dbo.Quotations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Quotations
    (
        Id                 UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Quotations_Id DEFAULT NEWID(),
        QuotationNumber    NVARCHAR(30)     NOT NULL,
        Version            INT              NOT NULL CONSTRAINT DF_Quotations_Version DEFAULT (1),
        OpportunityId      UNIQUEIDENTIFIER NOT NULL,
        CustomerId         UNIQUEIDENTIFIER NOT NULL,
        Status             NVARCHAR(30)     NOT NULL,
        ValidUntil         DATETIME2        NULL,
        TermsAndConditions NVARCHAR(4000)   NULL,
        Notes              NVARCHAR(MAX)    NULL,
        Subtotal           DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_Quotations_Subtotal DEFAULT (0),
        TaxTotal           DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_Quotations_TaxTotal DEFAULT (0),
        GrandTotal         DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_Quotations_GrandTotal DEFAULT (0),
        AssignedToUserId   UNIQUEIDENTIFIER NULL,
        CreatedByUserId    UNIQUEIDENTIFIER NULL,
        CreatedAtUtc       DATETIME2        NOT NULL,
        UpdatedAtUtc       DATETIME2        NULL,
        CONSTRAINT PK_Quotations PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Quotations_Opportunities FOREIGN KEY (OpportunityId) REFERENCES dbo.Opportunities (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Quotations_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_Quotations_AssignedToUser FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users (Id) ON DELETE SET NULL
    );

    CREATE UNIQUE INDEX IX_Quotations_QuotationNumber_Version ON dbo.Quotations (QuotationNumber, Version);
    CREATE INDEX IX_Quotations_OpportunityId ON dbo.Quotations (OpportunityId);
    CREATE INDEX IX_Quotations_CustomerId ON dbo.Quotations (CustomerId);
    CREATE INDEX IX_Quotations_Status ON dbo.Quotations (Status);
    CREATE INDEX IX_Quotations_AssignedToUserId ON dbo.Quotations (AssignedToUserId);
END
GO

-- ============================================================================
-- QuotationLineItems
-- ============================================================================
IF OBJECT_ID(N'dbo.QuotationLineItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QuotationLineItems
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_QuotationLineItems_Id DEFAULT NEWID(),
        QuotationId     UNIQUEIDENTIFIER NOT NULL,
        ProductName     NVARCHAR(200)    NOT NULL,
        Quantity        DECIMAL(18, 2)   NOT NULL,
        UnitPrice       DECIMAL(18, 2)   NOT NULL,
        DiscountPercent DECIMAL(5, 2)    NULL,
        TaxPercent      DECIMAL(5, 2)    NULL,
        CONSTRAINT PK_QuotationLineItems PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_QuotationLineItems_Quotations FOREIGN KEY (QuotationId) REFERENCES dbo.Quotations (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_QuotationLineItems_QuotationId ON dbo.QuotationLineItems (QuotationId);
END
GO

-- ============================================================================
-- SalesOrders (converted from an Accepted Quotation — CRM SRS Phase 6, section 6)
-- ============================================================================
IF OBJECT_ID(N'dbo.SalesOrders', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesOrders
    (
        Id                    UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_SalesOrders_Id DEFAULT NEWID(),
        SalesOrderNumber      NVARCHAR(30)     NOT NULL,
        QuotationId           UNIQUEIDENTIFIER NOT NULL,
        CustomerId            UNIQUEIDENTIFIER NOT NULL,
        Status                NVARCHAR(30)     NOT NULL,
        OrderDate             DATETIME2        NOT NULL,
        ExpectedDeliveryDate  DATETIME2        NULL,
        Notes                 NVARCHAR(MAX)    NULL,
        Subtotal              DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_SalesOrders_Subtotal DEFAULT (0),
        TaxTotal              DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_SalesOrders_TaxTotal DEFAULT (0),
        GrandTotal            DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_SalesOrders_GrandTotal DEFAULT (0),
        AssignedToUserId      UNIQUEIDENTIFIER NULL,
        CreatedByUserId       UNIQUEIDENTIFIER NULL,
        CreatedAtUtc          DATETIME2        NOT NULL,
        UpdatedAtUtc          DATETIME2        NULL,
        CONSTRAINT PK_SalesOrders PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_SalesOrders_Quotations FOREIGN KEY (QuotationId) REFERENCES dbo.Quotations (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_SalesOrders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_SalesOrders_AssignedToUser FOREIGN KEY (AssignedToUserId) REFERENCES dbo.Users (Id) ON DELETE SET NULL
    );

    -- One quotation converts to at most one sales order.
    CREATE UNIQUE INDEX IX_SalesOrders_QuotationId ON dbo.SalesOrders (QuotationId);
    CREATE UNIQUE INDEX IX_SalesOrders_SalesOrderNumber ON dbo.SalesOrders (SalesOrderNumber);
    CREATE INDEX IX_SalesOrders_CustomerId ON dbo.SalesOrders (CustomerId);
    CREATE INDEX IX_SalesOrders_Status ON dbo.SalesOrders (Status);
    CREATE INDEX IX_SalesOrders_AssignedToUserId ON dbo.SalesOrders (AssignedToUserId);
END
GO

-- ============================================================================
-- SalesOrderLineItems (DeliveredQuantity tracks partial/split delivery)
-- ============================================================================
IF OBJECT_ID(N'dbo.SalesOrderLineItems', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SalesOrderLineItems
    (
        Id                UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_SalesOrderLineItems_Id DEFAULT NEWID(),
        SalesOrderId      UNIQUEIDENTIFIER NOT NULL,
        ProductName       NVARCHAR(200)    NOT NULL,
        Quantity          DECIMAL(18, 2)   NOT NULL,
        UnitPrice         DECIMAL(18, 2)   NOT NULL,
        DiscountPercent   DECIMAL(5, 2)    NULL,
        TaxPercent        DECIMAL(5, 2)    NULL,
        DeliveredQuantity DECIMAL(18, 2)   NOT NULL CONSTRAINT DF_SalesOrderLineItems_DeliveredQuantity DEFAULT (0),
        CONSTRAINT PK_SalesOrderLineItems PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_SalesOrderLineItems_SalesOrders FOREIGN KEY (SalesOrderId) REFERENCES dbo.SalesOrders (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_SalesOrderLineItems_SalesOrderId ON dbo.SalesOrderLineItems (SalesOrderId);
END
GO

-- ============================================================================
-- Activities (generic timeline shared by Leads, Customers, Opportunities, and future modules)
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
        ReminderSentAtUtc DATETIME2       NULL,
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

-- Activities.ReminderSentAtUtc — added post-launch; guarded ALTER for an Activities table that
-- already existed from an earlier run of this script.
IF OBJECT_ID(N'dbo.Activities', N'U') IS NOT NULL AND COL_LENGTH('dbo.Activities', 'ReminderSentAtUtc') IS NULL
BEGIN
    ALTER TABLE dbo.Activities ADD ReminderSentAtUtc DATETIME2 NULL;
END
GO

-- ============================================================================
-- AuditLogs (plain-English history entries for Lead/Opportunity/Customer mutations —
-- intentionally not a full field-by-field diff, see AuditLog.cs for rationale)
-- ============================================================================
IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs
    (
        Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_AuditLogs_Id DEFAULT NEWID(),
        EntityType      NVARCHAR(50)     NOT NULL,
        EntityId        UNIQUEIDENTIFIER NOT NULL,
        Action          NVARCHAR(30)     NOT NULL,
        Summary         NVARCHAR(1000)   NOT NULL,
        PerformedByUserId UNIQUEIDENTIFIER NULL,
        CreatedAtUtc    DATETIME2        NOT NULL,
        CONSTRAINT PK_AuditLogs PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_AuditLogs_PerformedByUser FOREIGN KEY (PerformedByUserId) REFERENCES dbo.Users (Id) ON DELETE SET NULL
    );

    CREATE INDEX IX_AuditLogs_EntityType_EntityId ON dbo.AuditLogs (EntityType, EntityId);
END
GO

-- ============================================================================
-- Notifications (polling-based in-app notifications — no push/SignalR in this milestone)
-- ============================================================================
IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Notifications
    (
        Id                UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Notifications_Id DEFAULT NEWID(),
        RecipientUserId   UNIQUEIDENTIFIER NOT NULL,
        Message           NVARCHAR(500)    NOT NULL,
        RelatedEntityType NVARCHAR(30)     NULL,
        RelatedEntityId   UNIQUEIDENTIFIER NULL,
        IsRead            BIT              NOT NULL CONSTRAINT DF_Notifications_IsRead DEFAULT (0),
        CreatedAtUtc      DATETIME2        NOT NULL,
        CONSTRAINT PK_Notifications PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Notifications_RecipientUser FOREIGN KEY (RecipientUserId) REFERENCES dbo.Users (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_Notifications_RecipientUserId_IsRead ON dbo.Notifications (RecipientUserId, IsRead);
END
GO

-- ============================================================================
-- Documents (generic file attachments for any CRM record — content stored as a blob directly
-- in the tenant database; see Document.cs for the storage-location rationale)
-- ============================================================================
IF OBJECT_ID(N'dbo.Documents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Documents
    (
        Id               UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Documents_Id DEFAULT NEWID(),
        EntityType       NVARCHAR(50)     NOT NULL,
        EntityId         UNIQUEIDENTIFIER NOT NULL,
        FileName         NVARCHAR(260)    NOT NULL,
        ContentType      NVARCHAR(150)    NOT NULL,
        SizeBytes        BIGINT           NOT NULL,
        Content          VARBINARY(MAX)   NOT NULL,
        UploadedByUserId UNIQUEIDENTIFIER NULL,
        CreatedAtUtc     DATETIME2        NOT NULL,
        CONSTRAINT PK_Documents PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT FK_Documents_UploadedByUser FOREIGN KEY (UploadedByUserId) REFERENCES dbo.Users (Id) ON DELETE SET NULL
    );

    CREATE INDEX IX_Documents_EntityType_EntityId ON dbo.Documents (EntityType, EntityId);
END
GO
