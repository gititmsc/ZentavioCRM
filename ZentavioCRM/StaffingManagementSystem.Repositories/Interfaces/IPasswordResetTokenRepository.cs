using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface IPasswordResetTokenRepository
    {
        Task AddAsync(PasswordResetToken token);

        Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash);

        Task UpdateAsync(PasswordResetToken token);

        /// <summary>
        /// Atomically flips UsedAtUtc from null to now, in a single conditional UPDATE, so two
        /// concurrent requests presenting the same reset link can't both succeed. Returns false if
        /// the token was already used (lost the race, or genuinely already consumed) — the caller
        /// should treat that exactly like an already-used token.
        /// </summary>
        Task<bool> TryConsumeAsync(Guid tokenId);
    }
}
