using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Leads;
using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Services.Interfaces
{
    public interface ILeadService
    {
        Task<PagedResult<LeadListItemDto>> SearchAsync(
            string? search, LeadStatus? status, Guid? assignedToUserId, int page, int pageSize);

        Task<ApiResponse<LeadDto>> GetByIdAsync(Guid id);

        Task<ApiResponse<LeadDto>> CreateAsync(SaveLeadRequest request, Guid? currentUserId);

        Task<ApiResponse<LeadDto>> UpdateAsync(Guid id, SaveLeadRequest request);

        Task<ApiResponse<LeadDto>> UpdateStatusAsync(Guid id, UpdateLeadStatusRequest request);

        Task<ApiResponse<LeadDto>> AssignAsync(Guid id, AssignLeadRequest request);

        Task<ApiResponse<bool>> DeleteAsync(Guid id);

        Task<ApiResponse<ConvertLeadResultDto>> ConvertAsync(Guid id, ConvertLeadRequest request);

        /// <summary>
        /// Converts a lead straight into an Opportunity: creates the Customer (or reuses one from
        /// a prior plain conversion) and a new Opportunity linked back to this lead via SourceLeadId.
        /// </summary>
        Task<ApiResponse<ConvertLeadToOpportunityResultDto>> ConvertToOpportunityAsync(
            Guid id, ConvertLeadToOpportunityRequest request, Guid? currentUserId);
    }
}
