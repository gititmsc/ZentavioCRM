using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="ITerritoryRepository"/>
    public class TerritoryRepository : ITerritoryRepository
    {
        private readonly AppDbContext _dbContext;

        public TerritoryRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Territory?> GetByIdAsync(Guid id)
            => _dbContext.Territories.Include(t => t.ParentTerritory).FirstOrDefaultAsync(t => t.Id == id);

        public async Task<IReadOnlyList<Territory>> GetAllAsync()
            => await _dbContext.Territories
                .Include(t => t.ParentTerritory)
                .OrderBy(t => t.Name)
                .ToListAsync();

        public Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
            => _dbContext.Territories.AnyAsync(t =>
                t.Name.ToLower() == name.ToLower() &&
                (excludeId == null || t.Id != excludeId));

        public Task<int> CountUsersAsync(Guid territoryId)
            => _dbContext.Users.CountAsync(u => u.TerritoryId == territoryId);

        public Task<int> CountLeadsAsync(Guid territoryId)
            => _dbContext.Leads.CountAsync(l => l.TerritoryId == territoryId);

        public async Task AddAsync(Territory territory)
        {
            _dbContext.Territories.Add(territory);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Territory territory)
        {
            _dbContext.Territories.Update(territory);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Territory territory)
        {
            _dbContext.Territories.Remove(territory);
            await _dbContext.SaveChangesAsync();
        }
    }
}
