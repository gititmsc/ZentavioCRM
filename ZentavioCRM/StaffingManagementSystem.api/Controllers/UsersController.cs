using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Api.Extensions;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Auth;
using ZentavioCRM.Core.DTOs.Users;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Produces("application/json")]
    [Authorize]
    public sealed class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        // Intentionally just [Authorize] rather than gated on Users.View: the assignment
        // dropdowns on Leads/Customers need the user directory for every authenticated user,
        // not just those with the Users administration permission. Only Create/Update (actually
        // managing accounts) require Users.Manage.
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(ApiResponse<IReadOnlyList<UserDto>>.SuccessResponse(users));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _userService.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [Authorize(Policy = PermissionCodes.UsersManage)]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<UserDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _userService.CreateAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionCodes.UsersManage)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<UserDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _userService.UpdateAsync(id, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // Profile photo — self-service (a user can manage their own avatar) OR Users.Manage
        // (an admin managing someone else's). Download is open to any authenticated user,
        // same as GetAll/GetById above, since avatars need to render in shared lists.

        [HttpGet("{id:guid}/photo")]
        public async Task<IActionResult> DownloadPhoto(Guid id)
        {
            var photo = await _userService.DownloadPhotoAsync(id);
            if (photo is null)
            {
                return NotFound(ApiResponse<bool>.FailureResponse("No profile photo uploaded."));
            }

            return File(photo.Value.Content, photo.Value.ContentType);
        }

        [HttpPost("{id:guid}/photo")]
        [RequestSizeLimit(5 * 1024 * 1024)]
        public async Task<IActionResult> UploadPhoto(Guid id, IFormFile file)
        {
            if (!CanManagePhoto(id))
            {
                return Forbid();
            }

            if (file is null || file.Length == 0)
            {
                return BadRequest(ApiResponse<UserDto>.FailureResponse("No file was uploaded."));
            }

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            var content = stream.ToArray();

            // Validate — and derive the stored Content-Type from — the file's own bytes, not the
            // client-supplied file.ContentType header. That header reflects the file's EXTENSION in
            // most browsers, not its actual content, so a file renamed from ".html" to ".png" would
            // still arrive here claiming "image/png". Accepted formats: PNG, JPEG, GIF.
            var detectedContentType = ImageSignature.Detect(content);
            if (detectedContentType is null)
            {
                return BadRequest(ApiResponse<UserDto>.FailureResponse(
                    "Unsupported file type.",
                    ["Profile photos must be a genuine PNG, JPEG, or GIF image."]));
            }

            var result = await _userService.UploadPhotoAsync(id, detectedContentType, content);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:guid}/photo")]
        public async Task<IActionResult> DeletePhoto(Guid id)
        {
            if (!CanManagePhoto(id))
            {
                return Forbid();
            }

            var result = await _userService.DeletePhotoAsync(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private bool CanManagePhoto(Guid targetUserId)
            => targetUserId == User.GetUserId() || User.HasClaim(PermissionCodes.ClaimType, PermissionCodes.UsersManage);

        /// <summary>
        /// Self-service password change — only the account owner can call this (proof of identity
        /// is the CurrentPassword field itself, so there's no reason to also allow admins here;
        /// admins use <see cref="ResetPassword"/> instead, which is deliberately a distinct endpoint).
        /// </summary>
        [HttpPost("{id:guid}/change-password")]
        public async Task<IActionResult> ChangePassword(Guid id, [FromBody] ChangePasswordRequest request)
        {
            if (id != User.GetUserId())
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<LoginResponseDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _userService.ChangePasswordAsync(id, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Admin-initiated password reset for another user — requires Users.Manage.</summary>
        [HttpPost("{id:guid}/reset-password")]
        [Authorize(Policy = PermissionCodes.UsersManage)]
        public async Task<IActionResult> ResetPassword(Guid id, [FromBody] AdminResetPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<bool>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _userService.AdminResetPasswordAsync(id, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private List<string> CollectErrors() => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    }
}
