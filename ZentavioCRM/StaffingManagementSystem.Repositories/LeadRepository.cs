using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="ILeadRepository"/>
    public class LeadRepository : ILeadRepository
    {
        private readonly AppDbContext _dbContext;

        public LeadRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Lead?> GetByIdAsync(Guid id)
            => _dbContext.Leads.Include(l => l.AssignedToUser).FirstOrDefaultAsync(l => l.Id == id);

        public async Task<(IReadOnlyList<Lead> Items, int TotalCount)> SearchAsync(
            string? search, LeadStatus? status, Guid? assignedToUserId, int page, int pageSize)
        {
            var query = _dbContext.Leads.Include(l => l.AssignedToUser).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(l =>
                    l.CompanyName.ToLower().Contains(term) ||
                    l.ContactName.ToLower().Contains(term) ||
                    l.LeadNumber.ToLower().Contains(term) ||
                    (l.Email != null && l.Email.ToLower().Contains(term)));
            }

            if (status is not null)
            {
                query = query.Where(l => l.Status == status);
            }

            if (assignedToUserId is not null)
            {
                query = query.Where(l => l.AssignedToUserId == assignedToUserId);
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(l => l.CreatedAtUtc)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<string> GetNextLeadNumberAsync()
        {
            var count = await _dbContext.Leads.CountAsync();
            return $"LEAD-{count + 1:000000}";
        }

        public async Task AddAsync(Lead lead)
        {
            _dbContext.Leads.Add(lead);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Lead lead)
        {
            _dbContext.Leads.Update(lead);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Lead lead)
        {
            _dbContext.Leads.Remove(lead);
            await _dbContext.SaveChangesAsync();
        }
    }
}
