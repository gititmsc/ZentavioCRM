using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Leads;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="ILeadAssignmentService"/>
    public class LeadAssignmentService : ILeadAssignmentService
    {
        private const string SettingsEntityType = "LeadAssignmentSettings";
        private const string RuleEntityType = "LeadAssignmentRule";

        private readonly ILeadAssignmentRepository _repository;
        private readonly ITerritoryRepository _territoryRepository;
        private readonly IAuditLogService _auditLogService;

        public LeadAssignmentService(
            ILeadAssignmentRepository repository,
            ITerritoryRepository territoryRepository,
            IAuditLogService auditLogService)
        {
            _repository = repository;
            _territoryRepository = territoryRepository;
            _auditLogService = auditLogService;
        }

        public async Task<LeadAssignmentSettingsDto> GetSettingsAsync()
        {
            var settings = await _repository.GetOrCreateSettingsAsync();
            return MapSettings(settings);
        }

        public async Task<ApiResponse<LeadAssignmentSettingsDto>> UpdateSettingsAsync(LeadAssignmentSettingsDto request, Guid? currentUserId)
        {
            var settings = await _repository.GetOrCreateSettingsAsync();
            settings.AutoAssignEnabled = request.AutoAssignEnabled;
            settings.UpdatedByUserId = currentUserId;
            settings.UpdatedAtUtc = DateTime.UtcNow;

            await _repository.UpdateSettingsAsync(settings);
            await _auditLogService.LogAsync(
                SettingsEntityType, settings.Id, "Updated",
                $"Lead auto-assignment turned {(settings.AutoAssignEnabled ? "on" : "off")}.",
                currentUserId);

            return ApiResponse<LeadAssignmentSettingsDto>.SuccessResponse(MapSettings(settings), "Lead assignment settings saved.");
        }

        public async Task<IReadOnlyList<LeadAssignmentRuleDto>> GetRulesAsync()
        {
            var rules = await _repository.GetAllRulesAsync();
            return rules.Select(MapRule).ToList();
        }

        public async Task<ApiResponse<LeadAssignmentRuleDto>> CreateRuleAsync(SaveLeadAssignmentRuleRequest request, Guid? currentUserId)
        {
            var validationError = await ValidateAsync(request, excludeRuleId: null);
            if (validationError is not null)
            {
                return ApiResponse<LeadAssignmentRuleDto>.FailureResponse(validationError);
            }

            var rule = new LeadAssignmentRule
            {
                TerritoryId = request.TerritoryId,
                IsActive = request.IsActive,
                CreatedByUserId = currentUserId,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _repository.AddRuleAsync(rule);
            await _repository.ReplaceRuleUsersAsync(rule.Id, request.EligibleUserIds);
            await _auditLogService.LogAsync(RuleEntityType, rule.Id, "Created", DescribeRule(rule), currentUserId);

            var created = await _repository.GetRuleByIdAsync(rule.Id);
            return ApiResponse<LeadAssignmentRuleDto>.SuccessResponse(MapRule(created!), "Assignment rule created.");
        }

        public async Task<ApiResponse<LeadAssignmentRuleDto>> UpdateRuleAsync(Guid id, SaveLeadAssignmentRuleRequest request, Guid? currentUserId)
        {
            var rule = await _repository.GetRuleByIdAsync(id);
            if (rule is null)
            {
                return ApiResponse<LeadAssignmentRuleDto>.FailureResponse("Assignment rule not found.");
            }

            var validationError = await ValidateAsync(request, excludeRuleId: id);
            if (validationError is not null)
            {
                return ApiResponse<LeadAssignmentRuleDto>.FailureResponse(validationError);
            }

            rule.TerritoryId = request.TerritoryId;
            rule.IsActive = request.IsActive;
            rule.UpdatedAtUtc = DateTime.UtcNow;

            await _repository.UpdateRuleAsync(rule);
            await _repository.ReplaceRuleUsersAsync(rule.Id, request.EligibleUserIds);
            await _auditLogService.LogAsync(RuleEntityType, rule.Id, "Updated", DescribeRule(rule), currentUserId);

            var updated = await _repository.GetRuleByIdAsync(id);
            return ApiResponse<LeadAssignmentRuleDto>.SuccessResponse(MapRule(updated!), "Assignment rule updated.");
        }

        public async Task<ApiResponse<bool>> DeleteRuleAsync(Guid id, Guid? currentUserId)
        {
            var rule = await _repository.GetRuleByIdAsync(id);
            if (rule is null)
            {
                return ApiResponse<bool>.FailureResponse("Assignment rule not found.");
            }

            await _repository.DeleteRuleAsync(rule);
            await _auditLogService.LogAsync(RuleEntityType, id, "Deleted", DescribeRule(rule) + " deleted.", currentUserId);

            return ApiResponse<bool>.SuccessResponse(true, "Assignment rule deleted.");
        }

        public async Task<Guid?> PickAssigneeAsync(Guid? leadTerritoryId)
        {
            var settings = await _repository.GetOrCreateSettingsAsync();
            if (!settings.AutoAssignEnabled)
            {
                return null;
            }

            var rule = await _repository.GetActiveRuleForTerritoryAsync(leadTerritoryId);
            if (rule is null)
            {
                return null;
            }

            var eligibleUserIds = await _repository.GetEligibleUserIdsAsync(rule.Id);
            if (eligibleUserIds.Count == 0)
            {
                return null;
            }

            Guid chosen;
            if (rule.LastAssignedUserId is null)
            {
                chosen = eligibleUserIds[0];
            }
            else
            {
                var lastIndex = eligibleUserIds.IndexOf(rule.LastAssignedUserId.Value);
                chosen = lastIndex < 0 || lastIndex == eligibleUserIds.Count - 1
                    ? eligibleUserIds[0]
                    : eligibleUserIds[lastIndex + 1];
            }

            await _repository.AdvanceCursorAsync(rule.Id, chosen, DateTime.UtcNow);
            return chosen;
        }

        /// <summary>Null means valid. A rule's TerritoryId (including null, the tenant-wide fallback) must be unique — at most one rule per territory and at most one fallback rule.</summary>
        private async Task<string?> ValidateAsync(SaveLeadAssignmentRuleRequest request, Guid? excludeRuleId)
        {
            if (request.EligibleUserIds.Count == 0)
            {
                return "Pick at least one eligible user.";
            }

            if (request.TerritoryId is not null)
            {
                var territory = await _territoryRepository.GetByIdAsync(request.TerritoryId.Value);
                if (territory is null)
                {
                    return "The selected territory could not be found.";
                }
            }

            var existingRules = await _repository.GetAllRulesAsync();
            var duplicate = existingRules.Any(r => r.Id != excludeRuleId && r.TerritoryId == request.TerritoryId);
            if (duplicate)
            {
                return request.TerritoryId is null
                    ? "A tenant-wide fallback rule already exists. Edit it instead of creating another."
                    : "A rule already exists for this territory. Edit it instead of creating another.";
            }

            return null;
        }

        private static string DescribeRule(LeadAssignmentRule rule)
            => rule.TerritoryId is null ? "Tenant-wide fallback rule" : $"Rule for territory {rule.TerritoryId}";

        private static LeadAssignmentSettingsDto MapSettings(LeadAssignmentSettings settings) => new()
        {
            AutoAssignEnabled = settings.AutoAssignEnabled,
            UpdatedAtUtc = settings.UpdatedAtUtc,
        };

        private static LeadAssignmentRuleDto MapRule(LeadAssignmentRule rule) => new()
        {
            Id = rule.Id,
            TerritoryId = rule.TerritoryId,
            TerritoryName = rule.Territory?.Name,
            IsActive = rule.IsActive,
            EligibleUsers = rule.EligibleUsers
                .Where(ru => ru.User is not null)
                .Select(ru => new LeadAssignmentRuleUserDto { Id = ru.User!.Id, FullName = ru.User.FullName })
                .OrderBy(u => u.FullName)
                .ToList(),
            LastAssignedUserId = rule.LastAssignedUserId,
            LastAssignedUserName = rule.LastAssignedUser?.FullName,
            LastAssignedAtUtc = rule.LastAssignedAtUtc,
            CreatedAtUtc = rule.CreatedAtUtc,
        };
    }
}
