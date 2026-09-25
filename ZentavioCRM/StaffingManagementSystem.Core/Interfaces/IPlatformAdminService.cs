using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Platform;

namespace ZentavioCRM.Core.Interfaces
{
    /// <summary>
    /// Authenticates Platform Admins and manages the platform admin roster. Implemented in the
    /// Infrastructure layer because — like <see cref="ITenantProvisioningService"/> — it talks
    /// directly to the Platform database rather than through a tenant's own repositories.
    /// </summary>
    public interface IPlatformAdminService
    {
        Task<ApiResponse<PlatformLoginResponseDto>> LoginAsync(PlatformLoginRequestDto request);

        Task<IReadOnlyList<PlatformAdminDto>> GetAllAsync();

        /// <summary>Creates a new platform admin account. Only callable by an already-authenticated platform admin (enforced at the controller).</summary>
        Task<ApiResponse<PlatformAdminDto>> CreateAsync(CreatePlatformAdminRequest request);
    }
}
