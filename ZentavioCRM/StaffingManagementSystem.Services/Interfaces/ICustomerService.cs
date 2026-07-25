using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Customers;

namespace ZentavioCRM.Services.Interfaces
{
    public interface ICustomerService
    {
        Task<PagedResult<CustomerListItemDto>> SearchAsync(
            string? search, Guid? assignedToUserId, bool? isActive, int page, int pageSize);

        Task<ApiResponse<CustomerDto>> GetByIdAsync(Guid id);

        Task<ApiResponse<CustomerDto>> CreateAsync(SaveCustomerRequest request);

        Task<ApiResponse<CustomerDto>> UpdateAsync(Guid id, SaveCustomerRequest request);

        Task<ApiResponse<bool>> DeleteAsync(Guid id);
    }
}
