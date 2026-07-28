using System.ComponentModel.DataAnnotations;

namespace ZentavioCRM.Core.DTOs.Territories
{
    /// <summary>Shared shape for creating and updating a Territory.</summary>
    public class SaveTerritoryRequest
    {
        [Required(ErrorMessage = "Territory name is required.")]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public Guid? ParentTerritoryId { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
