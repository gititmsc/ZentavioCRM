using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Core.Security;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface ISalesOrderRepository
    {
        Task<SalesOrder?> GetByIdAsync(Guid id);

        /// <param name="accessScope">When non-null and Scope != All, restricts results to records the scope's user is allowed to see (Own/Team, plus any active delegations).</param>
        /// <param name="sortBy">Column key (case-insensitive): salesOrderNumber, quotationNumber, customerName, grandTotal, orderDate, expectedDeliveryDate, status, createdAtUtc. Unrecognized/null falls back to createdAtUtc.</param>
        Task<(IReadOnlyList<SalesOrder> Items, int TotalCount)> SearchAsync(
            string? search, SalesOrderStatus? status, Guid? customerId, int page, int pageSize,
            AccessScope? accessScope = null, string? sortBy = null, bool sortDescending = true);

        Task<string> GetNextSalesOrderNumberAsync();

        Task AddAsync(SalesOrder salesOrder);

        Task UpdateAsync(SalesOrder salesOrder);

        /// <summary>Persists updated DeliveredQuantity values on the order's existing line items (no full replace — quantities/pricing are locked once ordered).</summary>
        Task SaveLineItemsAsync(IEnumerable<SalesOrderLineItem> lineItems);
    }
}
