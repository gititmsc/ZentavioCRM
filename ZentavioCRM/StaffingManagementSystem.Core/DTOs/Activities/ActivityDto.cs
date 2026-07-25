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
    }
}
