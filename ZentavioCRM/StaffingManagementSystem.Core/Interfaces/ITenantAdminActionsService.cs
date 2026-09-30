using ZentavioCRM.Core.Common;

namespace ZentavioCRM.Core.Interfaces
{
    /// <summary>
    /// "Quick admin actions" a platform admin can take on a specific tenant's admin user without
    /// impersonating them — resending their welcome/login email, or forcing a password reset when
    /// they're locked out and can't use the tenant's own self-service "Forgot Password?" flow.
    /// Implemented in the Infrastructure layer because, like <see cref="IImpersonationService"/>,
    /// it has to open an ad-hoc connection to one specific tenant's own database (never the current
    /// request's own AppDbContext, which TenantResolutionMiddleware never resolves for /api/platform
    /// routes).
    /// </summary>
    public interface ITenantAdminActionsService
    {
        /// <summary>Sends the tenant's admin user a fresh "Welcome to ZentavioCRM" email containing
        /// a password-reset link, so they can set a password and sign in for the first time (or
        /// again, if the original welcome email never arrived). Reuses the same reset-token
        /// mechanism as a normal password reset.</summary>
        Task<ApiResponse<bool>> ResendWelcomeEmailAsync(Guid tenantId, Guid? performedByAdminId);

        /// <summary>Sends the tenant's admin user a password-reset link, the same as if they'd used
        /// "Forgot Password?" themselves — for when they're locked out and can't request one on
        /// their own. Does not change their password directly; they still have to click the link.</summary>
        Task<ApiResponse<bool>> ForcePasswordResetAsync(Guid tenantId, Guid? performedByAdminId);
    }
}
