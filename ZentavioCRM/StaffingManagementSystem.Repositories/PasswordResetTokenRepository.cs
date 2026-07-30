using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="IPasswordResetTokenRepository"/>
    public class PasswordResetTokenRepository : IPasswordResetTokenRepository
    {
        private readonly AppDbContext _dbContext;

        public PasswordResetTokenRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddAsync(PasswordResetToken token)
        {
            _dbContext.PasswordResetTokens.Add(token);
            await _dbContext.SaveChangesAsync();
        }

        public Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash)
            => _dbContext.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash);

        public async Task UpdateAsync(PasswordResetToken token)
        {
            _dbContext.PasswordResetTokens.Update(token);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> TryConsumeAsync(Guid tokenId)
        {
            // A single conditional UPDATE (not load-then-save) so two concurrent reset-password
            // requests presenting the same link can't both pass — only one UPDATE can match the
            // UsedAtUtc == null predicate and actually flip a row.
            var affected = await _dbContext.PasswordResetTokens
                .Where(t => t.Id == tokenId && t.UsedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(t => t.UsedAtUtc, DateTime.UtcNow));

            return affected > 0;
        }
    }
}
