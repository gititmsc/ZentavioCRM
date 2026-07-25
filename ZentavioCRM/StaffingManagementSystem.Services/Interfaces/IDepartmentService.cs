using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Departments;

namespace ZentavioCRM.Services.Interfaces
{
    public interface IDepartmentService
    {
        Task<IReadOnlyList<DepartmentDto>> GetAllAsync();

        Task<ApiResponse<DepartmentDto>> GetByIdAsync(Guid id);

        Task<ApiResponse<DepartmentDto>> CreateAsync(SaveDepartmentRequest request);

        Task<ApiResponse<DepartmentDto>> UpdateAsync(Guid id, SaveDepartmentRequest request);

        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
