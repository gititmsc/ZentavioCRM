using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="ISalesOrderRepository"/>
    public class SalesOrderRepository : ISalesOrderRepository
    {
        private readonly AppDbContext _dbContext;

        public SalesOrderRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<SalesOrder?> GetByIdAsync(Guid id)
            => _dbContext.SalesOrders
                .Include(so => so.Quotation)
                .Include(so => so.Customer)
                .Include(so => so.AssignedToUser)
                .Include(so => so.LineItems)
                .FirstOrDefaultAsync(so => so.Id == id);

        public async Task<(IReadOnlyList<SalesOrder> Items, int TotalCount)> SearchAsync(
            string? search, SalesOrderStatus? status, Guid? customerId, int page, int pageSize,
            string? sortBy = null, bool sortDescending = true)
        {
            var query = _dbContext.SalesOrders
                .Include(so => so.Quotation)
                .Include(so => so.Customer)
                .Include(so => so.AssignedToUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(so =>
                    so.SalesOrderNumber.ToLower().Contains(term) ||
                    so.Customer!.DisplayName.ToLower().Contains(term));
            }

            if (status is not null)
            {
                query = query.Where(so => so.Status == status);
            }

            if (customerId is not null)
            {
                query = query.Where(so => so.CustomerId == customerId);
            }

            var totalCount = await query.CountAsync();

            var items = await ApplySort(query, sortBy, sortDescending)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <summary>
        /// Column-key-driven sort, kept as an explicit switch (not reflection/dynamic-LINQ) so every
        /// sortable column is a real, EF-translatable expression. Unrecognized/null sortBy falls back to CreatedAtUtc.
        /// </summary>
        private static IOrderedQueryable<SalesOrder> ApplySort(IQueryable<SalesOrder> query, string? sortBy, bool sortDescending)
        {
            return sortBy?.Trim().ToLowerInvariant() switch
            {
                "salesordernumber" => sortDescending ? query.OrderByDescending(so => so.SalesOrderNumber) : query.OrderBy(so => so.SalesOrderNumber),
                "quotationnumber" => sortDescending ? query.OrderByDescending(so => so.Quotation!.QuotationNumber) : query.OrderBy(so => so.Quotation!.QuotationNumber),
                "customername" => sortDescending ? query.OrderByDescending(so => so.Customer!.DisplayName) : query.OrderBy(so => so.Customer!.DisplayName),
                "grandtotal" => sortDescending ? query.OrderByDescending(so => so.GrandTotal) : query.OrderBy(so => so.GrandTotal),
                "orderdate" => sortDescending ? query.OrderByDescending(so => so.OrderDate) : query.OrderBy(so => so.OrderDate),
                "expecteddeliverydate" => sortDescending ? query.OrderByDescending(so => so.ExpectedDeliveryDate) : query.OrderBy(so => so.ExpectedDeliveryDate),
                "status" => sortDescending ? query.OrderByDescending(so => so.Status) : query.OrderBy(so => so.Status),
                _ => sortDescending ? query.OrderByDescending(so => so.CreatedAtUtc) : query.OrderBy(so => so.CreatedAtUtc),
            };
        }

        public async Task<string> GetNextSalesOrderNumberAsync()
        {
            var count = await _dbContext.SalesOrders.CountAsync();
            return $"SO-{count + 1:000000}";
        }

        public async Task AddAsync(SalesOrder salesOrder)
        {
            _dbContext.SalesOrders.Add(salesOrder);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(SalesOrder salesOrder)
        {
            _dbContext.SalesOrders.Update(salesOrder);
            await _dbContext.SaveChangesAsync();
        }

        public async Task SaveLineItemsAsync(IEnumerable<SalesOrderLineItem> lineItems)
        {
            _dbContext.SalesOrderLineItems.UpdateRange(lineItems);
            await _dbContext.SaveChangesAsync();
        }
    }
}
