using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="ISavedAnalyticsRepository"/>
    public class SavedAnalyticsRepository : ISavedAnalyticsRepository
    {
        private readonly AppDbContext _dbContext;

        public SavedAnalyticsRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<SavedAnalyticsItem>> GetVisibleAsync(SavedAnalyticsKind kind, Guid userId)
            => await _dbContext.SavedAnalyticsItems
                .AsNoTracking()
                .Include(i => i.OwnerUser)
                .Where(i => i.Kind == kind && (i.OwnerUserId == userId || i.IsShared))
                .OrderBy(i => i.Name)
                .ToListAsync();

        public Task<SavedAnalyticsItem?> GetByIdAsync(Guid id)
            => _dbContext.SavedAnalyticsItems
                .Include(i => i.OwnerUser)
                .FirstOrDefaultAsync(i => i.Id == id);

        public async Task AddAsync(SavedAnalyticsItem item)
        {
            _dbContext.SavedAnalyticsItems.Add(item);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(SavedAnalyticsItem item)
        {
            _dbContext.SavedAnalyticsItems.Update(item);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(SavedAnalyticsItem item)
        {
            _dbContext.SavedAnalyticsItems.Remove(item);
            await _dbContext.SaveChangesAsync();
        }
    }
}
