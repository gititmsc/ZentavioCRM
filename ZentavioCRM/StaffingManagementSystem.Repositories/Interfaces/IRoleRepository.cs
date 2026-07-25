using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(Guid id);

        Task<IReadOnlyList<Role>> GetAllAsync();

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
