using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// A sellable catalog entry — either a physical/orderable Product or a billable Service —
    /// per the CRM SRS (Phase 5 — Product & Service Catalog). Used as a convenience picker when
    /// building Opportunity/Quotation line items, autofilling name/price/tax instead of the
    /// free-text entry those line items previously required.
    /// </summary>
    public class Product
    {
        public Guid Id { get; set; }

        public string Sku { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public ProductType Type { get; set; } = ProductType.Product;

        public string? Category { get; set; }

        public string? Brand { get; set; }

        public string? UnitOfMeasure { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal? Cost { get; set; }

        public decimal? TaxPercent { get; set; }

        public string? Description { get; set; }

        /// <summary>Service-only: typical duration to deliver/perform the service, in minutes.</summary>
        public int? DurationMinutes { get; set; }

        /// <summary>Service-only: free-text billing cadence, e.g. "One-Time", "Hourly", "Monthly", "Annual".</summary>
        public string? BillingType { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
    }
}
