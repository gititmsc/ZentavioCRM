using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Roles;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="IRoleService"/>
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;

        public RoleService(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<IReadOnlyList<RoleDto>> GetAllAsync()
        {
            var roles = await _roleRepository.GetAllAsync();
            return roles.Select(Map).ToList();
        }

        public async Task<ApiResponse<RoleDto>> GetByIdAsync(Guid id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            return role is null
                ? ApiResponse<RoleDto>.FailureResponse("Role not found.")
                : ApiResponse<RoleDto>.SuccessResponse(Map(role));
        }

        public async Task<IReadOnlyDictionary<string, List<string>>> GetPermissionCatalogAsync()
        {
            var permissions = await _roleRepository.GetAllPermissionsAsync();
            return permissions
                .GroupBy(p => p.Module)
                .ToDictionary(g => g.Key, g => g.Select(p => p.Code).ToList());
        }

        public async Task<ApiResponse<RoleDto>> CreateAsync(SaveRoleRequest request)
        {
            if (await _roleRepository.NameExistsAsync(request.Name))
            {
                return ApiResponse<RoleDto>.FailureResponse("A role with this name already exists.", ["Role name already in use."]);
            }

            var role = new Role
            {
                Name = request.Name.Trim(),
                Description = request.Description,
                IsSystemRole = false,
                VisibilityScope = request.VisibilityScope,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _roleRepository.AddAsync(role);

            var permissionIds = await ResolvePermissionIdsAsync(request.PermissionCodes);
            await _roleRepository.ReplacePermissionsAsync(role.Id, permissionIds);

            var created = await _roleRepository.GetByIdAsync(role.Id);
            return ApiResponse<RoleDto>.SuccessResponse(Map(created!), "Role created.");
        }

        public async Task<ApiResponse<RoleDto>> UpdateAsync(Guid id, SaveRoleRequest request)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role is null)
            {
                return ApiResponse<RoleDto>.FailureResponse("Role not found.");
            }

            if (role.IsSystemRole)
            {
                return ApiResponse<RoleDto>.FailureResponse(
                    "System roles cannot be modified.",
                    ["Create a new role instead of editing a built-in one."]);
            }

            if (await _roleRepository.NameExistsAsync(request.Name, id))
            {
                return ApiResponse<RoleDto>.FailureResponse("A role with this name already exists.", ["Role name already in use."]);
            }

            role.Name = request.Name.Trim();
            role.Description = request.Description;
            role.VisibilityScope = request.VisibilityScope;
            await _roleRepository.UpdateAsync(role);

            var permissionIds = await ResolvePermissionIdsAsync(request.PermissionCodes);
            await _roleRepository.ReplacePermissionsAsync(id, permissionIds);

            var updated = await _roleRepository.GetByIdAsync(id);
            return ApiResponse<RoleDto>.SuccessResponse(Map(updated!), "Role updated.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            var role = await _roleRepository.GetByIdAsync(id);
            if (role is null)
            {
                return ApiResponse<bool>.FailureResponse("Role not found.");
            }

            if (role.IsSystemRole)
            {
                return ApiResponse<bool>.FailureResponse("System roles cannot be deleted.");
            }

            var userCount = await _roleRepository.CountUsersAsync(id);
            if (userCount > 0)
            {
                return ApiResponse<bool>.FailureResponse(
                    $"Cannot delete — {userCount} user(s) still have this role.",
                    ["Reassign those users to a different role first."]);
            }

            await _roleRepository.DeleteAsync(role);
            return ApiResponse<bool>.SuccessResponse(true, "Role deleted.");
        }

        private async Task<List<Guid>> ResolvePermissionIdsAsync(List<string> codes)
        {
            var allPermissions = await _roleRepository.GetAllPermissionsAsync();
            return allPermissions.Where(p => codes.Contains(p.Code)).Select(p => p.Id).ToList();
        }

        private static RoleDto Map(Role role) => new()
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsSystemRole = role.IsSystemRole,
            VisibilityScope = role.VisibilityScope,
            PermissionCodes = role.RolePermissions
                .Where(rp => rp.Permission is not null)
                .Select(rp => rp.Permission!.Code)
                .ToList(),
        };
    }
}
