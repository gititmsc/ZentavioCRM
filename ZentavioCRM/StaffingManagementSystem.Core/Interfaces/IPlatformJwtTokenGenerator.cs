using ZentavioCRM.Core.Entities.Platform;

namespace ZentavioCRM.Core.Interfaces
{
    /// <summary>
    /// Issues JWT access tokens for authenticated Platform Admins. Implemented in the
    /// Infrastructure layer, signed with <see cref="Configuration.PlatformJwtSettings"/> — a
    /// separate key from tenant user tokens (<see cref="IJwtTokenGenerator"/>).
    /// </summary>
    public interface IPlatformJwtTokenGenerator
    {
        /// <summary>Claim carried by every platform token — the "PlatformAdmin" authorization policy (registered in Program.cs) requires it, so a validly-signed-but-wrong-audience token still can't pass.</summary>
        const string PlatformAdminClaimType = "platform_admin";

        /// <summary>Carries the admin's <see cref="Enums.PlatformAdminRole"/> as its string name — the "PlatformSuperAdmin" authorization policy requires this claim equal "SuperAdmin".</summary>
        const string RoleClaimType = "platform_role";

        (string Token, DateTime ExpiresAtUtc) GenerateToken(PlatformAdmin admin);
    }
}
