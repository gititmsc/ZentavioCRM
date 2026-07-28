using System.ComponentModel.DataAnnotations;
using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.DTOs.Activities
{
    public class ActivityDto
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

        public string? AssignedToUserName { get; set; }

        public string? CreatedByUserName { get; set; }

        public ActivityRecurrenceRule? RecurrenceRule { get; set; }

        public Guid? RecurrenceGroupId { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    public class CreateActivityRequest
    {
        [Required]
        public ActivityType Type { get; set; }

        [Required(ErrorMessage = "Subject is required.")]
        [MaxLength(200)]
        public string Subject { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime? DueAtUtc { get; set; }

        public Guid? AssignedToUserId { get; set; }

        /// <summary>When set, generates a recurring series (this occurrence plus <see cref="RecurrenceCount"/> - 1 more) instead of a single activity. Requires <see cref="DueAtUtc"/> to be set, since it's the anchor date the later occurrences are computed from.</summary>
        public ActivityRecurrenceRule? RecurrenceRule { get; set; }

        /// <summary>Total occurrences to generate (including this one) when <see cref="RecurrenceRule"/> is set. Ignored otherwise.</summary>
        [Range(2, 52, ErrorMessage = "Occurrences must be between 2 and 52.")]
        public int? RecurrenceCount { get; set; }
    }
}
