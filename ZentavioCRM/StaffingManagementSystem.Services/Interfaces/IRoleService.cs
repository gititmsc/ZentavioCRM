using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Roles;

namespace ZentavioCRM.Services.Interfaces
{
    public interface IRoleService
    {
        Task<IReadOnlyList<RoleDto>> GetAllAsync();

        Task<ApiResponse<RoleDto>> GetByIdAsync(Guid id);

        /// <summary>All permission codes in the system, grouped by module, for the role editor UI.</summary>
        Task<IReadOnlyDictionary<string, List<string>>> GetPermissionCatalogAsync();

        Task<ApiResponse<RoleDto>> CreateAsync(SaveRoleRequest request);

        Task<ApiResponse<RoleDto>> UpdateAsync(Guid id, SaveRoleRequest request);

        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
