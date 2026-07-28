using System.ComponentModel.DataAnnotations;

namespace ZentavioCRM.Core.DTOs.Quotations
{
    /// <summary>Creates the first version of a quotation against an Opportunity.</summary>
    public class CreateQuotationRequest
    {
        [Required(ErrorMessage = "An opportunity must be selected.")]
        public Guid OpportunityId { get; set; }

        public DateTime? ValidUntil { get; set; }

        public string? TermsAndConditions { get; set; }

        public string? Notes { get; set; }

        public Guid? AssignedToUserId { get; set; }

        [MinLength(1, ErrorMessage = "At least one line item is required.")]
        public List<SaveQuotationLineItemRequest> LineItems { get; set; } = [];
    }

    /// <summary>Edits a Draft quotation in place — quotations that have already been Sent/Accepted/Rejected use "New Version" instead.</summary>
    public class UpdateQuotationRequest
    {
        public DateTime? ValidUntil { get; set; }

        public string? TermsAndConditions { get; set; }

        public string? Notes { get; set; }

        [MinLength(1, ErrorMessage = "At least one line item is required.")]
        public List<SaveQuotationLineItemRequest> LineItems { get; set; } = [];
    }

    public class SaveQuotationLineItemRequest
    {
        [Required(ErrorMessage = "Product name is required.")]
        [MaxLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [Range(0.01, double.MaxValue, ErrorMessage = "Quantity must be greater than 0.")]
        public decimal Quantity { get; set; } = 1;

        [Range(0, double.MaxValue, ErrorMessage = "Unit price cannot be negative.")]
        public decimal UnitPrice { get; set; }

        [Range(0, 100, ErrorMessage = "Discount must be between 0 and 100.")]
        public decimal? DiscountPercent { get; set; }

        [Range(0, 100, ErrorMessage = "Tax must be between 0 and 100.")]
        public decimal? TaxPercent { get; set; }
    }

    public class UpdateQuotationStatusRequest
    {
        [Required]
        public Core.Enums.QuotationStatus Status { get; set; }
    }

    public class AssignQuotationRequest
    {
        [Required(ErrorMessage = "A user must be selected to assign the quotation to.")]
        public Guid UserId { get; set; }
    }
}
