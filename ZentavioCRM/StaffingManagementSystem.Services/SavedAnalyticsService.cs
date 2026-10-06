using System.Text.Json;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Analytics;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="ISavedAnalyticsService"/>
    public class SavedAnalyticsService : ISavedAnalyticsService
    {
        private const string EntityType = "SavedAnalyticsItem";

        /// <summary>A dashboard of a few dozen widgets is a few KB; this just stops someone parking megabytes in the column.</summary>
        private const int MaxConfigChars = 200_000;

        private readonly ISavedAnalyticsRepository _repository;
        private readonly IAuditLogService _auditLogService;

        public SavedAnalyticsService(ISavedAnalyticsRepository repository, IAuditLogService auditLogService)
        {
            _repository = repository;
            _auditLogService = auditLogService;
        }

        public async Task<IReadOnlyList<SavedAnalyticsItemDto>> ListAsync(SavedAnalyticsKind kind, Guid userId, bool canManageShared)
        {
            var items = await _repository.GetVisibleAsync(kind, userId);
            return items.Select(i => Map(i, userId, canManageShared)).ToList();
        }

        public async Task<SavedAnalyticsItemDto?> GetAsync(Guid id, Guid userId, bool canManageShared)
        {
            var item = await _repository.GetByIdAsync(id);
            return item is null || !IsVisible(item, userId) ? null : Map(item, userId, canManageShared);
        }

        public async Task<ApiResponse<SavedAnalyticsItemDto>> CreateAsync(SaveAnalyticsItemRequest request, Guid userId, bool canManageShared)
        {
            if (!Enum.IsDefined(request.Kind))
            {
                return ApiResponse<SavedAnalyticsItemDto>.FailureResponse("Unknown item kind.");
            }

            var error = Validate(request) ?? (request.IsShared && !canManageShared ? ShareDenied : null);
            if (error is not null)
            {
                return ApiResponse<SavedAnalyticsItemDto>.FailureResponse(error);
            }

            var item = new SavedAnalyticsItem
            {
                Kind = request.Kind,
                Name = request.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                OwnerUserId = userId,
                IsShared = request.IsShared,
                ConfigJson = request.ConfigJson,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _repository.AddAsync(item);
            await _auditLogService.LogAsync(EntityType, item.Id, "Created", $"{item.Kind} '{item.Name}' created{(item.IsShared ? " (shared)" : string.Empty)}.", userId);

            var created = await _repository.GetByIdAsync(item.Id);
            return ApiResponse<SavedAnalyticsItemDto>.SuccessResponse(Map(created!, userId, canManageShared), $"{item.Kind} saved.");
        }

        public async Task<ApiResponse<SavedAnalyticsItemDto>> UpdateAsync(Guid id, SaveAnalyticsItemRequest request, Guid userId, bool canManageShared)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item is null || !IsVisible(item, userId))
            {
                return ApiResponse<SavedAnalyticsItemDto>.FailureResponse("Item not found.");
            }

            if (!CanEdit(item, userId, canManageShared))
            {
                return ApiResponse<SavedAnalyticsItemDto>.FailureResponse("You can't edit this item.");
            }

            // Changing visibility (either direction) is a sharing decision, so it needs the share permission.
            var error = Validate(request) ?? (request.IsShared != item.IsShared && !canManageShared ? ShareDenied : null);
            if (error is not null)
            {
                return ApiResponse<SavedAnalyticsItemDto>.FailureResponse(error);
            }

            item.Name = request.Name.Trim();
            item.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
            item.IsShared = request.IsShared;
            item.ConfigJson = request.ConfigJson;
            item.UpdatedAtUtc = DateTime.UtcNow;

            await _repository.UpdateAsync(item);
            await _auditLogService.LogAsync(EntityType, item.Id, "Updated", $"{item.Kind} '{item.Name}' updated{(item.IsShared ? " (shared)" : string.Empty)}.", userId);

            return ApiResponse<SavedAnalyticsItemDto>.SuccessResponse(Map(item, userId, canManageShared), $"{item.Kind} saved.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id, Guid userId, bool canManageShared)
        {
            var item = await _repository.GetByIdAsync(id);
            if (item is null || !IsVisible(item, userId))
            {
                return ApiResponse<bool>.FailureResponse("Item not found.");
            }

            if (!CanEdit(item, userId, canManageShared))
            {
                return ApiResponse<bool>.FailureResponse("You can't delete this item.");
            }

            await _repository.DeleteAsync(item);
            await _auditLogService.LogAsync(EntityType, item.Id, "Deleted", $"{item.Kind} '{item.Name}' deleted.", userId);

            return ApiResponse<bool>.SuccessResponse(true, $"{item.Kind} deleted.");
        }

        private const string ShareDenied = "You don't have permission to share items with the whole team.";

        private static bool IsVisible(SavedAnalyticsItem item, Guid userId) => item.IsShared || item.OwnerUserId == userId;

        private static bool CanEdit(SavedAnalyticsItem item, Guid userId, bool canManageShared)
            => item.OwnerUserId == userId || (item.IsShared && canManageShared);

        private static string? Validate(SaveAnalyticsItemRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return "Name is required.";
            }

            if (request.ConfigJson.Length > MaxConfigChars)
            {
                return "That configuration is too large to save.";
            }

            try
            {
                using var document = JsonDocument.Parse(request.ConfigJson);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return "The configuration must be a JSON object.";
                }
            }
            catch (JsonException)
            {
                return "The configuration isn't valid JSON.";
            }

            return null;
        }

        private static SavedAnalyticsItemDto Map(SavedAnalyticsItem item, Guid userId, bool canManageShared) => new()
        {
            Id = item.Id,
            Kind = item.Kind,
            Name = item.Name,
            Description = item.Description,
            OwnerUserId = item.OwnerUserId,
            OwnerName = item.OwnerUser?.FullName ?? string.Empty,
            IsShared = item.IsShared,
            IsMine = item.OwnerUserId == userId,
            CanEdit = CanEdit(item, userId, canManageShared),
            ConfigJson = item.ConfigJson,
            CreatedAtUtc = item.CreatedAtUtc,
            UpdatedAtUtc = item.UpdatedAtUtc,
        };
    }
}
