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

        public string CurrencyCode { get; set; } = "USD";

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

        public string CurrencyCode { get; set; } = "USD";

        public int? Probability { get; set; }

        public string? Products { get; set; }

        public string? Competitors { get; set; }

        public DateTime? ExpectedCloseDate { get; set; }

        public OpportunityStage Stage { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public string? AssignedToUserName { get; set; }

        public Guid? SourceLeadId { get; set; }

        public string? Notes { get; set; }

        public string? NextStep { get; set; }

        public DateTime? NextStepDate { get; set; }

        public string? LostReason { get; set; }

        public DateTime? ClosedAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public List<OpportunityLineItemDto> LineItems { get; set; } = [];

        public List<OpportunityContactDto> Contacts { get; set; } = [];
    }

    public class OpportunityLineItemDto
    {
        public Guid Id { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal? DiscountPercent { get; set; }

        public decimal LineTotal { get; set; }
    }

    /// <summary>A buying-committee member — see <see cref="Entities.OpportunityContact"/>.</summary>
    public class OpportunityContactDto
    {
        public Guid Id { get; set; }

        public Guid ContactPersonId { get; set; }

        public string ContactPersonName { get; set; } = string.Empty;

        public string? ContactPersonDesignation { get; set; }

        public OpportunityContactRole Role { get; set; }

        public string? Notes { get; set; }
    }
}
