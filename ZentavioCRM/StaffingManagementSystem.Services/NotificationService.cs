using ZentavioCRM.Core.DTOs.Notifications;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="INotificationService"/>
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository _notificationRepository;

        public NotificationService(INotificationRepository notificationRepository)
        {
            _notificationRepository = notificationRepository;
        }

        public Task NotifyAsync(Guid recipientUserId, string message, RelatedEntityType? relatedEntityType, Guid? relatedEntityId)
            => _notificationRepository.AddAsync(new Notification
            {
                RecipientUserId = recipientUserId,
                Message = message,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId,
                IsRead = false,
                CreatedAtUtc = DateTime.UtcNow,
            });

        public async Task<IReadOnlyList<NotificationDto>> GetRecentAsync(Guid userId)
        {
            var notifications = await _notificationRepository.GetRecentForUserAsync(userId);

            return notifications.Select(n => new NotificationDto
            {
                Id = n.Id,
                Message = n.Message,
                RelatedEntityType = n.RelatedEntityType,
                RelatedEntityId = n.RelatedEntityId,
                IsRead = n.IsRead,
                CreatedAtUtc = n.CreatedAtUtc,
            }).ToList();
        }

        public Task<int> GetUnreadCountAsync(Guid userId) => _notificationRepository.GetUnreadCountAsync(userId);

        public Task<bool> MarkAsReadAsync(Guid id, Guid userId) => _notificationRepository.MarkAsReadAsync(id, userId);

        public Task MarkAllAsReadAsync(Guid userId) => _notificationRepository.MarkAllAsReadAsync(userId);
    }
}
