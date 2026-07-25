using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.DTOs.Leads
{
    /// <summary>Lightweight shape for the Leads list grid / Kanban pipeline view.</summary>
    public class LeadListItemDto
    {
        public Guid Id { get; set; }

        public string LeadNumber { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string ContactName { get; set; } = string.Empty;

        public LeadSource Source { get; set; }

        public LeadStatus Status { get; set; }

        public decimal? ExpectedValue { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public string? AssignedToUserName { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>Full shape for the Lead detail screen.</summary>
    public class LeadDto
    {
        public Guid Id { get; set; }

        public string LeadNumber { get; set; } = string.Empty;

        public string CompanyName { get; set; } = string.Empty;

        public string ContactName { get; set; } = string.Empty;

        public string? Email { get; set; }

        public string? Mobile { get; set; }

        public string? Industry { get; set; }

        public LeadSource Source { get; set; }

        public string? Campaign { get; set; }

        public decimal? Budget { get; set; }

        public string? Timeline { get; set; }

        public decimal? ExpectedValue { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public string? AssignedToUserName { get; set; }

        public string? Territory { get; set; }

        public LeadStatus Status { get; set; }

        public int? LeadScore { get; set; }

        public int? AiScore { get; set; }

        public string? Notes { get; set; }

        public string? LostReason { get; set; }

        public Guid? ConvertedCustomerId { get; set; }

        public DateTime? ConvertedAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
    }
}
