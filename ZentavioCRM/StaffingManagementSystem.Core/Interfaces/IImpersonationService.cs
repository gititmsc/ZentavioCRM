using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Platform;

namespace ZentavioCRM.Core.Interfaces
{
    /// <summary>
    /// Lets a platform admin obtain a short-lived tenant-user session token for that tenant's
    /// admin user, without knowing or resetting that user's password — for support/debugging.
    /// The single most security-sensitive capability in the Platform Admin surface: every call
    /// mints real access to a customer's data. See ImpersonationService for the specific
    /// safeguards (Active-tenant-only, no refresh token, dual audit trail).
    /// </summary>
    public interface IImpersonationService
    {
        Task<ApiResponse<ImpersonateTenantResponseDto>> ImpersonateAsync(Guid tenantId, string? reason, Guid? performedByAdminId);
    }
}
