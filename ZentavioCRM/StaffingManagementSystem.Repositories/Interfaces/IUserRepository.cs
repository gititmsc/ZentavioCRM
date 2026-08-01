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

        /// <summary>Paged, filterable, sortable user search — powers the Users administration list (distinct from the unpaged GetAllAsync, which many dropdowns rely on).</summary>
        /// <param name="sortBy">Column key (case-insensitive): employeeCode, fullName, email, roleName, departmentName, isActive, createdAtUtc. Unrecognized/null falls back to createdAtUtc.</param>
        Task<(IReadOnlyList<User> Items, int TotalCount)> SearchAsync(
            string? search, Guid? roleId, Guid? departmentId, bool? isActive, int page, int pageSize,
            string? sortBy = null, bool sortDescending = true);

        Task<bool> EmailExistsAsync(string email, Guid? excludeUserId = null);

        Task<bool> EmployeeCodeExistsAsync(string employeeCode, Guid? excludeUserId = null);

        Task AddAsync(User user);

        Task UpdateAsync(User user);

        Task UpdateLastLoginAsync(Guid userId, DateTime loginAtUtc);

        /// <summary>Every user ID sharing the given Department (including any user with that DepartmentId). Used to resolve "Team" visibility scope, where Team is defined as same-department.</summary>
        Task<IReadOnlySet<Guid>> GetUserIdsInDepartmentAsync(Guid departmentId);
    }
}
