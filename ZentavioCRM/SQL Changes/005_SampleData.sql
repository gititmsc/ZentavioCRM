/*
    005_SampleData.sql
    ZentavioCRM — OPTIONAL sample/demo data for local development and testing.

    001_CreateSchema.sql / 002_SeedData.sql intentionally seed only reference/config data
    (Company profile, RBAC). The 5 transactional tables — Customers, ContactPersons,
    CustomerAddresses, Leads, Activities — are correctly left empty there: a real tenant should
    start with zero fake customers. This script fills those 5 tables with a small, realistic
    dataset so there's something to click through in the UI (list views, detail pages, the
    activity timeline, lead status transitions, etc.) without manually typing test data first.

    >>> DO NOT run this against a real customer's tenant database. <<<
    Safe for: a local dev database, a demo environment, or a database you built by hand from
    001/002 (e.g. StaffingManagementSystemDb). Not inserted by TenantProvisioningService —
    real tenants created via POST /api/platform/tenants start with zero sample data, correctly.

    Run 001_CreateSchema.sql and 002_SeedData.sql first. Safe to re-run — every insert is
    guarded with a "not already present" check, keyed off the same fixed IDs used below.

    Adds:
      - 3 Customers (one Business, one Prospect, one Individual)
      - 2 ContactPersons (on the two company customers — an Individual customer has no separate contact)
      - 2 CustomerAddresses
      - 3 Leads (New / Assigned / Qualified)
      - 2 Activities (one on a Lead, one on a Customer)
    All assigned to the default Admin user (EMP-0001) seeded by 002_SeedData.sql.
*/

USE [StaffingManagementSystemDb];
GO

DECLARE @AdminUserId UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000001';

-- ============================================================================
-- Customers
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Id = '70000000-0000-0000-0000-000000000001')
BEGIN
    INSERT INTO dbo.Customers
        (Id, CustomerNumber, Type, LegalName, DisplayName, Industry, Email, Phone, CurrencyCode, Rating, AssignedToUserId, IsActive, CreatedAtUtc)
    VALUES
        ('70000000-0000-0000-0000-000000000001', N'CUST-000001', N'Business', N'Acme Manufacturing Ltd', N'Acme Manufacturing', N'Manufacturing', N'info@acmemfg.example', N'+1-555-0101', N'USD', N'Hot', @AdminUserId, 1, SYSUTCDATETIME());
END
GO

DECLARE @AdminUserId UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000001';

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Id = '70000000-0000-0000-0000-000000000002')
BEGIN
    INSERT INTO dbo.Customers
        (Id, CustomerNumber, Type, LegalName, DisplayName, Industry, Email, Phone, CurrencyCode, Rating, AssignedToUserId, IsActive, CreatedAtUtc)
    VALUES
        ('70000000-0000-0000-0000-000000000002', N'CUST-000002', N'Prospect', N'Blue Horizon Retail', N'Blue Horizon Retail', N'Retail', N'contact@bluehorizon.example', N'+1-555-0102', N'USD', N'Warm', @AdminUserId, 1, SYSUTCDATETIME());
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Customers WHERE Id = '70000000-0000-0000-0000-000000000003')
BEGIN
    INSERT INTO dbo.Customers
        (Id, CustomerNumber, Type, LegalName, DisplayName, Industry, Email, Phone, CurrencyCode, Rating, IsActive, CreatedAtUtc)
    VALUES
        ('70000000-0000-0000-0000-000000000003', N'CUST-000003', N'Individual', N'Priya Sharma', N'Priya Sharma', NULL, N'priya.sharma@example.com', N'+1-555-0103', N'USD', N'Cold', 1, SYSUTCDATETIME());
END
GO

-- ============================================================================
-- ContactPersons
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.ContactPersons WHERE Id = '70000000-0000-0000-0000-000000000101')
BEGIN
    INSERT INTO dbo.ContactPersons
        (Id, CustomerId, FirstName, LastName, Designation, Email, Mobile, IsPrimary, IsDecisionMaker, CreatedAtUtc)
    VALUES
        ('70000000-0000-0000-0000-000000000101', '70000000-0000-0000-0000-000000000001', N'Robert', N'Nguyen', N'VP of Operations', N'robert.nguyen@acmemfg.example', N'+1-555-0111', 1, 1, SYSUTCDATETIME());
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.ContactPersons WHERE Id = '70000000-0000-0000-0000-000000000201')
BEGIN
    INSERT INTO dbo.ContactPersons
        (Id, CustomerId, FirstName, LastName, Designation, Email, Mobile, IsPrimary, IsDecisionMaker, CreatedAtUtc)
    VALUES
        ('70000000-0000-0000-0000-000000000201', '70000000-0000-0000-0000-000000000002', N'Layla', N'Ahmadi', N'Purchasing Manager', N'layla.ahmadi@bluehorizon.example', N'+1-555-0121', 1, 0, SYSUTCDATETIME());
