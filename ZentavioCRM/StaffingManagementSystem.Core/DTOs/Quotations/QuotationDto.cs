using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.DTOs.Quotations
{
    /// <summary>Lightweight shape for the Quotations list grid.</summary>
    public class QuotationListItemDto
    {
        public Guid Id { get; set; }

        public string QuotationNumber { get; set; } = string.Empty;

        public int Version { get; set; }

        public Guid OpportunityId { get; set; }

        public string OpportunityName { get; set; } = string.Empty;

        public Guid CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public QuotationStatus Status { get; set; }

        public decimal GrandTotal { get; set; }

        public DateTime? ValidUntil { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public string? AssignedToUserName { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary>Full shape for the Quotation detail screen.</summary>
    public class QuotationDto
    {
        public Guid Id { get; set; }

        public string QuotationNumber { get; set; } = string.Empty;

        public int Version { get; set; }

        public Guid OpportunityId { get; set; }

        public string OpportunityName { get; set; } = string.Empty;

        public Guid CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public QuotationStatus Status { get; set; }

        public DateTime? ValidUntil { get; set; }

        public string? TermsAndConditions { get; set; }

        public string? Notes { get; set; }

        public decimal Subtotal { get; set; }

        public decimal TaxTotal { get; set; }

        public decimal GrandTotal { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public string? AssignedToUserName { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        /// <summary>True once a Sales Order has been created from this quotation (only possible when Accepted) — drives whether "Convert to Sales Order" shows.</summary>
        public bool HasSalesOrder { get; set; }

        public List<QuotationLineItemDto> LineItems { get; set; } = [];
    }

    public class QuotationLineItemDto
    {
        public Guid Id { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal? DiscountPercent { get; set; }

        public decimal? TaxPercent { get; set; }

        public decimal LineTotal { get; set; }
    }
}
