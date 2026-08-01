using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="IQuotationRepository"/>
    public class QuotationRepository : IQuotationRepository
    {
        private readonly AppDbContext _dbContext;

        public QuotationRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Quotation?> GetByIdAsync(Guid id)
            => _dbContext.Quotations
                .Include(q => q.Opportunity)
                .Include(q => q.Customer)
                .Include(q => q.AssignedToUser)
                .Include(q => q.LineItems)
                .FirstOrDefaultAsync(q => q.Id == id);

        public async Task<(IReadOnlyList<Quotation> Items, int TotalCount)> SearchAsync(
            string? search, QuotationStatus? status, Guid? opportunityId, Guid? customerId, int page, int pageSize,
            string? sortBy = null, bool sortDescending = true)
        {
            var query = _dbContext.Quotations
                .Include(q => q.Opportunity)
                .Include(q => q.Customer)
                .Include(q => q.AssignedToUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(q =>
                    q.QuotationNumber.ToLower().Contains(term) ||
                    q.Customer!.DisplayName.ToLower().Contains(term) ||
                    q.Opportunity!.Name.ToLower().Contains(term));
            }

            if (status is not null)
            {
                query = query.Where(q => q.Status == status);
            }

            if (opportunityId is not null)
            {
                query = query.Where(q => q.OpportunityId == opportunityId);
            }

            if (customerId is not null)
            {
                query = query.Where(q => q.CustomerId == customerId);
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
        private static IOrderedQueryable<Quotation> ApplySort(IQueryable<Quotation> query, string? sortBy, bool sortDescending)
        {
            return sortBy?.Trim().ToLowerInvariant() switch
            {
                "quotationnumber" => sortDescending ? query.OrderByDescending(q => q.QuotationNumber) : query.OrderBy(q => q.QuotationNumber),
                "opportunityname" => sortDescending ? query.OrderByDescending(q => q.Opportunity!.Name) : query.OrderBy(q => q.Opportunity!.Name),
                "customername" => sortDescending ? query.OrderByDescending(q => q.Customer!.DisplayName) : query.OrderBy(q => q.Customer!.DisplayName),
                "grandtotal" => sortDescending ? query.OrderByDescending(q => q.GrandTotal) : query.OrderBy(q => q.GrandTotal),
                "validuntil" => sortDescending ? query.OrderByDescending(q => q.ValidUntil) : query.OrderBy(q => q.ValidUntil),
                "assignedtousername" => sortDescending
                    ? query.OrderByDescending(q => q.AssignedToUser!.FirstName).ThenByDescending(q => q.AssignedToUser!.LastName)
                    : query.OrderBy(q => q.AssignedToUser!.FirstName).ThenBy(q => q.AssignedToUser!.LastName),
                "status" => sortDescending ? query.OrderByDescending(q => q.Status) : query.OrderBy(q => q.Status),
                _ => sortDescending ? query.OrderByDescending(q => q.CreatedAtUtc) : query.OrderBy(q => q.CreatedAtUtc),
            };
        }

        public async Task<IReadOnlyList<Quotation>> GetVersionsAsync(string quotationNumber)
            => await _dbContext.Quotations
                .Where(q => q.QuotationNumber == quotationNumber)
                .OrderByDescending(q => q.Version)
                .ToListAsync();

        public async Task<string> GetNextQuotationNumberAsync()
        {
            var count = await _dbContext.Quotations.Select(q => q.QuotationNumber).Distinct().CountAsync();
            return $"QUO-{count + 1:000000}";
        }

        public Task<bool> HasSalesOrderAsync(Guid quotationId)
            => _dbContext.SalesOrders.AnyAsync(so => so.QuotationId == quotationId);

        public Task<bool> HasAnyForOpportunityAsync(Guid opportunityId)
            => _dbContext.Quotations.AnyAsync(q => q.OpportunityId == opportunityId);

        public async Task AddAsync(Quotation quotation)
        {
            _dbContext.Quotations.Add(quotation);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Quotation quotation)
        {
            _dbContext.Quotations.Update(quotation);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Quotation quotation)
        {
            _dbContext.Quotations.Remove(quotation);
            await _dbContext.SaveChangesAsync();
        }

        public async Task ReplaceLineItemsAsync(Guid quotationId, IEnumerable<QuotationLineItem> lineItems)
        {
            var existing = await _dbContext.QuotationLineItems.Where(li => li.QuotationId == quotationId).ToListAsync();
            _dbContext.QuotationLineItems.RemoveRange(existing);

            foreach (var lineItem in lineItems)
            {
                lineItem.Id = Guid.Empty;
                lineItem.QuotationId = quotationId;
                _dbContext.QuotationLineItems.Add(lineItem);
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
