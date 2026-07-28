using ZentavioCRM.Core.Enums;
using ZentavioCRM.Core.Security;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="IAccessScopeService"/>
    public class AccessScopeService : IAccessScopeService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserDelegationRepository _userDelegationRepository;

        public AccessScopeService(IUserRepository userRepository, IUserDelegationRepository userDelegationRepository)
        {
            _userRepository = userRepository;
            _userDelegationRepository = userDelegationRepository;
        }

        public async Task<AccessScope> GetForUserAsync(Guid userId)
        {
            // Active delegations apply regardless of scope (an override layered on top of Own/Team/All,
            // per AccessScope's own doc comment) — resolved for every user, not just non-All scopes,
            // since a Own/Team-scoped delegate still needs to see the delegator's records.
            var activeDelegations = await _userDelegationRepository.GetActiveForDelegateAsync(userId, DateTime.UtcNow);
            var delegatedFromUserIds = activeDelegations.Select(d => d.DelegatorUserId).ToHashSet();

            var user = await _userRepository.GetByIdAsync(userId);
            if (user?.Role is null)
            {
                // Fail closed: an unresolvable user/role sees only their own records rather than everything.
                return new AccessScope { Scope = VisibilityScope.Own, UserId = userId, DelegatedFromUserIds = delegatedFromUserIds };
            }

            var scope = user.Role.VisibilityScope;

            IReadOnlySet<Guid> teamUserIds = scope == VisibilityScope.Team && user.DepartmentId is not null
                ? await _userRepository.GetUserIdsInDepartmentAsync(user.DepartmentId.Value)
                : new HashSet<Guid>();

            return new AccessScope
            {
                Scope = scope,
                UserId = userId,
                TeamUserIds = teamUserIds,
                DelegatedFromUserIds = delegatedFromUserIds,
            };
        }
    }
}
