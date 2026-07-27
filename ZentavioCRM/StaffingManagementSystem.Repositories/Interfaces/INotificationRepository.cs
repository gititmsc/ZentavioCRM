using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface INotificationRepository
    {
        Task AddAsync(Notification notification);

        Task<IReadOnlyList<Notification>> GetRecentForUserAsync(Guid userId, int take = 20);

        Task<int> GetUnreadCountAsync(Guid userId);

        /// <summary>Returns false if no notification with that id belongs to the given user (so a caller can 404/ignore).</summary>
        Task<bool> MarkAsReadAsync(Guid id, Guid userId);

        Task MarkAllAsReadAsync(Guid userId);
    }
}
