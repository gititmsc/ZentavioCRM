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
            string? search, QuotationStatus? status, Guid? opportunityId, Guid? customerId, int page, int pageSize)
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

            var items = await query
                .OrderByDescending(q => q.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
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
