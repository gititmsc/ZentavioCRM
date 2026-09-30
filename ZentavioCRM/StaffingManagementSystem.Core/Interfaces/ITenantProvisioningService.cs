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
        Task<ApiResponse<TenantDto>> ProvisionAsync(ProvisionTenantRequest request, Guid? performedByAdminId);

        Task<IReadOnlyList<TenantDto>> GetAllAsync();

        Task<ApiResponse<TenantDto>> GetByIdAsync(Guid id);

        /// <summary>Active -&gt; Suspended. Lock-out only — TenantResolutionMiddleware rejects any
        /// non-Active tenant's traffic; nothing about the tenant's database is touched.</summary>
        Task<ApiResponse<TenantDto>> SuspendAsync(Guid id, string? reason, Guid? performedByAdminId);

        /// <summary>Suspended or Terminated -&gt; Active. Both of this phase's lock-out states are
        /// reversible the same way.</summary>
        Task<ApiResponse<TenantDto>> ReactivateAsync(Guid id, Guid? performedByAdminId);

        /// <summary>Active or Suspended -&gt; Terminated. Same reversible lock-out as Suspend, just
        /// a distinct state/reason for "we're stopping this tenant" vs. "temporarily on hold" —
        /// still reversible via ReactivateAsync, per the deliberately-non-destructive design for
        /// tenant lifecycle actions (no database is ever deleted from this phase).</summary>
        Task<ApiResponse<TenantDto>> StopAsync(Guid id, string? reason, Guid? performedByAdminId);

        /// <summary>Changes a tenant's plan tier and/or overrides its usage limits. Omitted limit
        /// fields in the request fall back to that tier's <see cref="Configuration.PlanTierDefaults"/>.</summary>
        Task<ApiResponse<TenantDto>> UpdatePlanAsync(Guid id, UpdateTenantPlanRequest request, Guid? performedByAdminId);

        /// <summary>Edits the tenant registry's company name and denormalized admin-email display
        /// field. Does not touch the tenant's own database or its real admin user's sign-in
        /// email — see <see cref="DTOs.Platform.UpdateTenantMetadataRequest"/>.</summary>
        Task<ApiResponse<TenantDto>> UpdateMetadataAsync(Guid id, UpdateTenantMetadataRequest request, Guid? performedByAdminId);
    }
}
