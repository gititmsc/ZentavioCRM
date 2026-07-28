using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Territories;

namespace ZentavioCRM.Services.Interfaces
{
    public interface ITerritoryService
    {
        Task<IReadOnlyList<TerritoryDto>> GetAllAsync();

        Task<ApiResponse<TerritoryDto>> GetByIdAsync(Guid id);

        Task<ApiResponse<TerritoryDto>> CreateAsync(SaveTerritoryRequest request);

        Task<ApiResponse<TerritoryDto>> UpdateAsync(Guid id, SaveTerritoryRequest request);

        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
