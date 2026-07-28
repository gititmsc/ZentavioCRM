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

        Task<ApiResponse<UserDto>> UploadPhotoAsync(Guid id, string contentType, byte[] content);

        /// <summary>Returns null if the user doesn't exist or has no photo uploaded.</summary>
        Task<(byte[] Content, string ContentType)?> DownloadPhotoAsync(Guid id);

        Task<ApiResponse<bool>> DeletePhotoAsync(Guid id);
    }
}
