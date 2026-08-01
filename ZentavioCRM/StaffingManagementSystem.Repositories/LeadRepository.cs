using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Core.Security;
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
            => _dbContext.Leads.Include(l => l.AssignedToUser).Include(l => l.TerritoryRef).FirstOrDefaultAsync(l => l.Id == id);

        public async Task<(IReadOnlyList<Lead> Items, int TotalCount)> SearchAsync(
            string? search, LeadStatus? status, Guid? assignedToUserId, int page, int pageSize,
            AccessScope? accessScope = null, string? sortBy = null, bool sortDescending = true)
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

            query = ApplyAccessScope(query, accessScope);

            var totalCount = await query.CountAsync();

            var items = await ApplySort(query, sortBy, sortDescending)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <summary>
        /// Column-key-driven sort, kept as an explicit switch (not reflection/dynamic-LINQ) so every
        /// sortable column is a real, EF-translatable expression and arbitrary client input can never
        /// reach raw SQL. Unrecognized/null sortBy falls back to CreatedAtUtc.
        /// </summary>
        private static IOrderedQueryable<Lead> ApplySort(IQueryable<Lead> query, string? sortBy, bool sortDescending)
        {
            return sortBy?.Trim().ToLowerInvariant() switch
            {
                "leadnumber" => sortDescending ? query.OrderByDescending(l => l.LeadNumber) : query.OrderBy(l => l.LeadNumber),
                "companyname" => sortDescending ? query.OrderByDescending(l => l.CompanyName) : query.OrderBy(l => l.CompanyName),
                "contactname" => sortDescending ? query.OrderByDescending(l => l.ContactName) : query.OrderBy(l => l.ContactName),
                "source" => sortDescending ? query.OrderByDescending(l => l.Source) : query.OrderBy(l => l.Source),
                "expectedvalue" => sortDescending ? query.OrderByDescending(l => l.ExpectedValue) : query.OrderBy(l => l.ExpectedValue),
                "assignedtousername" => sortDescending
                    ? query.OrderByDescending(l => l.AssignedToUser!.FirstName).ThenByDescending(l => l.AssignedToUser!.LastName)
                    : query.OrderBy(l => l.AssignedToUser!.FirstName).ThenBy(l => l.AssignedToUser!.LastName),
                "status" => sortDescending ? query.OrderByDescending(l => l.Status) : query.OrderBy(l => l.Status),
                _ => sortDescending ? query.OrderByDescending(l => l.CreatedAtUtc) : query.OrderBy(l => l.CreatedAtUtc),
            };
        }

        /// <summary>Shared Own/Team/All + delegation filter, reused by SearchAsync and the dashboard count methods below.</summary>
        private static IQueryable<Lead> ApplyAccessScope(IQueryable<Lead> query, AccessScope? accessScope)
        {
            if (accessScope is null || accessScope.Scope == VisibilityScope.All)
            {
                return query;
            }

            var currentUserId = accessScope.UserId;
            // .ToHashSet() materializes to a concrete HashSet<Guid> (ICollection<Guid>) — EF Core's
            // Contains -> SQL IN translation is resolved from the compile-time type, and the source
            // properties are declared as IReadOnlySet<Guid>, which isn't covered by that translation.
            var teamIds = accessScope.TeamUserIds.ToHashSet();
            var delegatedIds = accessScope.DelegatedFromUserIds.ToHashSet();

            return accessScope.Scope == VisibilityScope.Team
                ? query.Where(l =>
                    (l.AssignedToUserId != null && teamIds.Contains(l.AssignedToUserId.Value)) ||
                    (l.AssignedToUserId == null && l.CreatedByUserId != null && teamIds.Contains(l.CreatedByUserId.Value)) ||
                    (l.AssignedToUserId != null && delegatedIds.Contains(l.AssignedToUserId.Value)))
                : query.Where(l =>
                    l.AssignedToUserId == currentUserId ||
                    (l.AssignedToUserId == null && l.CreatedByUserId == currentUserId) ||
                    (l.AssignedToUserId != null && delegatedIds.Contains(l.AssignedToUserId.Value)));
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

        public Task<int> CountOpenAsync(AccessScope? accessScope = null)
        {
            var query = _dbContext.Leads.Where(l => !TerminalStatuses.Contains(l.Status));
            query = ApplyAccessScope(query, accessScope);
            return query.CountAsync();
        }

        public Task<int> CountConvertedBetweenAsync(DateTime fromUtc, DateTime toUtcExclusive, AccessScope? accessScope = null)
        {
            var query = _dbContext.Leads.Where(l =>
                l.Status == LeadStatus.Converted &&
                l.ConvertedAtUtc != null &&
                l.ConvertedAtUtc >= fromUtc &&
                l.ConvertedAtUtc < toUtcExclusive);
            query = ApplyAccessScope(query, accessScope);
            return query.CountAsync();
        }

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
