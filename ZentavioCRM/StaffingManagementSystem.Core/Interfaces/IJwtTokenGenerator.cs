using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Core.Interfaces
{
    /// <summary>
    /// Issues JWT access tokens for authenticated users. Implemented in the Infrastructure layer.
    /// </summary>
    public interface IJwtTokenGenerator
    {
        /// <summary>Marks a token as minted by a platform admin's impersonation action rather
        /// than the user's own login — see IImpersonationService. Present with value "true" only
        /// on impersonation tokens; absent on every normal login token.</summary>
        const string ImpersonationClaimType = "impersonation";

        /// <summary>The PlatformAdmin.Id who initiated the impersonation. Present only alongside
        /// <see cref="ImpersonationClaimType"/>.</summary>
        const string ImpersonatedByClaimType = "impersonated_by";

        /// <summary>
        /// <paramref name="extraClaims"/> is additive — every normal login claim (sub, email,
        /// role, one per permission) is still included. Used only by impersonation today; every
        /// other caller omits it and gets an identical token to before this parameter existed.
        /// </summary>
        (string Token, DateTime ExpiresAtUtc) GenerateToken(User user, IReadOnlyDictionary<string, string>? extraClaims = null);
    }
}
