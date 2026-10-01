using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Repositories.Interfaces
{
    /// <summary>Data access for the Lead auto-assignment feature: the tenant-wide on/off settings row plus the round-robin routing rules and their eligible-user pools.</summary>
    public interface ILeadAssignmentRepository
    {
        /// <summary>Returns the tenant's single settings row, creating it (AutoAssignEnabled = false) if it's somehow missing — same lazy-create pattern as ILeadScoringSettingsRepository.</summary>
        Task<LeadAssignmentSettings> GetOrCreateSettingsAsync();

        Task UpdateSettingsAsync(LeadAssignmentSettings settings);

        /// <summary>Every rule, Territory and EligibleUsers (with User) eagerly included — powers the rules-management list.</summary>
        Task<IReadOnlyList<LeadAssignmentRule>> GetAllRulesAsync();

        Task<LeadAssignmentRule?> GetRuleByIdAsync(Guid id);

        /// <summary>The active rule to use for a lead with the given territory: a territory-specific active rule if one exists, otherwise the active rule with a null TerritoryId (the tenant-wide fallback). Returns null if no matching rule exists — the caller leaves the lead unassigned in that case.</summary>
        Task<LeadAssignmentRule?> GetActiveRuleForTerritoryAsync(Guid? territoryId);

        Task AddRuleAsync(LeadAssignmentRule rule);

        Task UpdateRuleAsync(LeadAssignmentRule rule);

        Task DeleteRuleAsync(LeadAssignmentRule rule);

        /// <summary>Replaces a rule's eligible-user pool with the given set (diff add/remove, same pattern as ITagRepository.ReplaceLeadTagsAsync).</summary>
        Task ReplaceRuleUsersAsync(Guid ruleId, IReadOnlyCollection<Guid> userIds);

        /// <summary>Ids of the rule's eligible users, restricted to currently-active users, ordered by Id for a stable round-robin sequence.</summary>
        Task<IReadOnlyList<Guid>> GetEligibleUserIdsAsync(Guid ruleId);

        /// <summary>Advances the round-robin cursor after a successful assignment.</summary>
        Task AdvanceCursorAsync(Guid ruleId, Guid assignedUserId, DateTime atUtc);
    }
}
