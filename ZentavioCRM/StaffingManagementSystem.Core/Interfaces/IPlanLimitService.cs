using ZentavioCRM.Core.DTOs.Platform;

namespace ZentavioCRM.Core.Interfaces
{
    /// <summary>
    /// Tenant-side lookup of a single tenant's plan limits (MaxUsers etc.), for enforcement
    /// checks like UserService.CreateAsync's seat-limit check. Deliberately a narrow, read-only
    /// contract distinct from <see cref="ITenantProvisioningService"/> — that one is Platform-
    /// admin-only surface area (provision/suspend/plan changes); this one is safe for an
    /// ordinary tenant-side Service to depend on.
    /// </summary>
    public interface IPlanLimitService
    {
        /// <summary>Null if the tenant id doesn't resolve to a row (shouldn't normally happen —
        /// the caller already has a valid, request-resolved tenant id).</summary>
        Task<TenantLimitsDto?> GetLimitsAsync(Guid tenantId);
    }
}
