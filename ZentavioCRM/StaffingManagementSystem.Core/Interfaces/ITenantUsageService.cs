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
    }
}
