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

        /// <summary>Set once a due-date reminder notification has been sent for this activity, so it isn't re-sent on every poll.</summary>
        public DateTime? ReminderSentAtUtc { get; set; }

        /// <summary>Null for a one-off activity. When set, this row is one occurrence of a recurring series — see <see cref="RecurrenceGroupId"/>.</summary>
        public ActivityRecurrenceRule? RecurrenceRule { get; set; }

        /// <summary>Shared by every occurrence generated from the same "repeat" request. All occurrences are created up front at creation time (no background scheduler in this app), so each is a normal Activity row that the existing due-date reminder check already covers.</summary>
        public Guid? RecurrenceGroupId { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public User? AssignedToUser { get; set; }

        public Guid? CreatedByUserId { get; set; }

        public User? CreatedByUser { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
