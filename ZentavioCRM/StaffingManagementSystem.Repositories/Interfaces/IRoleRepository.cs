using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(Guid id);

        Task<IReadOnlyList<Role>> GetAllAsync();

        /// <summary>Paged, filterable, sortable role search — powers the Roles administration list.</summary>
        /// <param name="sortBy">Column key (case-insensitive): name, description, visibilityScope, permissionCount, isSystemRole, createdAtUtc. Unrecognized/null falls back to name.</param>
        Task<(IReadOnlyList<Role> Items, int TotalCount)> SearchAsync(
            string? search, int page, int pageSize, string? sortBy = null, bool sortDescending = false);

        Task<IReadOnlyList<Permission>> GetAllPermissionsAsync();

        Task<bool> NameExistsAsync(string name, Guid? excludeId = null);

        Task<int> CountUsersAsync(Guid roleId);

        Task AddAsync(Role role);

        Task UpdateAsync(Role role);

        Task DeleteAsync(Role role);

        /// <summary>Replaces the full set of permissions granted to a role with the given permission IDs.</summary>
        Task ReplacePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds);
    }
}
