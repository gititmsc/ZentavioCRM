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

        public decimal? Budget { get; set; }

        public string? Timeline { get; set; }

        public decimal? ExpectedValue { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public string? Territory { get; set; }

        public string? Notes { get; set; }
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
}
