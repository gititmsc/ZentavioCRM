using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Territories;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="ITerritoryService"/>
    public class TerritoryService : ITerritoryService
    {
        private readonly ITerritoryRepository _territoryRepository;

        public TerritoryService(ITerritoryRepository territoryRepository)
        {
            _territoryRepository = territoryRepository;
        }

        public async Task<IReadOnlyList<TerritoryDto>> GetAllAsync()
        {
            var territories = await _territoryRepository.GetAllAsync();

            var result = new List<TerritoryDto>();
            foreach (var territory in territories)
            {
                result.Add(await MapAsync(territory));
            }

            return result;
        }

        public async Task<PagedResult<TerritoryDto>> SearchAsync(string? search, int page, int pageSize, string? sortBy = null, bool sortDescending = false)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

            var (items, totalCount) = await _territoryRepository.SearchAsync(search, page, pageSize, sortBy, sortDescending);

            var mapped = new List<TerritoryDto>();
            foreach (var territory in items)
            {
                mapped.Add(await MapAsync(territory));
            }

            return new PagedResult<TerritoryDto>
            {
                Items = mapped,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<ApiResponse<TerritoryDto>> GetByIdAsync(Guid id)
        {
            var territory = await _territoryRepository.GetByIdAsync(id);
            if (territory is null)
            {
                return ApiResponse<TerritoryDto>.FailureResponse("Territory not found.");
            }

            return ApiResponse<TerritoryDto>.SuccessResponse(await MapAsync(territory));
        }

        public async Task<ApiResponse<TerritoryDto>> CreateAsync(SaveTerritoryRequest request)
        {
            if (await _territoryRepository.NameExistsAsync(request.Name))
            {
                return ApiResponse<TerritoryDto>.FailureResponse(
                    "A territory with this name already exists.",
                    ["A territory with this name already exists."]);
            }

            var territory = new Territory
            {
                Name = request.Name.Trim(),
                ParentTerritoryId = request.ParentTerritoryId,
                IsActive = request.IsActive,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _territoryRepository.AddAsync(territory);

            return ApiResponse<TerritoryDto>.SuccessResponse(await MapAsync(territory), "Territory created.");
        }

        public async Task<ApiResponse<TerritoryDto>> UpdateAsync(Guid id, SaveTerritoryRequest request)
        {
            var territory = await _territoryRepository.GetByIdAsync(id);
            if (territory is null)
            {
                return ApiResponse<TerritoryDto>.FailureResponse("Territory not found.");
            }

            if (request.ParentTerritoryId == id)
            {
                return ApiResponse<TerritoryDto>.FailureResponse(
                    "A territory cannot be its own parent.",
                    ["A territory cannot be its own parent."]);
            }

            if (await _territoryRepository.NameExistsAsync(request.Name, id))
            {
                return ApiResponse<TerritoryDto>.FailureResponse(
                    "A territory with this name already exists.",
                    ["A territory with this name already exists."]);
            }

            territory.Name = request.Name.Trim();
            territory.ParentTerritoryId = request.ParentTerritoryId;
            territory.IsActive = request.IsActive;
            territory.UpdatedAtUtc = DateTime.UtcNow;

            await _territoryRepository.UpdateAsync(territory);

            return ApiResponse<TerritoryDto>.SuccessResponse(await MapAsync(territory), "Territory updated.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            var territory = await _territoryRepository.GetByIdAsync(id);
            if (territory is null)
            {
                return ApiResponse<bool>.FailureResponse("Territory not found.");
            }

            var userCount = await _territoryRepository.CountUsersAsync(id);
            if (userCount > 0)
            {
                return ApiResponse<bool>.FailureResponse(
                    $"Cannot delete — {userCount} user(s) are still assigned to this territory.",
                    ["Reassign the users in this territory first."]);
            }

            var leadCount = await _territoryRepository.CountLeadsAsync(id);
            if (leadCount > 0)
            {
                return ApiResponse<bool>.FailureResponse(
                    $"Cannot delete — {leadCount} lead(s) are still assigned to this territory.",
                    ["Reassign the leads in this territory first."]);
            }

            await _territoryRepository.DeleteAsync(territory);

            return ApiResponse<bool>.SuccessResponse(true, "Territory deleted.");
        }

        private async Task<TerritoryDto> MapAsync(Territory territory) => new()
        {
            Id = territory.Id,
            Name = territory.Name,
            ParentTerritoryId = territory.ParentTerritoryId,
            ParentTerritoryName = territory.ParentTerritory?.Name,
            IsActive = territory.IsActive,
            UserCount = await _territoryRepository.CountUsersAsync(territory.Id),
            LeadCount = await _territoryRepository.CountLeadsAsync(territory.Id),
        };
    }
}
