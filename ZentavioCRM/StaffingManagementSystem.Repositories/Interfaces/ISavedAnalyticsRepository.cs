using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface ISavedAnalyticsRepository
    {
        /// <summary>Items of the given kind the user owns, plus every shared one — owner navigation included for display names.</summary>
        Task<IReadOnlyList<SavedAnalyticsItem>> GetVisibleAsync(SavedAnalyticsKind kind, Guid userId);

        Task<SavedAnalyticsItem?> GetByIdAsync(Guid id);

        Task AddAsync(SavedAnalyticsItem item);

        Task UpdateAsync(SavedAnalyticsItem item);

        Task DeleteAsync(SavedAnalyticsItem item);
    }
}
