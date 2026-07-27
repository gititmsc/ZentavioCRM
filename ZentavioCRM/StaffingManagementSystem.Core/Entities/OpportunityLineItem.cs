namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// A single product/service line on an <see cref="Opportunity"/>. Replaces the earlier
    /// free-text "Products" field with real, priceable rows once an opportunity needs more than
    /// a one-line description — <see cref="Opportunity.Products"/> is kept for a short free-text
    /// summary/notes use case and is independent of these rows.
    /// </summary>
    public class OpportunityLineItem
    {
        public Guid Id { get; set; }

        public Guid OpportunityId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public decimal Quantity { get; set; } = 1;

        public decimal UnitPrice { get; set; }

        /// <summary>0-100.</summary>
        public decimal? DiscountPercent { get; set; }

        public decimal LineTotal => Math.Round(Quantity * UnitPrice * (1 - (DiscountPercent ?? 0) / 100m), 2);
    }
}
