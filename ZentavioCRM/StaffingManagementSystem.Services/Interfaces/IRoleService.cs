using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Roles;

namespace ZentavioCRM.Services.Interfaces
{
    public interface IRoleService
    {
        Task<IReadOnlyList<RoleDto>> GetAllAsync();

        /// <summary>Paged, filterable, sortable role search — powers the Roles administration list.</summary>
        /// <param name="sortBy">Column key (case-insensitive): name, description, visibilityScope, permissionCount, isSystemRole, createdAtUtc. Unrecognized/null falls back to name.</param>
        Task<PagedResult<RoleDto>> SearchAsync(string? search, int page, int pageSize, string? sortBy = null, bool sortDescending = false);

        Task<ApiResponse<RoleDto>> GetByIdAsync(Guid id);

        /// <summary>All permission codes in the system, grouped by module, for the role editor UI.</summary>
        Task<IReadOnlyDictionary<string, List<string>>> GetPermissionCatalogAsync();

        Task<ApiResponse<RoleDto>> CreateAsync(SaveRoleRequest request);

        Task<ApiResponse<RoleDto>> UpdateAsync(Guid id, SaveRoleRequest request);

        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
