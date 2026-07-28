using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// A named collection of permissions that can be assigned to <see cref="User"/> records.
    /// Replaces the old fixed <c>UserRole</c> enum with configurable role-based access control.
    /// </summary>
    public class Role
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        /// <summary>System roles (Admin, etc.) cannot be deleted or renamed from the UI.</summary>
        public bool IsSystemRole { get; set; }

        /// <summary>How much of the Leads/Customers/Opportunities record-set a user with this role can see and act on. Defaults to All to preserve existing behavior for roles created before this feature existed.</summary>
        public VisibilityScope VisibilityScope { get; set; } = VisibilityScope.All;

        public DateTime CreatedAtUtc { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
