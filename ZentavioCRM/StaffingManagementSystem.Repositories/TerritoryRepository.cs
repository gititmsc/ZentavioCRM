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

        public async Task<(IReadOnlyList<Territory> Items, int TotalCount)> SearchAsync(
            string? search, int page, int pageSize, string? sortBy = null, bool sortDescending = false)
        {
            var query = _dbContext.Territories.Include(t => t.ParentTerritory).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(t => t.Name.ToLower().Contains(term));
            }

            var totalCount = await query.CountAsync();

            var items = await ApplySort(query, sortBy, sortDescending)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <summary>
        /// Column-key-driven sort, kept as an explicit switch (not reflection/dynamic-LINQ) so every
        /// sortable column is a real, EF-translatable expression. UserCount/LeadCount are correlated-subquery
        /// counts (not mapped columns) since Territory has no denormalized count fields. Unrecognized/null
        /// sortBy falls back to Name (ascending), matching the prior unpaged GetAllAsync ordering.
        /// </summary>
        private IOrderedQueryable<Territory> ApplySort(IQueryable<Territory> query, string? sortBy, bool sortDescending)
        {
            return sortBy?.Trim().ToLowerInvariant() switch
            {
                "name" => sortDescending ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
                "parentterritoryname" => sortDescending ? query.OrderByDescending(t => t.ParentTerritory!.Name) : query.OrderBy(t => t.ParentTerritory!.Name),
                "usercount" => sortDescending
                    ? query.OrderByDescending(t => _dbContext.Users.Count(u => u.TerritoryId == t.Id))
                    : query.OrderBy(t => _dbContext.Users.Count(u => u.TerritoryId == t.Id)),
                "leadcount" => sortDescending
                    ? query.OrderByDescending(t => _dbContext.Leads.Count(l => l.TerritoryId == t.Id))
                    : query.OrderBy(t => _dbContext.Leads.Count(l => l.TerritoryId == t.Id)),
                "isactive" => sortDescending ? query.OrderByDescending(t => t.IsActive) : query.OrderBy(t => t.IsActive),
                "createdatutc" => sortDescending ? query.OrderByDescending(t => t.CreatedAtUtc) : query.OrderBy(t => t.CreatedAtUtc),
                _ => sortDescending ? query.OrderByDescending(t => t.Name) : query.OrderBy(t => t.Name),
            };
        }

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
