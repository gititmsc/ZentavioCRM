using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Users;

namespace ZentavioCRM.Services.Interfaces
{
    public interface IUserService
    {
        Task<IReadOnlyList<UserDto>> GetAllAsync();

        /// <summary>Paged, filterable, sortable user search — powers the Users administration list.</summary>
        /// <param name="sortBy">Column key (case-insensitive): employeeCode, fullName, email, roleName, departmentName, isActive, createdAtUtc. Unrecognized/null falls back to createdAtUtc.</param>
        Task<PagedResult<UserDto>> SearchAsync(
            string? search, Guid? roleId, Guid? departmentId, bool? isActive, int page, int pageSize,
            string? sortBy = null, bool sortDescending = true);

        Task<ApiResponse<UserDto>> GetByIdAsync(Guid id);

        Task<ApiResponse<UserDto>> CreateAsync(CreateUserRequest request);

        Task<ApiResponse<UserDto>> UpdateAsync(Guid id, UpdateUserRequest request);

        Task<ApiResponse<UserDto>> UploadPhotoAsync(Guid id, string contentType, byte[] content);

        /// <summary>Returns null if the user doesn't exist or has no photo uploaded.</summary>
        Task<(byte[] Content, string ContentType)?> DownloadPhotoAsync(Guid id);

        Task<ApiResponse<bool>> DeletePhotoAsync(Guid id);

        /// <summary>
        /// Self-service password change. Verifies <paramref name="request"/>'s CurrentPassword,
        /// then revokes every existing refresh token for this user and issues a fresh token pair —
        /// so the session that made this change continues seamlessly while every other
        /// device/session is signed out.
        /// </summary>
        Task<ApiResponse<ZentavioCRM.Core.DTOs.Auth.LoginResponseDto>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);

        /// <summary>
        /// Admin-initiated password reset for another user — no current-password proof required.
        /// Revokes every refresh token for that user (including any session they're currently in),
        /// since this represents someone else acting on the account.
        /// </summary>
        Task<ApiResponse<bool>> AdminResetPasswordAsync(Guid userId, AdminResetPasswordRequest request);
    }
}
