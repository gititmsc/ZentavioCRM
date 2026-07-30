using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="IRefreshTokenRepository"/>
    public class RefreshTokenRepository : IRefreshTokenRepository
    {
        private readonly AppDbContext _dbContext;

        public RefreshTokenRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(RefreshToken token)
        {
            _dbContext.RefreshTokens.Add(token);
            await _dbContext.SaveChangesAsync();
        }

        public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash)
            => _dbContext.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        public async Task UpdateAsync(RefreshToken token)
        {
            _dbContext.RefreshTokens.Update(token);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RevokeAllForUserAsync(Guid userId)
        {
            var now = DateTime.UtcNow;
            var activeTokens = await _dbContext.RefreshTokens
                .Where(t => t.UserId == userId && t.RevokedAtUtc == null && t.ExpiresAtUtc > now)
                .ToListAsync();

            foreach (var token in activeTokens)
            {
                token.RevokedAtUtc = now;
            }

            if (activeTokens.Count > 0)
            {
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<bool> TryClaimForRotationAsync(Guid tokenId)
        {
            // A single conditional UPDATE (not load-then-save) so two concurrent refresh requests
            // presenting the same token can't both pass — only one UPDATE can match the
            // RevokedAtUtc == null predicate and actually flip a row.
            var affected = await _dbContext.RefreshTokens
                .Where(t => t.Id == tokenId && t.RevokedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.RevokedAtUtc, DateTime.UtcNow));

            return affected > 0;
        }

        public async Task SetReplacedByTokenHashAsync(Guid tokenId, string replacedByTokenHash)
        {
            await _dbContext.RefreshTokens
                .Where(t => t.Id == tokenId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.ReplacedByTokenHash, replacedByTokenHash));
        }
    }
}
