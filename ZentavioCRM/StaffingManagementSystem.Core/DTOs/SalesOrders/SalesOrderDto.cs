using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.DTOs.SalesOrders
{
    /// <summary>Lightweight shape for the Sales Orders list grid.</summary>
    public class SalesOrderListItemDto
    {
        public Guid Id { get; set; }

        public string SalesOrderNumber { get; set; } = string.Empty;

        public Guid QuotationId { get; set; }

        public string QuotationNumber { get; set; } = string.Empty;

        public Guid CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public SalesOrderStatus Status { get; set; }

        public decimal GrandTotal { get; set; }

        public DateTime OrderDate { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public string? AssignedToUserName { get; set; }
    }

    /// <summary>Full shape for the Sales Order detail screen.</summary>
    public class SalesOrderDto
    {
        public Guid Id { get; set; }

        public string SalesOrderNumber { get; set; } = string.Empty;

        public Guid QuotationId { get; set; }

        public string QuotationNumber { get; set; } = string.Empty;

        public Guid CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public SalesOrderStatus Status { get; set; }

        public DateTime OrderDate { get; set; }

        public DateTime? ExpectedDeliveryDate { get; set; }

        public string? Notes { get; set; }

        public decimal Subtotal { get; set; }

        public decimal TaxTotal { get; set; }

        public decimal GrandTotal { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public string? AssignedToUserName { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        public List<SalesOrderLineItemDto> LineItems { get; set; } = [];
    }

    public class SalesOrderLineItemDto
    {
        public Guid Id { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public decimal Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal? DiscountPercent { get; set; }

        public decimal? TaxPercent { get; set; }

        public decimal DeliveredQuantity { get; set; }

        public decimal LineTotal { get; set; }
    }
}
