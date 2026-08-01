using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Territories;

namespace ZentavioCRM.Services.Interfaces
{
    public interface ITerritoryService
    {
        Task<IReadOnlyList<TerritoryDto>> GetAllAsync();

        /// <summary>Paged, filterable, sortable territory search — powers the Territories administration list.</summary>
        /// <param name="sortBy">Column key (case-insensitive): name, parentTerritoryName, userCount, leadCount, isActive, createdAtUtc. Unrecognized/null falls back to name.</param>
        Task<PagedResult<TerritoryDto>> SearchAsync(string? search, int page, int pageSize, string? sortBy = null, bool sortDescending = false);

        Task<ApiResponse<TerritoryDto>> GetByIdAsync(Guid id);

        Task<ApiResponse<TerritoryDto>> CreateAsync(SaveTerritoryRequest request);

        Task<ApiResponse<TerritoryDto>> UpdateAsync(Guid id, SaveTerritoryRequest request);

        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
