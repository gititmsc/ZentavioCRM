using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="IUserDelegationRepository"/>
    public class UserDelegationRepository : IUserDelegationRepository
    {
        private readonly AppDbContext _dbContext;

        public UserDelegationRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private IQueryable<UserDelegation> WithUsers() => _dbContext.UserDelegations
            .Include(d => d.DelegatorUser)
            .Include(d => d.DelegateUser);

        public Task<UserDelegation?> GetByIdAsync(Guid id)
            => WithUsers().FirstOrDefaultAsync(d => d.Id == id);

        public async Task<IReadOnlyList<UserDelegation>> GetForDelegatorAsync(Guid delegatorUserId)
            => await WithUsers()
                .Where(d => d.DelegatorUserId == delegatorUserId)
                .OrderByDescending(d => d.StartDateUtc)
                .ToListAsync();

        public async Task<UserDelegation> GetForReverseAsync(Guid delegatorUserId, Guid delegateUserId)
            => await WithUsers()
                .FirstOrDefaultAsync(d => d.DelegateUserId == delegatorUserId && d.DelegatorUserId == delegateUserId);

        public async Task<IReadOnlyList<UserDelegation>> GetActiveForDelegateAsync(Guid delegateUserId, DateTime nowUtc)
            => await _dbContext.UserDelegations
                .Where(d => d.DelegateUserId == delegateUserId && d.StartDateUtc <= nowUtc && d.EndDateUtc >= nowUtc)
                .ToListAsync();

        public async Task AddAsync(UserDelegation delegation)
        {
            _dbContext.UserDelegations.Add(delegation);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(UserDelegation delegation)
        {
            _dbContext.UserDelegations.Remove(delegation);
            await _dbContext.SaveChangesAsync();
        }
    }
}
