namespace ZentavioCRM.Core.Entities
{
    /// <summary>A single priced product/service row on a <see cref="Quotation"/>, including tax.</summary>
    public class QuotationLineItem
    {
        public Guid Id { get; set; }

        public Guid QuotationId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public decimal Quantity { get; set; } = 1;

        public decimal UnitPrice { get; set; }

        /// <summary>0-100.</summary>
        public decimal? DiscountPercent { get; set; }

        /// <summary>0-100.</summary>
        public decimal? TaxPercent { get; set; }

        /// <summary>Quantity * UnitPrice * (1 - Discount%), before tax.</summary>
        public decimal SubtotalAmount => Math.Round(Quantity * UnitPrice * (1 - (DiscountPercent ?? 0) / 100m), 2);

        /// <summary>SubtotalAmount * Tax%.</summary>
        public decimal TaxAmount => Math.Round(SubtotalAmount * (TaxPercent ?? 0) / 100m, 2);

        /// <summary>SubtotalAmount + TaxAmount.</summary>
        public decimal LineTotal => Math.Round(SubtotalAmount + TaxAmount, 2);
    }
}
