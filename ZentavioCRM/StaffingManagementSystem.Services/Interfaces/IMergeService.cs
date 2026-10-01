using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Common;

namespace ZentavioCRM.Services.Interfaces
{
    /// <summary>Validated, audit-logged wrapper around IMergeRepository's raw merge logic — see MergeLeadsRequest/MergeCustomersRequest for exactly what a merge repoints.</summary>
    public interface IMergeService
    {
        Task<ApiResponse<MergeResultDto>> MergeLeadsAsync(Guid survivingLeadId, Guid losingLeadId, Guid? currentUserId);

        Task<ApiResponse<MergeResultDto>> MergeCustomersAsync(Guid survivingCustomerId, Guid losingCustomerId, Guid? currentUserId);
    }
}
