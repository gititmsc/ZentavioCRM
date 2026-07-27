using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// A qualified deal in progress toward a won or lost outcome. Sits between the Customer
    /// master and the (future) Quotation/Sales Order modules in the Lead-to-Customer journey —
    /// see CRM_SRS Phase 6, section 4 "Opportunity Management".
    /// </summary>
    public class Opportunity
    {
        public Guid Id { get; set; }

        /// <summary>Human-friendly sequential number, e.g. "OPP-000123".</summary>
        public string OpportunityNumber { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public Guid CustomerId { get; set; }

        public Customer? Customer { get; set; }

        public decimal? Value { get; set; }

        /// <summary>0-100. Subjective likelihood of closing, set by the salesperson (not yet AI-driven).</summary>
        public int? Probability { get; set; }

        /// <summary>Free-text list of products/services in scope. Becomes a real line-item relation once the Product Catalog module (SRS Phase 5) is built.</summary>
        public string? Products { get; set; }

        public string? Competitors { get; set; }

        public DateTime? ExpectedCloseDate { get; set; }

        public OpportunityStage Stage { get; set; } = OpportunityStage.Qualification;

        public Guid? AssignedToUserId { get; set; }

        public User? AssignedToUser { get; set; }

        /// <summary>Set when this opportunity was created via Lead conversion, for traceability. Optional — opportunities can also be created directly against an existing customer.</summary>
        public Guid? SourceLeadId { get; set; }

        public Lead? SourceLead { get; set; }

        public string? Notes { get; set; }

        /// <summary>The single next action to move this deal forward — one of the most-used fields in any real sales CRM.</summary>
        public string? NextStep { get; set; }

        public DateTime? NextStepDate { get; set; }

        /// <summary>Set when <see cref="Stage"/> is <see cref="OpportunityStage.ClosedLost"/>.</summary>
        public string? LostReason { get; set; }

        public DateTime? ClosedAtUtc { get; set; }

        public Guid? CreatedByUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        /// <summary>Real, priceable product/service rows — see <see cref="OpportunityLineItem"/>. Empty when the deal only uses the free-text <see cref="Products"/> summary.</summary>
        public ICollection<OpportunityLineItem> LineItems { get; set; } = new List<OpportunityLineItem>();
    }
}
