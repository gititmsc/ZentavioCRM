using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface IUserDelegationRepository
    {
        Task<UserDelegation?> GetByIdAsync(Guid id);

        /// <summary>Every delegation the given user has set up (as delegator), most recent first.</summary>
        Task<IReadOnlyList<UserDelegation>> GetForDelegatorAsync(Guid delegatorUserId);

        /// <summary>Delegations where the given user is the delegate AND nowUtc falls within [StartDateUtc, EndDateUtc] — i.e. currently in effect.</summary>
        Task<IReadOnlyList<UserDelegation>> GetActiveForDelegateAsync(Guid delegateUserId, DateTime nowUtc);

        Task AddAsync(UserDelegation delegation);

        Task DeleteAsync(UserDelegation delegation);

        Task<UserDelegation> GetForReverseAsync(Guid delegatorUserId, Guid delegateUserId);
    }
}
