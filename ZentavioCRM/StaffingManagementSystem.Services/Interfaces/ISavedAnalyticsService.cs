using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Analytics;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Services.Interfaces
{
    /// <summary>
    /// CRUD for user-built dashboards and saved reports. Anyone may create/edit/delete their own; an item marked
    /// shared is visible to the whole tenant but only editable by its owner or a holder of Analytics.ManageShared
    /// (<paramref name="canManageShared"/> — resolved from the caller's permission claims by the controller).
    /// </summary>
    public interface ISavedAnalyticsService
    {
        Task<IReadOnlyList<SavedAnalyticsItemDto>> ListAsync(SavedAnalyticsKind kind, Guid userId, bool canManageShared);

        /// <summary>Null when the item doesn't exist or isn't visible to the user (private and owned by someone else).</summary>
        Task<SavedAnalyticsItemDto?> GetAsync(Guid id, Guid userId, bool canManageShared);

        Task<ApiResponse<SavedAnalyticsItemDto>> CreateAsync(SaveAnalyticsItemRequest request, Guid userId, bool canManageShared);

        Task<ApiResponse<SavedAnalyticsItemDto>> UpdateAsync(Guid id, SaveAnalyticsItemRequest request, Guid userId, bool canManageShared);

        Task<ApiResponse<bool>> DeleteAsync(Guid id, Guid userId, bool canManageShared);
    }
}
