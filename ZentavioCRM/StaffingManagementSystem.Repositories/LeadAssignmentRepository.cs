using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="ILeadAssignmentRepository"/>
    public class LeadAssignmentRepository : ILeadAssignmentRepository
    {
        private readonly AppDbContext _dbContext;

        public LeadAssignmentRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<LeadAssignmentSettings> GetOrCreateSettingsAsync()
        {
            var settings = await _dbContext.LeadAssignmentSettings.FirstOrDefaultAsync();
            if (settings is not null)
            {
                return settings;
            }

            // Lazily create the singleton row — only reachable for a tenant database provisioned
            // before this feature existed, since TenantSchema.sql now seeds this row (AutoAssignEnabled
            // = false) for every newly-provisioned tenant.
            settings = new LeadAssignmentSettings { Id = SeedIds.LeadAssignmentSettingsId };
            _dbContext.LeadAssignmentSettings.Add(settings);
            await _dbContext.SaveChangesAsync();
            return settings;
        }

        public async Task UpdateSettingsAsync(LeadAssignmentSettings settings)
        {
            _dbContext.LeadAssignmentSettings.Update(settings);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<LeadAssignmentRule>> GetAllRulesAsync()
            => await _dbContext.LeadAssignmentRules
                .Include(r => r.Territory)
                .Include(r => r.EligibleUsers).ThenInclude(ru => ru.User)
                .Include(r => r.LastAssignedUser)
                .OrderBy(r => r.Territory == null ? 0 : 1)
                .ThenBy(r => r.Territory!.Name)
                .ToListAsync();

        public Task<LeadAssignmentRule?> GetRuleByIdAsync(Guid id)
            => _dbContext.LeadAssignmentRules
                .Include(r => r.Territory)
                .Include(r => r.EligibleUsers).ThenInclude(ru => ru.User)
                .FirstOrDefaultAsync(r => r.Id == id);

        public async Task<LeadAssignmentRule?> GetActiveRuleForTerritoryAsync(Guid? territoryId)
        {
            if (territoryId is not null)
            {
                var territoryRule = await _dbContext.LeadAssignmentRules
                    .FirstOrDefaultAsync(r => r.TerritoryId == territoryId && r.IsActive);
                if (territoryRule is not null)
                {
                    return territoryRule;
                }
            }

            return await _dbContext.LeadAssignmentRules
                .FirstOrDefaultAsync(r => r.TerritoryId == null && r.IsActive);
        }

        public async Task AddRuleAsync(LeadAssignmentRule rule)
        {
            _dbContext.LeadAssignmentRules.Add(rule);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateRuleAsync(LeadAssignmentRule rule)
        {
            _dbContext.LeadAssignmentRules.Update(rule);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteRuleAsync(LeadAssignmentRule rule)
        {
            // LeadAssignmentRuleUsers rows cascade-delete at the database level.
            _dbContext.LeadAssignmentRules.Remove(rule);
            await _dbContext.SaveChangesAsync();
        }

        public async Task ReplaceRuleUsersAsync(Guid ruleId, IReadOnlyCollection<Guid> userIds)
        {
            var existing = await _dbContext.LeadAssignmentRuleUsers.Where(ru => ru.RuleId == ruleId).ToListAsync();
            var existingUserIds = existing.Select(ru => ru.UserId).ToHashSet();
            var requestedUserIds = userIds.ToHashSet();

            var toRemove = existing.Where(ru => !requestedUserIds.Contains(ru.UserId));
            _dbContext.LeadAssignmentRuleUsers.RemoveRange(toRemove);

            var toAdd = requestedUserIds.Where(id => !existingUserIds.Contains(id))
                .Select(id => new LeadAssignmentRuleUser { RuleId = ruleId, UserId = id });
            _dbContext.LeadAssignmentRuleUsers.AddRange(toAdd);

            await _dbContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<Guid>> GetEligibleUserIdsAsync(Guid ruleId)
            => await _dbContext.LeadAssignmentRuleUsers
                .Where(ru => ru.RuleId == ruleId)
                .Join(_dbContext.Users.Where(u => u.IsActive), ru => ru.UserId, u => u.Id, (ru, u) => u.Id)
                .OrderBy(id => id)
                .ToListAsync();

        public async Task AdvanceCursorAsync(Guid ruleId, Guid assignedUserId, DateTime atUtc)
        {
            var rule = await _dbContext.LeadAssignmentRules.FirstOrDefaultAsync(r => r.Id == ruleId);
            if (rule is null)
            {
                return;
            }

            rule.LastAssignedUserId = assignedUserId;
            rule.LastAssignedAtUtc = atUtc;
            await _dbContext.SaveChangesAsync();
        }
    }
}
