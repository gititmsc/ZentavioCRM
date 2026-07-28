namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// A single ordered product/service row on a <see cref="SalesOrder"/>. Copied from the
    /// source quotation's line items at conversion time. <see cref="DeliveredQuantity"/> tracks
    /// partial/split delivery — the parent order's Status is derived from these values rather
    /// than set directly (see SalesOrderService.RecordDeliveryAsync).
    /// </summary>
    public class SalesOrderLineItem
    {
        public Guid Id { get; set; }

        public Guid SalesOrderId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public decimal Quantity { get; set; } = 1;

        public decimal UnitPrice { get; set; }

        /// <summary>0-100.</summary>
        public decimal? DiscountPercent { get; set; }

        /// <summary>0-100.</summary>
        public decimal? TaxPercent { get; set; }

        /// <summary>How much of <see cref="Quantity"/> has been delivered so far.</summary>
        public decimal DeliveredQuantity { get; set; }

        public decimal SubtotalAmount => Math.Round(Quantity * UnitPrice * (1 - (DiscountPercent ?? 0) / 100m), 2);

        public decimal TaxAmount => Math.Round(SubtotalAmount * (TaxPercent ?? 0) / 100m, 2);

        public decimal LineTotal => Math.Round(SubtotalAmount + TaxAmount, 2);
    }
}
