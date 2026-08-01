using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Departments;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="IDepartmentService"/>
    public class DepartmentService : IDepartmentService
    {
        private readonly IDepartmentRepository _departmentRepository;

        public DepartmentService(IDepartmentRepository departmentRepository)
        {
            _departmentRepository = departmentRepository;
        }

        public async Task<IReadOnlyList<DepartmentDto>> GetAllAsync()
        {
            var departments = await _departmentRepository.GetAllAsync();

            var result = new List<DepartmentDto>();
            foreach (var department in departments)
            {
                result.Add(await MapAsync(department));
            }

            return result;
        }

        public async Task<PagedResult<DepartmentDto>> SearchAsync(string? search, int page, int pageSize, string? sortBy = null, bool sortDescending = false)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize is < 1 or > 200 ? 20 : pageSize;

            var (items, totalCount) = await _departmentRepository.SearchAsync(search, page, pageSize, sortBy, sortDescending);

            var mapped = new List<DepartmentDto>();
            foreach (var department in items)
            {
                mapped.Add(await MapAsync(department));
            }

            return new PagedResult<DepartmentDto>
            {
                Items = mapped,
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
            };
        }

        public async Task<ApiResponse<DepartmentDto>> GetByIdAsync(Guid id)
        {
            var department = await _departmentRepository.GetByIdAsync(id);
            if (department is null)
            {
                return ApiResponse<DepartmentDto>.FailureResponse("Department not found.");
            }

            return ApiResponse<DepartmentDto>.SuccessResponse(await MapAsync(department));
        }

        public async Task<ApiResponse<DepartmentDto>> CreateAsync(SaveDepartmentRequest request)
        {
            if (await _departmentRepository.NameExistsAsync(SeedIds.DefaultCompanyId, request.Name))
            {
                return ApiResponse<DepartmentDto>.FailureResponse(
                    "A department with this name already exists.",
                    ["A department with this name already exists."]);
            }

            var department = new Department
            {
                CompanyId = SeedIds.DefaultCompanyId,
                Name = request.Name.Trim(),
                ParentDepartmentId = request.ParentDepartmentId,
                IsActive = request.IsActive,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _departmentRepository.AddAsync(department);

            return ApiResponse<DepartmentDto>.SuccessResponse(await MapAsync(department), "Department created.");
        }

        public async Task<ApiResponse<DepartmentDto>> UpdateAsync(Guid id, SaveDepartmentRequest request)
        {
            var department = await _departmentRepository.GetByIdAsync(id);
            if (department is null)
            {
                return ApiResponse<DepartmentDto>.FailureResponse("Department not found.");
            }

            if (request.ParentDepartmentId == id)
            {
                return ApiResponse<DepartmentDto>.FailureResponse(
                    "A department cannot be its own parent.",
                    ["A department cannot be its own parent."]);
            }

            if (await _departmentRepository.NameExistsAsync(SeedIds.DefaultCompanyId, request.Name, id))
            {
                return ApiResponse<DepartmentDto>.FailureResponse(
                    "A department with this name already exists.",
                    ["A department with this name already exists."]);
            }

            department.Name = request.Name.Trim();
            department.ParentDepartmentId = request.ParentDepartmentId;
            department.IsActive = request.IsActive;
            department.UpdatedAtUtc = DateTime.UtcNow;

            await _departmentRepository.UpdateAsync(department);

            return ApiResponse<DepartmentDto>.SuccessResponse(await MapAsync(department), "Department updated.");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            var department = await _departmentRepository.GetByIdAsync(id);
            if (department is null)
            {
                return ApiResponse<bool>.FailureResponse("Department not found.");
            }

            var userCount = await _departmentRepository.CountUsersAsync(id);
            if (userCount > 0)
            {
                return ApiResponse<bool>.FailureResponse(
                    $"Cannot delete — {userCount} user(s) are still assigned to this department.",
                    ["Reassign or remove the users in this department first."]);
            }

            await _departmentRepository.DeleteAsync(department);

            return ApiResponse<bool>.SuccessResponse(true, "Department deleted.");
        }

        private async Task<DepartmentDto> MapAsync(Department department) => new()
        {
            Id = department.Id,
            Name = department.Name,
            ParentDepartmentId = department.ParentDepartmentId,
            ParentDepartmentName = department.ParentDepartment?.Name,
            IsActive = department.IsActive,
            UserCount = await _departmentRepository.CountUsersAsync(department.Id),
        };
    }
}
