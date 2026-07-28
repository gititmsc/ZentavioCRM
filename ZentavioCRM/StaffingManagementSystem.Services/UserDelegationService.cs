using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Delegations;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="IUserDelegationService"/>
    public class UserDelegationService : IUserDelegationService
    {
        private readonly IUserDelegationRepository _delegationRepository;
        private readonly IUserRepository _userRepository;

        public UserDelegationService(IUserDelegationRepository delegationRepository, IUserRepository userRepository)
        {
            _delegationRepository = delegationRepository;
            _userRepository = userRepository;
        }

        public async Task<IReadOnlyList<UserDelegationDto>> GetForCurrentUserAsync(Guid currentUserId)
        {
            var delegations = await _delegationRepository.GetForDelegatorAsync(currentUserId);
            return delegations.Select(Map).ToList();
        }

        public async Task<ApiResponse<UserDelegationDto>> CreateAsync(Guid currentUserId, SaveUserDelegationRequest request)
        {
            if (request.DelegateUserId == currentUserId)
            {
                return ApiResponse<UserDelegationDto>.FailureResponse(
                    "You cannot delegate to yourself.",
                    ["Choose a different delegate."]);
            }

            if (request.EndDateUtc < request.StartDateUtc)
            {
                return ApiResponse<UserDelegationDto>.FailureResponse(
                    "The end date must be on or after the start date.",
                    ["End date must be on or after the start date."]);
            }

            var delegateUser = await _userRepository.GetByIdAsync(request.DelegateUserId);
            if (delegateUser is null)
            {
                return ApiResponse<UserDelegationDto>.FailureResponse("Selected delegate does not exist.");
            }

            var delegation = new UserDelegation
            {
                DelegatorUserId = currentUserId,
                DelegateUserId = request.DelegateUserId,
                StartDateUtc = request.StartDateUtc,
                EndDateUtc = request.EndDateUtc,
                Notes = request.Notes,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _delegationRepository.AddAsync(delegation);

            var created = await _delegationRepository.GetByIdAsync(delegation.Id);
            return ApiResponse<UserDelegationDto>.SuccessResponse(Map(created!), "Delegation created.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id, Guid currentUserId)
        {
            var delegation = await _delegationRepository.GetByIdAsync(id);
            if (delegation is null)
            {
                return ApiResponse<bool>.FailureResponse("Delegation not found.");
            }

            if (delegation.DelegatorUserId != currentUserId)
            {
                return ApiResponse<bool>.FailureResponse("Delegation not found.");
            }

            await _delegationRepository.DeleteAsync(delegation);
            return ApiResponse<bool>.SuccessResponse(true, "Delegation cancelled.");
        }

        private static UserDelegationDto Map(UserDelegation delegation)
        {
            var now = DateTime.UtcNow;
            return new UserDelegationDto
            {
                Id = delegation.Id,
                DelegatorUserId = delegation.DelegatorUserId,
                DelegatorUserName = delegation.DelegatorUser?.FullName ?? string.Empty,
                DelegateUserId = delegation.DelegateUserId,
                DelegateUserName = delegation.DelegateUser?.FullName ?? string.Empty,
                StartDateUtc = delegation.StartDateUtc,
                EndDateUtc = delegation.EndDateUtc,
                Notes = delegation.Notes,
                IsActive = delegation.StartDateUtc <= now && delegation.EndDateUtc >= now,
                CreatedAtUtc = delegation.CreatedAtUtc,
            };
        }
    }
}
