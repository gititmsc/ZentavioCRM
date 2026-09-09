using System.ComponentModel.DataAnnotations;
using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.DTOs.Products
{
    /// <summary>Shared shape for creating and updating a Product/Service catalog entry.</summary>
    public class SaveProductRequest
    {
        [Required(ErrorMessage = "SKU is required.")]
        [MaxLength(50)]
        public string Sku { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Type is required.")]
        public ProductType Type { get; set; } = ProductType.Product;

        [MaxLength(100)]
        public string? Category { get; set; }

        [MaxLength(100)]
        public string? Brand { get; set; }

        [MaxLength(50)]
        public string? UnitOfMeasure { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Unit price cannot be negative.")]
        public decimal UnitPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Cost cannot be negative.")]
        public decimal? Cost { get; set; }

        [Range(0, 100, ErrorMessage = "Tax percent must be between 0 and 100.")]
        public decimal? TaxPercent { get; set; }

        [MaxLength(2000)]
        public string? Description { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Duration cannot be negative.")]
        public int? DurationMinutes { get; set; }

        [MaxLength(50)]
        public string? BillingType { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
