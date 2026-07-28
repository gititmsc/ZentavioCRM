using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Delegations;

namespace ZentavioCRM.Services.Interfaces
{
    /// <summary>Self-service out-of-office delegation setup — a user hands off their assigned records and due reminders to a delegate for a date range.</summary>
    public interface IUserDelegationService
    {
        /// <summary>Every delegation the current user has set up (as delegator).</summary>
        Task<IReadOnlyList<UserDelegationDto>> GetForCurrentUserAsync(Guid currentUserId);

        Task<ApiResponse<UserDelegationDto>> CreateAsync(Guid currentUserId, SaveUserDelegationRequest request);

        /// <summary>Only the delegator who created it can cancel a delegation.</summary>
        Task<ApiResponse<bool>> DeleteAsync(Guid id, Guid currentUserId);
    }
}