END
GO

-- ============================================================================
-- CustomerAddresses
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.CustomerAddresses WHERE Id = '70000000-0000-0000-0000-000000000301')
BEGIN
    INSERT INTO dbo.CustomerAddresses
        (Id, CustomerId, Type, Line1, City, State, Country, PostalCode, IsPrimary)
    VALUES
        ('70000000-0000-0000-0000-000000000301', '70000000-0000-0000-0000-000000000001', N'Billing', N'4500 Industrial Pkwy', N'Columbus', N'OH', N'USA', N'43215', 1);
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.CustomerAddresses WHERE Id = '70000000-0000-0000-0000-000000000401')
BEGIN
    INSERT INTO dbo.CustomerAddresses
        (Id, CustomerId, Type, Line1, City, State, Country, PostalCode, IsPrimary)
    VALUES
        ('70000000-0000-0000-0000-000000000401', '70000000-0000-0000-0000-000000000002', N'Billing', N'120 Market Street', N'Austin', N'TX', N'USA', N'73301', 1);
END
GO

-- ============================================================================
-- Leads
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM dbo.Leads WHERE Id = '80000000-0000-0000-0000-000000000001')
BEGIN
    INSERT INTO dbo.Leads
        (Id, LeadNumber, CompanyName, ContactName, Email, Mobile, Industry, Source, ExpectedValue, Territory, Status, CreatedAtUtc)
    VALUES
        ('80000000-0000-0000-0000-000000000001', N'LEAD-000001', N'Northwind Traders', N'John Carter', N'john.carter@northwind.example', N'+1-555-0201', N'Distribution', N'Website', 15000.00, N'West', N'New', SYSUTCDATETIME());
END
GO

DECLARE @AdminUserId3 UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000001';

IF NOT EXISTS (SELECT 1 FROM dbo.Leads WHERE Id = '80000000-0000-0000-0000-000000000002')
BEGIN
    INSERT INTO dbo.Leads
        (Id, LeadNumber, CompanyName, ContactName, Email, Mobile, Industry, Source, ExpectedValue, AssignedToUserId, Territory, Status, CreatedAtUtc)
    VALUES
        ('80000000-0000-0000-0000-000000000002', N'LEAD-000002', N'Contoso Health', N'Amara Okafor', N'amara.okafor@contosohealth.example', N'+1-555-0202', N'Healthcare', N'Referral', 42000.00, @AdminUserId3, N'East', N'Assigned', SYSUTCDATETIME());
END
GO

DECLARE @AdminUserId4 UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000001';

IF NOT EXISTS (SELECT 1 FROM dbo.Leads WHERE Id = '80000000-0000-0000-0000-000000000003')
BEGIN
    INSERT INTO dbo.Leads
        (Id, LeadNumber, CompanyName, ContactName, Email, Mobile, Industry, Source, ExpectedValue, AssignedToUserId, Territory, Status, LeadScore, CreatedAtUtc)
    VALUES
        ('80000000-0000-0000-0000-000000000003', N'LEAD-000003', N'Fabrikam Logistics', N'Diego Fernandez', N'diego.fernandez@fabrikam.example', N'+1-555-0203', N'Logistics', N'LinkedIn', 68000.00, @AdminUserId4, N'Central', N'Qualified', 72, SYSUTCDATETIME());
END
GO

-- ============================================================================
-- Activities (generic timeline — one on a Lead, one on a Customer)
-- ============================================================================
DECLARE @AdminUserId5 UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000001';

IF NOT EXISTS (SELECT 1 FROM dbo.Activities WHERE Id = '90000000-0000-0000-0000-000000000001')
BEGIN
    INSERT INTO dbo.Activities
        (Id, Type, Subject, Description, RelatedToType, RelatedToId, AssignedToUserId, CreatedByUserId, CreatedAtUtc)
    VALUES
        ('90000000-0000-0000-0000-000000000001', N'Call', N'Introductory call', N'Discussed requirements and budget range.', N'Lead', '80000000-0000-0000-0000-000000000001', @AdminUserId5, @AdminUserId5, SYSUTCDATETIME());
END
GO

DECLARE @AdminUserId6 UNIQUEIDENTIFIER = '50000000-0000-0000-0000-000000000001';

IF NOT EXISTS (SELECT 1 FROM dbo.Activities WHERE Id = '90000000-0000-0000-0000-000000000002')
BEGIN
    INSERT INTO dbo.Activities
        (Id, Type, Subject, Description, RelatedToType, RelatedToId, AssignedToUserId, CreatedByUserId, CreatedAtUtc)
    VALUES
        ('90000000-0000-0000-0000-000000000002', N'Note', N'Renewal reminder set', N'Contract renews in Q3 — flagged for follow-up.', N'Customer', '70000000-0000-0000-0000-000000000001', @AdminUserId6, @AdminUserId6, SYSUTCDATETIME());
END
GO
