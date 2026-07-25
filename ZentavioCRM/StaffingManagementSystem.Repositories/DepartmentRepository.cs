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
