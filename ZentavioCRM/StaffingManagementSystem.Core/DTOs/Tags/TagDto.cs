using System.ComponentModel.DataAnnotations;

namespace ZentavioCRM.Core.DTOs.Tags
{
    public class TagDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Color { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }

    /// <summary><see cref="TagDto"/> plus how many Leads/Customers currently carry it — the Tag Manager list shape.</summary>
    public class TagWithUsageDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Color { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public int LeadCount { get; set; }

        public int CustomerCount { get; set; }
    }

    public class SaveTagRequest
    {
        [Required(ErrorMessage = "Tag name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Color { get; set; }
    }
}
