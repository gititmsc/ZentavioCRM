using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface IRefreshTokenRepository
    {
        Task AddAsync(RefreshToken token);

        Task<RefreshToken?> GetByTokenHashAsync(string tokenHash);

        Task UpdateAsync(RefreshToken token);

        /// <summary>Revokes every still-active refresh token for a user (logout-all-devices, password change/reset).</summary>
        Task RevokeAllForUserAsync(Guid userId);

        /// <summary>
        /// Atomically flips RevokedAtUtc from null to now, in a single conditional UPDATE (via
        /// ExecuteUpdateAsync — not a load-then-save round trip), so two concurrent refresh
        /// requests presenting the same token can never both "win" the rotation. Returns false if
        /// the token was already revoked (lost the race, or was revoked/rotated by an earlier call)
        /// — the caller should treat that exactly like an inactive token, not mint a new session.
        /// </summary>
        Task<bool> TryClaimForRotationAsync(Guid tokenId);

        /// <summary>Best-effort — records which token replaced this one after rotation succeeded. Never affects whether rotation itself was allowed.</summary>
        Task SetReplacedByTokenHashAsync(Guid tokenId, string replacedByTokenHash);
    }
}
