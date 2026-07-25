using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.DTOs.Opportunities
{
    /// <summary>Lightweight shape for the Opportunities list grid / pipeline board view.</summary>
    public class OpportunityListItemDto
    {
        public Guid Id { get; set; }

        public string OpportunityNumber { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public Guid CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public decimal? Value { get; set; }

        public int? Probability { get; set; }

        public DateTime? ExpectedCloseDate { get; set; }

        public OpportunityStage Stage { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public string? AssignedToUserName { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>Full shape for the Opportunity detail screen.</summary>
    public class OpportunityDto
    {
        public Guid Id { get; set; }

        public string OpportunityNumber { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public Guid CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public decimal? Value { get; set; }

        public int? Probability { get; set; }

        public string? Products { get; set; }

        public string? Competitors { get; set; }

        public DateTime? ExpectedCloseDate { get; set; }

        public OpportunityStage Stage { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public string? AssignedToUserName { get; set; }

        public Guid? SourceLeadId { get; set; }

        public string? Notes { get; set; }

        public string? LostReason { get; set; }

        public DateTime? ClosedAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
    }
}
