using ZentavioCRM.Core.Entities;

namespace ZentavioCRM.Repositories.Interfaces
{
    public interface IDepartmentRepository
    {
        Task<Department?> GetByIdAsync(Guid id);

        Task<IReadOnlyList<Department>> GetAllAsync();

        /// <summary>Paged, filterable, sortable department search — powers the Departments administration list.</summary>
        /// <param name="sortBy">Column key (case-insensitive): name, parentDepartmentName, userCount, isActive, createdAtUtc. Unrecognized/null falls back to name.</param>
        Task<(IReadOnlyList<Department> Items, int TotalCount)> SearchAsync(
            string? search, int page, int pageSize, string? sortBy = null, bool sortDescending = false);

        Task<bool> NameExistsAsync(Guid companyId, string name, Guid? excludeId = null);

        Task<int> CountUsersAsync(Guid departmentId);

        Task AddAsync(Department department);

        Task UpdateAsync(Department department);

        Task DeleteAsync(Department department);
    }
}
