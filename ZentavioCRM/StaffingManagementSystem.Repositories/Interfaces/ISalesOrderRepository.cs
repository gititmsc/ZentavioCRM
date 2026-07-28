using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface ISalesOrderRepository
    {
        Task<SalesOrder?> GetByIdAsync(Guid id);

        Task<(IReadOnlyList<SalesOrder> Items, int TotalCount)> SearchAsync(
            string? search, SalesOrderStatus? status, Guid? customerId, int page, int pageSize);

        Task<string> GetNextSalesOrderNumberAsync();

        Task AddAsync(SalesOrder salesOrder);

        Task UpdateAsync(SalesOrder salesOrder);

        /// <summary>Persists updated DeliveredQuantity values on the order's existing line items (no full replace — quantities/pricing are locked once ordered).</summary>
        Task SaveLineItemsAsync(IEnumerable<SalesOrderLineItem> lineItems);
    }
}
