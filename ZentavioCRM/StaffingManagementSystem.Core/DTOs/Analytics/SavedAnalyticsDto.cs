using System.ComponentModel.DataAnnotations;
using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Core.DTOs.Analytics
{
    public class SavedAnalyticsItemDto
    {
        public Guid Id { get; set; }

        public SavedAnalyticsKind Kind { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public Guid OwnerUserId { get; set; }

        public string OwnerName { get; set; } = string.Empty;

        public bool IsShared { get; set; }

        /// <summary>True when the viewer owns it.</summary>
        public bool IsMine { get; set; }

        /// <summary>True when the viewer may edit/delete it (owner, or holder of Analytics.ManageShared for shared items).</summary>
        public bool CanEdit { get; set; }

        public string ConfigJson { get; set; } = "{}";

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }
    }

    public class SaveAnalyticsItemRequest
    {
        /// <summary>Only honored on create; an item's kind never changes.</summary>
        public SavedAnalyticsKind Kind { get; set; }

        [Required(ErrorMessage = "Name is required.")]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public bool IsShared { get; set; }

        [Required(ErrorMessage = "Configuration is required.")]
        public string ConfigJson { get; set; } = "{}";
    }
}
