namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// A single grantable capability (e.g. "Leads.Create"). Seeded from
    /// <see cref="Common.PermissionCodes"/> and assigned to roles via <see cref="RolePermission"/>.
    /// </summary>
    public class Permission
    {
        public Guid Id { get; set; }

        /// <summary>Stable code referenced by authorization policies, e.g. "Leads.Create".</summary>
        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Module { get; set; } = string.Empty;

        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }
}
