using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.SalesOrders;
using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Services.Interfaces
{
    public interface ISalesOrderService
    {
        Task<PagedResult<SalesOrderListItemDto>> SearchAsync(
            string? search, SalesOrderStatus? status, Guid? customerId, int page, int pageSize);

        Task<ApiResponse<SalesOrderDto>> GetByIdAsync(Guid id);

        /// <summary>Converts an Accepted quotation into a new Sales Order, copying its line items as a pricing snapshot.</summary>
        Task<ApiResponse<SalesOrderDto>> ConvertFromQuotationAsync(ConvertQuotationToSalesOrderRequest request, Guid? currentUserId);

        Task<ApiResponse<SalesOrderDto>> UpdateAsync(Guid id, UpdateSalesOrderRequest request, Guid? currentUserId);

        Task<ApiResponse<SalesOrderDto>> AssignAsync(Guid id, AssignSalesOrderRequest request, Guid? currentUserId);

        /// <summary>Records delivered quantities against one or more line items; the order's Status is then re-derived (Confirmed/PartiallyDelivered/Delivered).</summary>
        Task<ApiResponse<SalesOrderDto>> RecordDeliveryAsync(Guid id, RecordDeliveryRequest request, Guid? currentUserId);

        Task<ApiResponse<SalesOrderDto>> CancelAsync(Guid id, Guid? currentUserId);
    }
}
