using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Auth;

namespace ZentavioCRM.Services.Interfaces
{
    /// <summary>
    /// Business logic contract for authentication.
    /// </summary>
    public interface IAuthService
    {
        Task<ApiResponse<LoginResponseDto>> LoginAsync(LoginRequestDto request);
    }
}
