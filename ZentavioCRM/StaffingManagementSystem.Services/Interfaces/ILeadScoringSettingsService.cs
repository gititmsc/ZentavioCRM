using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Leads;

namespace ZentavioCRM.Services.Interfaces
{
    public interface ILeadScoringSettingsService
    {
        Task<LeadScoringSettingsDto> GetAsync();

        Task<ApiResponse<LeadScoringSettingsDto>> UpdateAsync(LeadScoringSettingsDto request, Guid? currentUserId);
    }
}
