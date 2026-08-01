using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Common;
using ZentavioCRM.Core.DTOs.Customers;

namespace ZentavioCRM.Services.Interfaces
{
    public interface ICustomerService
    {
        /// <param name="sortBy">Column key (case-insensitive): customerNumber, displayName, type, industry, healthStatus, assignedToUserName, isActive, createdAtUtc. Unrecognized/null falls back to createdAtUtc.</param>
        Task<PagedResult<CustomerListItemDto>> SearchAsync(
            string? search, Guid? assignedToUserId, bool? isActive, int page, int pageSize, Guid? currentUserId = null,
            string? sortBy = null, bool sortDescending = true);

        Task<ApiResponse<CustomerDto>> GetByIdAsync(Guid id, Guid? currentUserId = null);

        Task<ApiResponse<CustomerDto>> CreateAsync(SaveCustomerRequest request, Guid? currentUserId);

        Task<ApiResponse<CustomerDto>> UpdateAsync(Guid id, SaveCustomerRequest request, Guid? currentUserId);

        Task<ApiResponse<bool>> DeleteAsync(Guid id, Guid? currentUserId);

        Task<string> ExportCsvAsync();

        Task<ImportResultDto> ImportCsvAsync(string csvContent, Guid? currentUserId);
    }
}
