using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Auth;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    /// <summary>
    /// Authentication endpoints — thin controller, all logic in IAuthService.
    /// </summary>
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public sealed class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// Authenticates a user with email and password and returns a JWT access token.
        /// </summary>
        [HttpPost("login")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<LoginResponseDto>.FailureResponse("Validation failed.", errors));
            }

            var result = await _authService.LoginAsync(request);

            return result.Success ? Ok(result) : Unauthorized(result);
        }
    }
}
