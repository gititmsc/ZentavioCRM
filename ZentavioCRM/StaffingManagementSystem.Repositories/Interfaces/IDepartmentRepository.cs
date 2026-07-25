using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<Department?> GetByIdAsync(Guid id);

        Task<IReadOnlyList<Department>> GetAllAsync();

        Task<bool> NameExistsAsync(Guid companyId, string name, Guid? excludeId = null);

        Task<int> CountUsersAsync(Guid departmentId);

        Task AddAsync(Department department);

        Task UpdateAsync(Department department);

        Task DeleteAsync(Department department);
    }
}
