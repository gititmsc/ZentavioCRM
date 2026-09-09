namespace ZentavioCRM.Core.DTOs.Products
{
    public class ProductDto
    {
        public Guid Id { get; set; }

        public string Sku { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public string? Category { get; set; }

        public string? Brand { get; set; }

        public string? UnitOfMeasure { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal? Cost { get; set; }

        public decimal? TaxPercent { get; set; }

        public string? Description { get; set; }

        public int? DurationMinutes { get; set; }

        public string? BillingType { get; set; }

        public bool IsActive { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
    }
}
