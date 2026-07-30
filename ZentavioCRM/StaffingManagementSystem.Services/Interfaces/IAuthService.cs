using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Auth;

namespace ZentavioCRM.Services.Interfaces
{
    /// <summary>
    /// Business logic contract for authentication.
    /// </summary>
    public interface IAuthService
    {
        Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request);

        /// <summary>Exchanges a still-valid, unused refresh token for a brand new access + refresh token pair (rotation).</summary>
        Task<ApiResponse<LoginResponseDto>> RefreshAsync(string refreshToken);

        /// <summary>Revokes a single refresh token (the one the caller is currently holding) — used when the user explicitly logs out.</summary>
        Task<ApiResponse<bool>> LogoutAsync(string refreshToken);

        /// <summary>
        /// Requests a password-reset email. Always returns the same generic success message
        /// regardless of whether the email matched a real account, so this endpoint can never be
        /// used to enumerate which addresses have accounts.
        /// </summary>
        Task<ApiResponse<bool>> ForgotPasswordAsync(string email);

        /// <summary>Consumes a password-reset token (from the emailed link) to set a new password.</summary>
        Task<ApiResponse<bool>> ResetPasswordAsync(string token, string newPassword);

        /// <summary>
        /// Mints a brand new access + refresh token pair for an already-authenticated user, without
        /// re-checking credentials. Used by self-service password change so the acting user's own
        /// session continues seamlessly after their old refresh tokens are revoked.
        /// </summary>
        Task<ApiResponse<LoginResponseDto>> IssueSessionAsync(Guid userId);
    }
}
