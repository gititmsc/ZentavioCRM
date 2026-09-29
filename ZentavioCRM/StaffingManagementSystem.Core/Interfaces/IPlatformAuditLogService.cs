using ZentavioCRM.Core.DTOs.Platform;

namespace ZentavioCRM.Core.Interfaces
{
    /// <summary>
    /// Records and lists platform-level audit history (admin login, tenant provisioned/suspended/
    /// reactivated/stopped, impersonated). Lives in the Infrastructure layer with direct
    /// PlatformDbContext access — same reasoning as <see cref="ITenantProvisioningService"/> and
    /// <see cref="IPlatformAdminService"/>: this is Platform-database-only data, not tenant data,
    /// so it bypasses the normal per-tenant Repository layer entirely.
    /// </summary>
    public interface IPlatformAuditLogService
    {
        /// <summary>Fire-and-forget from the caller's point of view — never throws; a logging
        /// failure should not fail the action being logged.</summary>
        Task LogAsync(Guid? platformAdminId, string action, string summary, Guid? tenantId = null);

        Task<IReadOnlyList<PlatformAuditLogDto>> GetAllAsync(Guid? tenantId = null);
    }
}
