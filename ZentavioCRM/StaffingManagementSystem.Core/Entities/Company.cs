namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// The organization's own company profile. Single-tenant today — modeled as a first-class
    /// entity so multi-company / multi-tenant support can be layered on without a redesign.
    /// </summary>
    public class Company
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? LegalName { get; set; }

        public string? Industry { get; set; }

        public string? Website { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? TaxNumber { get; set; }

        public string? AddressLine1 { get; set; }

        public string? AddressLine2 { get; set; }

        public string? City { get; set; }

        public string? State { get; set; }

        public string? Country { get; set; }

        public string? PostalCode { get; set; }

        public string DefaultCurrency { get; set; } = "USD";

        public string TimeZone { get; set; } = "UTC";

        public string? LogoUrl { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public ICollection<Department> Departments { get; set; } = new List<Department>();
    }
}
