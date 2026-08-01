using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Departments;

namespace ZentavioCRM.Services.Interfaces
{
    public interface IDepartmentService
    {
        Task<IReadOnlyList<DepartmentDto>> GetAllAsync();

        /// <summary>Paged, filterable, sortable department search — powers the Departments administration list.</summary>
        /// <param name="sortBy">Column key (case-insensitive): name, parentDepartmentName, userCount, isActive, createdAtUtc. Unrecognized/null falls back to name.</param>
        Task<PagedResult<DepartmentDto>> SearchAsync(string? search, int page, int pageSize, string? sortBy = null, bool sortDescending = false);

        Task<ApiResponse<DepartmentDto>> GetByIdAsync(Guid id);

        Task<ApiResponse<DepartmentDto>> CreateAsync(SaveDepartmentRequest request);

        Task<ApiResponse<DepartmentDto>> UpdateAsync(Guid id, SaveDepartmentRequest request);

        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
