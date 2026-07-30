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

        /// <summary>
        /// Exchanges a still-valid refresh token for a new access + refresh token pair, so the
        /// frontend can silently keep a session alive past the 15-minute access token lifetime.
        /// </summary>
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<LoginResponseDto>.FailureResponse("Validation failed.", errors));
            }

            var result = await _authService.RefreshAsync(request.RefreshToken);

            return result.Success ? Ok(result) : Unauthorized(result);
        }

        /// <summary>
        /// Revokes the caller's refresh token. Best-effort — always reports success so the client
        /// can safely clear its local session regardless of the token's server-side state.
        /// </summary>
        [HttpPost("logout")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Logout([FromBody] LogoutRequestDto request)
        {
            var result = await _authService.LogoutAsync(request.RefreshToken);

            return Ok(result);
        }

        /// <summary>
        /// Requests a password-reset email. Always returns 200 with the same generic message,
        /// whether or not the email matches a real account, to avoid leaking which addresses exist.
        /// </summary>
        [HttpPost("forgot-password")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<bool>.FailureResponse("Validation failed.", errors));
            }

            var result = await _authService.ForgotPasswordAsync(request.Email);

            return Ok(result);
        }

        /// <summary>
        /// Consumes a password-reset token (from the emailed link) to set a new password.
        /// </summary>
        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();

                return BadRequest(ApiResponse<bool>.FailureResponse("Validation failed.", errors));
            }

            var result = await _authService.ResetPasswordAsync(request.Token, request.NewPassword);

            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
