using Microsoft.EntityFrameworkCore;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Infrastructure.Persistence;
using ZentavioCRM.Repositories.Interfaces;

namespace ZentavioCRM.Repositories
{
    /// <inheritdoc cref="IDepartmentRepository"/>
    public class DepartmentRepository : IDepartmentRepository
    {
        private readonly AppDbContext _dbContext;

        public DepartmentRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<Department?> GetByIdAsync(Guid id)
            => _dbContext.Departments.Include(d => d.ParentDepartment).FirstOrDefaultAsync(d => d.Id == id);

        public async Task<IReadOnlyList<Department>> GetAllAsync()
            => await _dbContext.Departments
                .Include(d => d.ParentDepartment)
                .OrderBy(d => d.Name)
                .ToListAsync();

        public async Task<(IReadOnlyList<Department> Items, int TotalCount)> SearchAsync(
            string? search, int page, int pageSize, string? sortBy = null, bool sortDescending = false)
        {
            var query = _dbContext.Departments.Include(d => d.ParentDepartment).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim().ToLower();
                query = query.Where(d => d.Name.ToLower().Contains(term));
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
        /// sortable column is a real, EF-translatable expression. UserCount is a correlated-subquery
        /// count (not a mapped column) since Department has no denormalized count field. Unrecognized/null
        /// sortBy falls back to Name (ascending), matching the prior unpaged GetAllAsync ordering.
        /// </summary>
        private IOrderedQueryable<Department> ApplySort(IQueryable<Department> query, string? sortBy, bool sortDescending)
        {
            return sortBy?.Trim().ToLowerInvariant() switch
            {
                "name" => sortDescending ? query.OrderByDescending(d => d.Name) : query.OrderBy(d => d.Name),
                "parentdepartmentname" => sortDescending ? query.OrderByDescending(d => d.ParentDepartment!.Name) : query.OrderBy(d => d.ParentDepartment!.Name),
                "usercount" => sortDescending
                    ? query.OrderByDescending(d => _dbContext.Users.Count(u => u.DepartmentId == d.Id))
                    : query.OrderBy(d => _dbContext.Users.Count(u => u.DepartmentId == d.Id)),
                "isactive" => sortDescending ? query.OrderByDescending(d => d.IsActive) : query.OrderBy(d => d.IsActive),
                "createdatutc" => sortDescending ? query.OrderByDescending(d => d.CreatedAtUtc) : query.OrderBy(d => d.CreatedAtUtc),
                _ => sortDescending ? query.OrderByDescending(d => d.Name) : query.OrderBy(d => d.Name),
            };
        }

        public Task<bool> NameExistsAsync(Guid companyId, string name, Guid? excludeId = null)
            => _dbContext.Departments.AnyAsync(d =>
                d.CompanyId == companyId &&
                d.Name.ToLower() == name.ToLower() &&
                (excludeId == null || d.Id != excludeId));

        public Task<int> CountUsersAsync(Guid departmentId)
            => _dbContext.Users.CountAsync(u => u.DepartmentId == departmentId);

        public async Task AddAsync(Department department)
        {
            _dbContext.Departments.Add(department);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Department department)
        {
            _dbContext.Departments.Update(department);
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(Department department)
        {
            _dbContext.Departments.Remove(department);
            await _dbContext.SaveChangesAsync();
        }
    }
}
