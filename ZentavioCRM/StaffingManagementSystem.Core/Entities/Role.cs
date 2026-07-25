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

        public DateTime CreatedAtUtc { get; set; }

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        public ICollection<User> Users { get; set; } = new List<User>();
    }
}
