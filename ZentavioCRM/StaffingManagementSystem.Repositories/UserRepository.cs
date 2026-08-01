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
            .Include(u => u.ReportingManager)
            .Include(u => u.Territory);

        public Task<User?> GetByEmailAsync(string email)
            => WithAuthData().FirstOrDefaultAsync(u => u.Email == email);

        public Task<User?> GetByIdAsync(Guid id)
            => WithAuthData().FirstOrDefaultAsync(u => u.Id == id);

        public async Task<IReadOnlyList<User>> GetAllAsync()
            => await WithAuthData().OrderBy(u => u.FirstName).ThenBy(u => u.LastName).ToListAsync();

        public async Task<(IReadOnlyList<User> Items, int TotalCount)> SearchAsync(
            string? search, Guid? roleId, Guid? departmentId, bool? isActive, int page, int pageSize,
            string? sortBy = null, bool sortDescending = true)
        {
            var query = WithAuthData();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(term) ||
                    u.LastName.ToLower().Contains(term) ||
                    u.Email.ToLower().Contains(term) ||
                    u.EmployeeCode.ToLower().Contains(term));
            }

            if (roleId is not null)
            {
                query = query.Where(u => u.RoleId == roleId);
            }

            if (departmentId is not null)
            {
                query = query.Where(u => u.DepartmentId == departmentId);
            }

            if (isActive is not null)
            {
                query = query.Where(u => u.IsActive == isActive);
            }

            var totalCount = await query.CountAsync();

            var items = await ApplySort(query, sortBy, sortDescending)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        /// <summary>
        /// Column-key-driven sort, kept as an explicit switch (not reflection/dynamic-LINQ) so every
        /// sortable column is a real, EF-translatable expression. Unrecognized/null sortBy falls back to CreatedAtUtc.
        /// </summary>
        private static IOrderedQueryable<User> ApplySort(IQueryable<User> query, string? sortBy, bool sortDescending)
        {
            return sortBy?.Trim().ToLowerInvariant() switch
            {
                "employeecode" => sortDescending ? query.OrderByDescending(u => u.EmployeeCode) : query.OrderBy(u => u.EmployeeCode),
                "fullname" => sortDescending
                    ? query.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName)
                    : query.OrderBy(u => u.FirstName).ThenBy(u => u.LastName),
                "email" => sortDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),
                "rolename" => sortDescending ? query.OrderByDescending(u => u.Role!.Name) : query.OrderBy(u => u.Role!.Name),
                "departmentname" => sortDescending ? query.OrderByDescending(u => u.Department!.Name) : query.OrderBy(u => u.Department!.Name),
                "isactive" => sortDescending ? query.OrderByDescending(u => u.IsActive) : query.OrderBy(u => u.IsActive),
                _ => sortDescending ? query.OrderByDescending(u => u.CreatedAtUtc) : query.OrderBy(u => u.CreatedAtUtc),
            };
        }

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

        public async Task<IReadOnlySet<Guid>> GetUserIdsInDepartmentAsync(Guid departmentId)
        {
            var ids = await _dbContext.Users
                .Where(u => u.DepartmentId == departmentId)
                .Select(u => u.Id)
                .ToListAsync();

            return ids.ToHashSet();
        }
    }
}
