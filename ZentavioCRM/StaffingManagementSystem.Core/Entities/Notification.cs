using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// An in-app notification for one user (assignment, a deal closing, etc.). Delivered via
    /// polling from the frontend — no push/SignalR in this milestone.
    /// </summary>
    public class Notification
    {
        public Guid Id { get; set; }

        public Guid RecipientUserId { get; set; }

        public User? RecipientUser { get; set; }

        public string Message { get; set; } = string.Empty;

        public RelatedEntityType? RelatedEntityType { get; set; }

        public Guid? RelatedEntityId { get; set; }

        public bool IsRead { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
