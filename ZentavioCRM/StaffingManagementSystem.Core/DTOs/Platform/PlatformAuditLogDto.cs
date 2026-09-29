namespace ZentavioCRM.Core.DTOs.Platform
{
    public class PlatformAuditLogDto
    {
        public Guid Id { get; set; }

        public Guid? PlatformAdminId { get; set; }

        /// <summary>Null if the admin account was later deleted, or for system-initiated entries.</summary>
        public string? PlatformAdminEmail { get; set; }

        public string Action { get; set; } = string.Empty;

        public Guid? TenantId { get; set; }

        /// <summary>Null for account-level actions (e.g. Login) that aren't tied to a tenant, or if
        /// the tenant was later deleted.</summary>
        public string? TenantName { get; set; }

        public string Summary { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
    }
}
