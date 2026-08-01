using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface ITerritoryRepository
    {
        Task<Territory?> GetByIdAsync(Guid id);

        Task<IReadOnlyList<Territory>> GetAllAsync();

        /// <summary>Paged, filterable, sortable territory search — powers the Territories administration list.</summary>
        /// <param name="sortBy">Column key (case-insensitive): name, parentTerritoryName, userCount, leadCount, isActive, createdAtUtc. Unrecognized/null falls back to name.</param>
        Task<(IReadOnlyList<Territory> Items, int TotalCount)> SearchAsync(
            string? search, int page, int pageSize, string? sortBy = null, bool sortDescending = false);

        Task<bool> NameExistsAsync(string name, Guid? excludeId = null);

        Task<int> CountUsersAsync(Guid territoryId);

        Task<int> CountLeadsAsync(Guid territoryId);

        Task AddAsync(Territory territory);

        Task UpdateAsync(Territory territory);

        Task DeleteAsync(Territory territory);
    }
}
