using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// A single timeline entry (call, email, meeting, task, note...) attached to any CRM record.
    /// Every module is expected to reuse this rather than inventing its own activity log.
    /// </summary>
    public class Activity
    {
        public Guid Id { get; set; }

        public ActivityType Type { get; set; }

        public string Subject { get; set; } = string.Empty;

        public string? Description { get; set; }

        public RelatedEntityType RelatedToType { get; set; }

        public Guid RelatedToId { get; set; }

        public DateTime? DueAtUtc { get; set; }

        public DateTime? CompletedAtUtc { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public User? AssignedToUser { get; set; }

        public Guid? CreatedByUserId { get; set; }

        public User? CreatedByUser { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
