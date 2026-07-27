using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.DTOs.Notifications
{
    public class NotificationDto
    {
        public Guid Id { get; set; }

        public string Message { get; set; } = string.Empty;

        public RelatedEntityType? RelatedEntityType { get; set; }

        public Guid? RelatedEntityId { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
