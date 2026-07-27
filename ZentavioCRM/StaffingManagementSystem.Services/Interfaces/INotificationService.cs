using ZentavioCRM.Core.DTOs.Notifications;
using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Services.Interfaces
{
    public interface INotificationService
    {
        Task NotifyAsync(Guid recipientUserId, string message, RelatedEntityType? relatedEntityType, Guid? relatedEntityId);

        Task<IReadOnlyList<NotificationDto>> GetRecentAsync(Guid userId);

        Task<int> GetUnreadCountAsync(Guid userId);

        Task<bool> MarkAsReadAsync(Guid id, Guid userId);

        Task MarkAllAsReadAsync(Guid userId);
    }
}
