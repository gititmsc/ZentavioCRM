using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Common;
using ZentavioCRM.Core.DTOs.Leads;
using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Services.Interfaces
{
    public interface ILeadService
    {
        /// <param name="sortBy">Column key (case-insensitive): leadNumber, companyName, contactName, source, expectedValue, assignedToUserName, status, createdAtUtc. Unrecognized/null falls back to createdAtUtc.</param>
        Task<PagedResult<LeadListItemDto>> SearchAsync(
            string? search, LeadStatus? status, Guid? assignedToUserId, int page, int pageSize, Guid? currentUserId = null,
            string? sortBy = null, bool sortDescending = true);

        Task<ApiResponse<LeadDto>> GetByIdAsync(Guid id, Guid? currentUserId = null);

        Task<ApiResponse<LeadDto>> CreateAsync(SaveLeadRequest request, Guid? currentUserId);

        Task<ApiResponse<LeadDto>> UpdateAsync(Guid id, SaveLeadRequest request, Guid? currentUserId);

        Task<ApiResponse<LeadDto>> UpdateStatusAsync(Guid id, UpdateLeadStatusRequest request, Guid? currentUserId);

        Task<ApiResponse<LeadDto>> AssignAsync(Guid id, AssignLeadRequest request, Guid? currentUserId);

        Task<ApiResponse<bool>> DeleteAsync(Guid id, Guid? currentUserId);

        Task<ApiResponse<ConvertLeadResultDto>> ConvertAsync(Guid id, ConvertLeadRequest request, Guid? currentUserId);

        /// <summary>Non-blocking pre-flight check for possible duplicate Leads/Customers by email or mobile — the frontend surfaces matches as a dismissible warning, it never blocks the save.</summary>
        Task<DuplicateCheckResultDto> CheckDuplicatesAsync(string? email, string? mobile, Guid? excludeLeadId);

        Task<string> ExportCsvAsync();

        Task<ImportResultDto> ImportCsvAsync(string csvContent, Guid? currentUserId);

        /// <summary>
        /// Converts a lead straight into an Opportunity: creates the Customer (or reuses one from
        /// a prior plain conversion) and a new Opportunity linked back to this lead via SourceLeadId.
        /// </summary>
        Task<ApiResponse<ConvertLeadToOpportunityResultDto>> ConvertToOpportunityAsync(
            Guid id, ConvertLeadToOpportunityRequest request, Guid? currentUserId);
    }
}
