using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Users;

namespace ZentavioCRM.Services.Interfaces
{
    public interface IUserService
    {
        Task<IReadOnlyList<UserDto>> GetAllAsync();

        Task<ApiResponse<UserDto>> GetByIdAsync(Guid id);

        Task<ApiResponse<UserDto>> CreateAsync(CreateUserRequest request);

        Task<ApiResponse<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request);
    }
}
