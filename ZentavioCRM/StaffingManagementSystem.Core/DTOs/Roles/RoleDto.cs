using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.DTOs.Roles
{
    public class RoleDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool IsSystemRole { get; set; }

        public VisibilityScope VisibilityScope { get; set; } = VisibilityScope.All;

        public List<string> PermissionCodes { get; set; } = [];
    }
}
