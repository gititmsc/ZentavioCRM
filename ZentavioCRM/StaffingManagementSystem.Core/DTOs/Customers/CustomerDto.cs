using System.ComponentModel.DataAnnotations;
using ZentavioCRM.Core.DTOs.Tags;
using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.DTOs.Customers
{
    /// <summary>Lightweight shape for the Customers list grid.</summary>
    public class CustomerListItemDto
    {
        public Guid Id { get; set; }

        public string CustomerNumber { get; set; } = string.Empty;

        public CustomerType Type { get; set; }

        public string DisplayName { get; set; } = string.Empty;

        public string? Industry { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? AssignedToUserName { get; set; }

        /// <summary>Original freeform comma-separated text field — unchanged, still read/written by CSV import/export.</summary>
        public string? Tags { get; set; }

        /// <summary>Structured tags from the new Tag system (see Tag.cs) — distinct from the freeform <see cref="Tags"/> field above.</summary>
        public List<TagDto> TagList { get; set; } = [];

        public CustomerHealthStatus? HealthStatus { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>Full shape for the Customer detail screen.</summary>
    public class CustomerDto
    {
        public Guid Id { get; set; }

        public string CustomerNumber { get; set; } = string.Empty;

        public CustomerType Type { get; set; }

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

        public string? Rating { get; set; }

        public string? Tags { get; set; }

        public List<TagDto> TagList { get; set; } = [];

        public LeadSource? AcquisitionSource { get; set; }

        public CustomerHealthStatus? HealthStatus { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public string? AssignedToUserName { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public List<ContactPersonDto> Contacts { get; set; } = [];

        public List<CustomerAddressDto> Addresses { get; set; } = [];
    }

    public class SaveCustomerRequest
    {
        public CustomerType Type { get; set; } = CustomerType.Prospect;

        [Required(ErrorMessage = "Legal name is required.")]
        [MaxLength(200)]
        public string LegalName { get; set; } = string.Empty;

        [MaxLength(200)]
        public string DisplayName { get; set; } = string.Empty;

        public string? Industry { get; set; }

        public string? Website { get; set; }

        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string? TaxNumber { get; set; }

        public int? EmployeesCount { get; set; }

        public decimal? AnnualRevenue { get; set; }

        public string CurrencyCode { get; set; } = "USD";

        public int? PaymentTermsDays { get; set; }

        public decimal? CreditLimit { get; set; }

        public string? Rating { get; set; }

        public string? Tags { get; set; }

        /// <summary>Structured tag Ids from the new Tag system — replaces the full set on save (not an incremental add).</summary>
        public List<Guid> TagIds { get; set; } = [];

        public LeadSource? AcquisitionSource { get; set; }

        public CustomerHealthStatus? HealthStatus { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public bool IsActive { get; set; } = true;

        public List<SaveContactPersonRequest> Contacts { get; set; } = [];

        public List<SaveCustomerAddressRequest> Addresses { get; set; } = [];
    }
}
