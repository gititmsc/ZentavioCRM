using System.ComponentModel.DataAnnotations;
using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.DTOs.Leads
{
    public class SaveLeadRequest
    {
        [Required(ErrorMessage = "Company name is required.")]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Contact name is required.")]
        [MaxLength(200)]
        public string ContactName { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        public string? Mobile { get; set; }

        public string? Industry { get; set; }

        public LeadSource Source { get; set; } = LeadSource.ManualEntry;

        public string? Campaign { get; set; }

        public string? UtmSource { get; set; }

        public string? UtmMedium { get; set; }

        public string? UtmCampaign { get; set; }

        public string? UtmTerm { get; set; }

        public string? UtmContent { get; set; }

        public decimal? Budget { get; set; }

        public string? Timeline { get; set; }

        public decimal? ExpectedValue { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public string? Territory { get; set; }

        public Guid? TerritoryId { get; set; }

        public string? Notes { get; set; }

        public DateTime? NextFollowUpDate { get; set; }

        /// <summary>Optional — set when the user picked a match from the "existing customer" lookup on the Lead form instead of typing a brand-new company. See <see cref="ZentavioCRM.Core.Entities.Lead.LinkedCustomerId"/>.</summary>
        public Guid? LinkedCustomerId { get; set; }

        /// <summary>Optional — set when a specific existing contact was matched/selected for the linked customer. See <see cref="ZentavioCRM.Core.Entities.Lead.LinkedContactId"/>.</summary>
        public Guid? LinkedContactId { get; set; }
    }

    public class UpdateLeadStatusRequest
    {
        [Required]
        public LeadStatus Status { get; set; }

        /// <summary>Required when <see cref="Status"/> is <see cref="LeadStatus.Lost"/> or <see cref="LeadStatus.Junk"/>.</summary>
        public string? Reason { get; set; }
    }

    public class AssignLeadRequest
    {
        [Required(ErrorMessage = "A user must be selected to assign the lead to.")]
        public Guid UserId { get; set; }
    }

    public class ConvertLeadRequest
    {
        /// <summary>Optional overrides — if omitted, the Customer is created from the Lead's own fields.</summary>
        public string? DisplayName { get; set; }

        public Guid? AssignToUserId { get; set; }
    }

    public class ConvertLeadResultDto
    {
        public Guid CustomerId { get; set; } = Guid.Empty;

        public string CustomerNumber { get; set; } = string.Empty;
    }

    public class ConvertLeadToOpportunityRequest
    {
        /// <summary>Optional overrides — if omitted, sensible defaults are derived from the Lead's own fields.</summary>
        public string? OpportunityName { get; set; }

        public string? CustomerDisplayName { get; set; }

        public decimal? Value { get; set; }

        public DateTime? ExpectedCloseDate { get; set; }

        public Guid? AssignToUserId { get; set; }
    }

    public class ConvertLeadToOpportunityResultDto
    {
        public Guid CustomerId { get; set; } = Guid.Empty;

        public string CustomerNumber { get; set; } = string.Empty;

        public Guid OpportunityId { get; set; } = Guid.Empty;

        public string OpportunityNumber { get; set; } = string.Empty;
    }

    public class DuplicateMatchDto
    {
        /// <summary>"Lead" or "Customer".</summary>
        public string Type { get; set; } = string.Empty;

        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Mobile { get; set; }
    }

    public class DuplicateCheckResultDto
    {
        public List<DuplicateMatchDto> Matches { get; set; } = [];
    }

    /// <summary>A single Contact on a customer matched by the "existing customer" lookup, shown so the user can pick the right person to prefill from (not just the right company).</summary>
    public class CustomerLookupContactDto
    {
        public Guid Id { get; set; }

        public string FullName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Mobile { get; set; }

        public bool IsPrimary { get; set; }
    }

    /// <summary>A Customer matched by company name while adding/editing a Lead — powers the "select an existing customer" picker on the Lead form, distinct from <see cref="DuplicateMatchDto"/> (which is email/mobile-keyed and link-only, not name-keyed and select-to-prefill).</summary>
    public class CustomerLookupDto
    {
        public Guid Id { get; set; }

        public string CustomerNumber { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string LegalName { get; set; } = string.Empty;

        public string? Industry { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public List<CustomerLookupContactDto> Contacts { get; set; } = [];
    }
}
