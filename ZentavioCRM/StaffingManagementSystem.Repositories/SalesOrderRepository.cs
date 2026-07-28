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
            string? search, SalesOrderStatus? status, Guid? customerId, int page, int pageSize)
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

            var items = await query
                .OrderByDescending(so => so.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
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
