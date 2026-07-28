using System.ComponentModel.DataAnnotations;
using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.DTOs.Roles
{
    public class SaveRoleRequest
    {
        [Required(ErrorMessage = "Role name is required.")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public VisibilityScope VisibilityScope { get; set; } = VisibilityScope.All;

        public List<string> PermissionCodes { get; set; } = [];
    }
}
