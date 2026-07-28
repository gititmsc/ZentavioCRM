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

        private static readonly LeadStatus[] TerminalStatuses = [LeadStatus.Converted, LeadStatus.Lost, LeadStatus.Junk];

        public Task<int> CountOpenAsync()
            => _dbContext.Leads.CountAsync(l => !TerminalStatuses.Contains(l.Status));

        public Task<int> CountConvertedBetweenAsync(DateTime fromUtc, DateTime toUtcExclusive)
            => _dbContext.Leads.CountAsync(l =>
                l.Status == LeadStatus.Converted &&
                l.ConvertedAtUtc != null &&
                l.ConvertedAtUtc >= fromUtc &&
                l.ConvertedAtUtc < toUtcExclusive);

        public async Task<IReadOnlyList<Lead>> FindPotentialDuplicatesAsync(string? email, string? mobile, Guid? excludeLeadId)
        {
            if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(mobile))
            {
                return [];
            }

            var normalizedEmail = email?.Trim().ToLower();
            var normalizedMobile = mobile?.Trim();

            var matches = await _dbContext.Leads
                .Where(l => excludeLeadId == null || l.Id != excludeLeadId)
                .Where(l =>
                    (normalizedEmail != null && l.Email != null && l.Email.ToLower() == normalizedEmail) ||
                    (normalizedMobile != null && l.Mobile != null && l.Mobile == normalizedMobile))
                .ToListAsync();

            return matches;
        }

        public async Task<IReadOnlyList<Lead>> GetAllAsync()
        {
            var leads = await _dbContext.Leads
                .Include(l => l.AssignedToUser)
                .OrderByDescending(l => l.CreatedAtUtc)
                .ToListAsync();

            return leads;
        }

        public async Task<IReadOnlyList<Lead>> GetDueForFollowUpReminderAsync(Guid userId, DateTime nowUtc)
            => await _dbContext.Leads
                .Where(l =>
                    l.AssignedToUserId == userId &&
                    !TerminalStatuses.Contains(l.Status) &&
                    l.FollowUpReminderSentAtUtc == null &&
                    l.NextFollowUpDate != null && l.NextFollowUpDate <= nowUtc)
                .ToListAsync();
    }
}
