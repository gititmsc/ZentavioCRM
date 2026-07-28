using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface ITerritoryRepository
    {
        Task<Territory?> GetByIdAsync(Guid id);

        Task<IReadOnlyList<Territory>> GetAllAsync();

        Task<bool> NameExistsAsync(string name, Guid? excludeId = null);

        Task<int> CountUsersAsync(Guid territoryId);

        Task<int> CountLeadsAsync(Guid territoryId);

        Task AddAsync(Territory territory);

        Task UpdateAsync(Territory territory);

        Task DeleteAsync(Territory territory);
    }
}
