using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="IUserRepository"/>
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _dbContext;

        public UserRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private IQueryable<User> WithAuthData() => _dbContext.Users
            .Include(u => u.Role)
                .ThenInclude(r => r!.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .Include(u => u.Department)
            .Include(u => u.ReportingManager);

        public Task<User?> GetByEmailAsync(string email)
            => WithAuthData().FirstOrDefaultAsync(u => u.Email == email);

        public Task<User?> GetByIdAsync(Guid id)
            => WithAuthData().FirstOrDefaultAsync(u => u.Id == id);

        public async Task<IReadOnlyList<User>> GetAllAsync()
            => await WithAuthData().OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync();

        public Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null)
            => _dbContext.Users.AnyAsync(u => u.Email == email && (excludeUserId == null || u.Id != excludeUserId));

        public Task<bool> EmployeeCodeExistsAsync(string employeeCode, Guid? excludeUserId = null)
            => _dbContext.Users.AnyAsync(u => u.EmployeeCode == employeeCode && (excludeUserId == null || u.Id != excludeUserId));

        public async Task AddAsync(User user)
        {
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(User user)
        {
            _dbContext.Users.Update(user);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateLastLoginAsync(Guid userId, DateTime loginAtUtc)
        {
            await _dbContext.Users
                .Where(u => u.Id == userId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.LastLoginAtUtc, loginAtUtc));
        }
    }
}
