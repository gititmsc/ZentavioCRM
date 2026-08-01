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

        public async Task<(IReadOnlyList<Role> Items, int TotalCount)> SearchAsync(
            string? search, int page, int pageSize, string? sortBy = null, bool sortDescending = false)
        {
            var query = WithPermissions();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(r =>
                    r.Name.ToLower().Contains(term) ||
                    (r.Description != null && r.Description.ToLower().Contains(term)));
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
        /// sortable column is a real, EF-translatable expression. Unrecognized/null sortBy falls back to Name (ascending), matching the prior unpaged GetAllAsync ordering.
        /// </summary>
        private static IOrderedQueryable<Role> ApplySort(IQueryable<Role> query, string? sortBy, bool sortDescending)
        {
            return sortBy?.Trim().ToLowerInvariant() switch
            {
                "name" => sortDescending ? query.OrderByDescending(r => r.Name) : query.OrderBy(r => r.Name),
                "description" => sortDescending ? query.OrderByDescending(r => r.Description) : query.OrderBy(r => r.Description),
                "visibilityscope" => sortDescending ? query.OrderByDescending(r => r.VisibilityScope) : query.OrderBy(r => r.VisibilityScope),
                "permissioncount" => sortDescending ? query.OrderByDescending(r => r.RolePermissions.Count) : query.OrderBy(r => r.RolePermissions.Count),
                "issystemrole" => sortDescending ? query.OrderByDescending(r => r.IsSystemRole) : query.OrderBy(r => r.IsSystemRole),
                "createdatutc" => sortDescending ? query.OrderByDescending(r => r.CreatedAtUtc) : query.OrderBy(r => r.CreatedAtUtc),
                _ => sortDescending ? query.OrderByDescending(r => r.Name) : query.OrderBy(r => r.Name),
            };
        }

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
