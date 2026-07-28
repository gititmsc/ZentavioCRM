using System.ComponentModel.DataAnnotations;

namespace ZentavioCRM.Core.DTOs.SalesOrders
{
    /// <summary>Converts an Accepted quotation into a Sales Order — line items are copied from the quotation, not re-entered.</summary>
    public class ConvertQuotationToSalesOrderRequest
    {
        [Required(ErrorMessage = "A quotation must be selected.")]
        public Guid QuotationId { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }

        public string? Notes { get; set; }

        public Guid? AssignedToUserId { get; set; }
    }

    public class UpdateSalesOrderRequest
    {
        public DateTime? ExpectedDeliveryDate { get; set; }

        public string? Notes { get; set; }
    }

    public class AssignSalesOrderRequest
    {
        [Required(ErrorMessage = "A user must be selected to assign the order to.")]
        public Guid UserId { get; set; }
    }

    public class UpdateSalesOrderStatusRequest
    {
        [Required]
        public Core.Enums.SalesOrderStatus Status { get; set; }
    }

    /// <summary>Records delivery against one or more line items — drives the order's derived Status (see SalesOrderService.RecordDeliveryAsync).</summary>
    public class RecordDeliveryRequest
    {
        [MinLength(1, ErrorMessage = "At least one delivery line is required.")]
        public List<RecordDeliveryLineRequest> Lines { get; set; } = [];
    }

    public class RecordDeliveryLineRequest
    {
        [Required]
        public Guid LineItemId { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Delivered quantity must be greater than 0.")]
        public decimal DeliveredQuantity { get; set; }
    }
}
