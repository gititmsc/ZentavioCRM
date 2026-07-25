using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="IRoleRepository"/>
    public class RoleRepository : IRoleRepository
    {
        private readonly AppDbContext _dbContext;

        public RoleRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private IQueryable<Role> WithPermissions() => _dbContext.Roles
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission);

        public Task<Role?> GetByIdAsync(Guid id)
            => WithPermissions().FirstOrDefaultAsync(r => r.Id == id);

        public async Task<IReadOnlyList<Role>> GetAllAsync()
            => await WithPermissions().OrderBy(r => r.Name).ToListAsync();

        public async Task<IReadOnlyList<Permission>> GetAllPermissionsAsync()
            => await _dbContext.Permissions.OrderBy(p => p.Module).ThenBy(p => p.Name).ToListAsync();

        public Task<bool> NameExistsAsync(string name, Guid? excludeId = null)
            => _dbContext.Roles.AnyAsync(r => r.Name.ToLower() == name.ToLower() && (excludeId == null || r.Id != excludeId));

        public Task<int> CountUsersAsync(Guid roleId)
            => _dbContext.Users.CountAsync(u => u.RoleId == roleId);

        public async Task AddAsync(Role role)
        {
            _dbContext.Roles.Add(role);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Role role)
        {
            _dbContext.Roles.Update(role);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Role role)
        {
            _dbContext.Roles.Remove(role);
            await _dbContext.SaveChangesAsync();
        }

        public async Task ReplacePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
        {
            var existing = await _dbContext.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
            _dbContext.RolePermissions.RemoveRange(existing);

            // Persist the removals before adding the new set — RolePermission's key is the plain
            // (RoleId, PermissionId) composite with no store-generated value, so if a permission is
            // being re-granted (the common case when only some permissions change), inserting a new
            // instance with the same key while the old one is still tracked as Deleted throws
            // "cannot be tracked because another instance with the same key value is already being tracked."
            await _dbContext.SaveChangesAsync();

            var grants = permissionIds.Distinct().Select(permissionId => new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
            });

            _dbContext.RolePermissions.AddRange(grants);
            await _dbContext.SaveChangesAsync();
        }
    }
}
