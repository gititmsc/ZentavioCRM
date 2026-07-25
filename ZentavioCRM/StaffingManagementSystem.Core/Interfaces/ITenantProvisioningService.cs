using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Platform;

namespace ZentavioCRM.Core.Interfaces
{
    /// <summary>
    /// Creates a brand-new tenant end to end: a dedicated database, its schema, the default
    /// RBAC seed (roles/permissions), the tenant's Company profile, its first Admin user, and
    /// the corresponding registry row in the Platform database. Implemented in the
    /// Infrastructure layer because it talks directly to SQL Server outside of any single
    /// tenant's DbContext (it's what CREATES that DbContext's target database).
    /// </summary>
    public interface ITenantProvisioningService
    {
        Task<ApiResponse<TenantDto>> ProvisionAsync(ProvisionTenantRequest request);

        Task<IReadOnlyList<TenantDto>> GetAllAsync();
    }
}
