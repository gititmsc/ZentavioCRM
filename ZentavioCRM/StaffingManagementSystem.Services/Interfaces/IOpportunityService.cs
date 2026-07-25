using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Opportunities;
using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Services.Interfaces
{
    public interface IOpportunityService
    {
        Task<PagedResult<OpportunityListItemDto>> SearchAsync(
            string? search, OpportunityStage? stage, Guid? customerId, Guid? assignedToUserId, int page, int pageSize);

        Task<ApiResponse<OpportunityDto>> GetByIdAsync(Guid id);

        Task<ApiResponse<OpportunityDto>> CreateAsync(SaveOpportunityRequest request, Guid? currentUserId);

        Task<ApiResponse<OpportunityDto>> UpdateAsync(Guid id, SaveOpportunityRequest request);

        Task<ApiResponse<OpportunityDto>> UpdateStageAsync(Guid id, UpdateOpportunityStageRequest request);

        Task<ApiResponse<OpportunityDto>> AssignAsync(Guid id, AssignOpportunityRequest request);

        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
