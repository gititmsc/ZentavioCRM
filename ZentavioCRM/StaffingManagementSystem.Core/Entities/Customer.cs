using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// The Customer Master — the single source of truth for any organization or individual
    /// the business deals with (prospect, business account, vendor, partner, etc.).
    /// </summary>
    public class Customer
    {
        public Guid Id { get; set; }

        /// <summary>Human-friendly sequential number, e.g. "CUST-000123".</summary>
        public string CustomerNumber { get; set; } = string.Empty;

        public CustomerType Type { get; set; } = CustomerType.Prospect;

        public string LegalName { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string? Industry { get; set; }

        public string? Website { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? TaxNumber { get; set; }

        public int? EmployeesCount { get; set; }

        public decimal? AnnualRevenue { get; set; }

        public string CurrencyCode { get; set; } = "USD";

        public int? PaymentTermsDays { get; set; }

        public decimal? CreditLimit { get; set; }

        /// <summary>Simple qualitative rating, e.g. "Hot", "Warm", "Cold".</summary>
        public string? Rating { get; set; }

        /// <summary>Freeform, comma-separated segmentation labels (e.g. "VIP, At Risk, Hot Account"). No separate Tag table in this milestone — kept as simple as Rating above.</summary>
        public string? Tags { get; set; }

        /// <summary>How this customer was originally acquired. Auto-populated from the source Lead when converted; set manually for customers created directly.</summary>
        public LeadSource? AcquisitionSource { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public User? AssignedToUser { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public ICollection<ContactPerson> Contacts { get; set; } = new List<ContactPerson>();

        public ICollection<CustomerAddress> Addresses { get; set; } = new List<CustomerAddress>();

        public ICollection<Lead> ConvertedFromLeads { get; set; } = new List<Lead>();

        public ICollection<Opportunity> Opportunities { get; set; } = new List<Opportunity>();
    }
}
