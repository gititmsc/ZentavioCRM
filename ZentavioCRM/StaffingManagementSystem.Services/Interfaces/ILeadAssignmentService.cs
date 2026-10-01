using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Leads;

namespace ZentavioCRM.Services.Interfaces
{
    public interface ILeadAssignmentService
    {
        Task<LeadAssignmentSettingsDto> GetSettingsAsync();

        Task<ApiResponse<LeadAssignmentSettingsDto>> UpdateSettingsAsync(LeadAssignmentSettingsDto request, Guid? currentUserId);

        Task<IReadOnlyList<LeadAssignmentRuleDto>> GetRulesAsync();

        Task<ApiResponse<LeadAssignmentRuleDto>> CreateRuleAsync(SaveLeadAssignmentRuleRequest request, Guid? currentUserId);

        Task<ApiResponse<LeadAssignmentRuleDto>> UpdateRuleAsync(Guid id, SaveLeadAssignmentRuleRequest request, Guid? currentUserId);

        Task<ApiResponse<bool>> DeleteRuleAsync(Guid id, Guid? currentUserId);

        /// <summary>
        /// Picks the next eligible user for a newly-created lead with the given territory, via
        /// round-robin over the matching rule's eligible-user pool (territory-specific rule first,
        /// falling back to the tenant-wide rule), advancing that rule's cursor. Returns null — leaving
        /// the lead unassigned, same as today — when auto-assign is off, or no active rule exists for
        /// the territory (with no fallback either), or the matching rule has no eligible users.
        /// </summary>
        Task<Guid?> PickAssigneeAsync(Guid? leadTerritoryId);
    }
}
