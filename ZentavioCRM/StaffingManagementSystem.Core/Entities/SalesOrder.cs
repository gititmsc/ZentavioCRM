using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// An order created by converting an accepted <see cref="Quotation"/> — CRM SRS Phase 6,
    /// section 6 "Sales Order Management". Line items are copied from the quotation at
    /// conversion time so the order is an independent, immutable-pricing snapshot even if the
    /// quotation (or its opportunity) changes afterward. ERP/invoice synchronization is out of
    /// scope — no external system integration exists in this milestone.
    /// </summary>
    public class SalesOrder
    {
        public Guid Id { get; set; }

        /// <summary>Human-friendly sequential number, e.g. "SO-000123".</summary>
        public string SalesOrderNumber { get; set; } = string.Empty;

        public Guid QuotationId { get; set; }

        public Quotation? Quotation { get; set; }

        public Guid CustomerId { get; set; }

        public Customer? Customer { get; set; }

        public SalesOrderStatus Status { get; set; } = SalesOrderStatus.Draft;

        public DateTime OrderDate { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }

        public string? Notes { get; set; }

        /// <summary>Snapshot totals, copied from the source quotation at conversion time.</summary>
        public decimal Subtotal { get; set; }

        public decimal TaxTotal { get; set; }

        public decimal GrandTotal { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public User? AssignedToUser { get; set; }

        public Guid? CreatedByUserId { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public ICollection<SalesOrderLineItem> LineItems { get; set; } = new List<SalesOrderLineItem>();
    }
}
