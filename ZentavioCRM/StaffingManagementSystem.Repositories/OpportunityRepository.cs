using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="IOpportunityRepository"/>
    public class OpportunityRepository : IOpportunityRepository
    {
        private readonly AppDbContext _dbContext;

        public OpportunityRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Opportunity?> GetByIdAsync(Guid id)
            => _dbContext.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.AssignedToUser)
                .Include(o => o.LineItems)
                .Include(o => o.Contacts).ThenInclude(oc => oc.ContactPerson)
                .FirstOrDefaultAsync(o => o.Id == id);

        public async Task<(IReadOnlyList<Opportunity> Items, int TotalCount)> SearchAsync(
            string? search, OpportunityStage? stage, Guid? customerId, Guid? assignedToUserId, int page, int pageSize)
        {
            var query = _dbContext.Opportunities
                .Include(o => o.Customer)
                .Include(o => o.AssignedToUser)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(o =>
                    o.Name.ToLower().Contains(term) ||
                    o.OpportunityNumber.ToLower().Contains(term) ||
                    o.Customer!.DisplayName.ToLower().Contains(term));
            }

            if (stage is not null)
            {
                query = query.Where(o => o.Stage == stage);
            }

            if (customerId is not null)
            {
                query = query.Where(o => o.CustomerId == customerId);
            }

            if (assignedToUserId is not null)
            {
                query = query.Where(o => o.AssignedToUserId == assignedToUserId);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(o => o.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<string> GetNextOpportunityNumberAsync()
        {
            var count = await _dbContext.Opportunities.CountAsync();
            return $"OPP-{count + 1:000000}";
        }

        public async Task AddAsync(Opportunity opportunity)
        {
            _dbContext.Opportunities.Add(opportunity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Opportunity opportunity)
        {
            _dbContext.Opportunities.Update(opportunity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Opportunity opportunity)
        {
            _dbContext.Opportunities.Remove(opportunity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<Opportunity>> GetAllForDashboardAsync()
        {
            var opportunities = await _dbContext.Opportunities
                .AsNoTracking()
                .Select(o => new Opportunity { Stage = o.Stage, Value = o.Value })
                .ToListAsync();

            return opportunities;
        }

        public async Task ReplaceLineItemsAsync(Guid opportunityId, IEnumerable<OpportunityLineItem> lineItems)
        {
            var existing = await _dbContext.OpportunityLineItems.Where(li => li.OpportunityId == opportunityId).ToListAsync();
            _dbContext.OpportunityLineItems.RemoveRange(existing);

            foreach (var lineItem in lineItems)
            {
                lineItem.Id = Guid.Empty;
                lineItem.OpportunityId = opportunityId;
                _dbContext.OpportunityLineItems.Add(lineItem);
            }

            await _dbContext.SaveChangesAsync();
        }

        public async Task ReplaceContactsAsync(Guid opportunityId, IEnumerable<OpportunityContact> contacts)
        {
            var existing = await _dbContext.OpportunityContacts.Where(oc => oc.OpportunityId == opportunityId).ToListAsync();
            _dbContext.OpportunityContacts.RemoveRange(existing);

            foreach (var contact in contacts)
            {
                contact.Id = Guid.Empty;
                contact.OpportunityId = opportunityId;
                _dbContext.OpportunityContacts.Add(contact);
            }

            await _dbContext.SaveChangesAsync();
        }
    }
}
