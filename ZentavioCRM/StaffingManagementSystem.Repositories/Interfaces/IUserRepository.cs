using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Repositories.Interfaces
{
    /// <summary>
    /// Data access contract for <see cref="User"/> records.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>Loads a user with Role -&gt; RolePermissions -&gt; Permission eagerly included, for auth/JWT generation.</summary>
        Task<User?> GetByEmailAsync(string email);

        Task<User?> GetByIdAsync(Guid id);

        Task<IReadOnlyList<User>> GetAllAsync();

        Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null);

        Task<bool> EmployeeCodeExistsAsync(string employeeCode, Guid? excludeUserId = null);

        Task AddAsync(User user);

        Task UpdateAsync(User user);

        Task UpdateLastLoginAsync(Guid userId, DateTime loginAtUtc);
    }
}
