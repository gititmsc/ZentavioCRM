using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// A priced proposal sent to a Customer against an Opportunity — CRM SRS Phase 6, section 5
    /// "Quotation Management". An Opportunity can have multiple quotations (re-quotes, revised
    /// pricing); <see cref="Version"/> distinguishes them when a quotation is re-issued via
    /// "New Version" rather than edited in place once it has been sent.
    /// </summary>
    public class Quotation
    {
        public Guid Id { get; set; }

        /// <summary>Human-friendly sequential number, e.g. "QUO-000123". Stays the same across versions of the same quote.</summary>
        public string QuotationNumber { get; set; } = string.Empty;

        public int Version { get; set; } = 1;

        public Guid OpportunityId { get; set; }

        public Opportunity? Opportunity { get; set; }

        /// <summary>Denormalized from the Opportunity at creation time for simpler querying/filtering.</summary>
        public Guid CustomerId { get; set; }

        public Customer? Customer { get; set; }

        public QuotationStatus Status { get; set; } = QuotationStatus.Draft;

        public DateTime? ValidUntil { get; set; }

        public string? TermsAndConditions { get; set; }

        public string? Notes { get; set; }

        /// <summary>Sum of line totals before tax. Server-computed from <see cref="LineItems"/>.</summary>
        public decimal Subtotal { get; set; }

        /// <summary>Sum of per-line tax amounts. Server-computed from <see cref="LineItems"/>.</summary>
        public decimal TaxTotal { get; set; }

        /// <summary>Subtotal + TaxTotal. Server-computed from <see cref="LineItems"/>.</summary>
        public decimal GrandTotal { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public User? AssignedToUser { get; set; }

        public Guid? CreatedByUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public ICollection<QuotationLineItem> LineItems { get; set; } = new List<QuotationLineItem>();
    }
}
