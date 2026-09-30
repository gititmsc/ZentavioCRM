using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Platform;

namespace ZentavioCRM.Core.Interfaces
{
    /// <summary>
    /// Computes a tenant's current usage (user count, core record count, database size, last
    /// activity) live against that tenant's own database, for the platform admin usage dashboard.
    /// Implemented in the Infrastructure layer because — like <see cref="ITenantProvisioningService"/>
    /// — it has to open a connection to a database outside of the current request's own
    /// AppDbContext (which is scoped to whichever single tenant TenantResolutionMiddleware
    /// resolved, never the tenant a platform admin is asking about).
    /// </summary>
    public interface ITenantUsageService
    {
        Task<ApiResponse<TenantUsageDto>> GetUsageAsync(Guid tenantId);

        /// <summary>Every Active tenant/metric pair at or above 80% of its plan limit (Users,
        /// Records or Storage), ordered by how close to/over the limit it is — feeds the "Tenants
        /// nearing limits" Dashboard widget. Only Active tenants are checked: a Suspended,
        /// Terminated, Provisioning or Failed tenant isn't accumulating new usage that a platform
        /// admin needs to act on right now.</summary>
        Task<ApiResponse<IReadOnlyList<TenantUsageAlertDto>>> GetTenantsNearLimitsAsync();
    }
}
