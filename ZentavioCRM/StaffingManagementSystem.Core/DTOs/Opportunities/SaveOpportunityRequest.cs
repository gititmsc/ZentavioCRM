using System.ComponentModel.DataAnnotations;

namespace ZentavioCRM.Core.DTOs.Opportunities
{
    public class SaveOpportunityRequest
    {
        [Required(ErrorMessage = "Opportunity name is required.")]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "A customer must be selected.")]
        public Guid CustomerId { get; set; }

        public decimal? Value { get; set; }

        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int? Probability { get; set; }

        public string? Products { get; set; }

        public string? Competitors { get; set; }

        public DateTime? ExpectedCloseDate { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public string? Notes { get; set; }

        public string? NextStep { get; set; }

        public DateTime? NextStepDate { get; set; }

        /// <summary>When non-empty, <see cref="Value"/> is recomputed server-side as the sum of these lines' totals instead of using the submitted Value.</summary>
        public List<SaveOpportunityLineItemRequest> LineItems { get; set; } = [];
    }

    public class SaveOpportunityLineItemRequest
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
    }

    public class UpdateOpportunityStageRequest
    {
        [Required]
        public Core.Enums.OpportunityStage Stage { get; set; }

        /// <summary>Required when <see cref="Stage"/> is <see cref="Core.Enums.OpportunityStage.ClosedLost"/>.</summary>
        public string? Reason { get; set; }
    }

    public class AssignOpportunityRequest
    {
        [Required(ErrorMessage = "A user must be selected to assign the opportunity to.")]
        public Guid UserId { get; set; }
    }
}
