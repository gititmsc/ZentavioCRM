namespace ZentavioCRM.Core.Entities
{
    /// <summary>Join entity linking a <see cref="Entities.Role"/> to a granted <see cref="Entities.Permission"/>.</summary>
    public class RolePermission
    {
        public Guid RoleId { get; set; }

        public Role? Role { get; set; }

        public Guid PermissionId { get; set; }

        public Permission? Permission { get; set; }
    }
}
