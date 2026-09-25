using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Platform;
using ZentavioCRM.Core.Interfaces;

namespace ZentavioCRM.Api.Controllers.Platform
{
    /// <summary>
    /// Login for the Super Admin panel. Deliberately unauthenticated (that's the point of a login
    /// endpoint) — every other /api/platform/* route requires the "PlatformAdmin" policy this
    /// issues a token for. Route is under /api/platform, which TenantResolutionMiddleware
    /// always bypasses.
    /// </summary>
    [ApiController]
    [Route("api/platform/auth")]
    [Produces("application/json")]
    public sealed class PlatformAuthController : ControllerBase
    {
        private readonly IPlatformAdminService _platformAdminService;

        public PlatformAuthController(IPlatformAdminService platformAdminService)
        {
            _platformAdminService = platformAdminService;
        }

        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<PlatformLoginResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<PlatformLoginResponseDto>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] PlatformLoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<PlatformLoginResponseDto>.FailureResponse("Validation failed.", errors));
            }

            var result = await _platformAdminService.LoginAsync(request);
            return result.Success ? Ok(result) : Unauthorized(result);
        }
    }
}
