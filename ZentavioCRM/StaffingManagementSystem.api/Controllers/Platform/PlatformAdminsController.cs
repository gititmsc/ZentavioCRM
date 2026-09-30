using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Api.Authorization;
using ZentavioCRM.Api.Extensions;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Platform;
using ZentavioCRM.Core.Interfaces;

namespace ZentavioCRM.Api.Controllers.Platform
{
    /// <summary>
    /// Platform admin roster management — list existing admins and onboard new ones. Every
    /// endpoint here requires an already-authenticated platform admin; there's no self-service
    /// registration, so the very first account can only come from the database seed
    /// (see SQL Changes/Tenant Admin/003_SeedFirstPlatformAdmin.sql).
    /// </summary>
    [ApiController]
    [Route("api/platform/admins")]
    [Produces("application/json")]
    [Authorize(Policy = PlatformAuthorizationPolicies.PlatformAdmin)]
    public sealed class PlatformAdminsController : ControllerBase
    {
        private readonly IPlatformAdminService _platformAdminService;

        public PlatformAdminsController(IPlatformAdminService platformAdminService)
        {
            _platformAdminService = platformAdminService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var admins = await _platformAdminService.GetAllAsync();
            return Ok(ApiResponse<IReadOnlyList<PlatformAdminDto>>.SuccessResponse(admins));
        }

        [HttpPost]
        [Authorize(Policy = PlatformAuthorizationPolicies.PlatformSuperAdmin)]
        public async Task<IActionResult> Create([FromBody] CreatePlatformAdminRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<PlatformAdminDto>.FailureResponse("Validation failed.", errors));
            }

            var result = await _platformAdminService.CreateAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Self-service only — changes the CALLING admin's own password. There is
        /// deliberately no endpoint for one admin to reset another's password.</summary>
        [HttpPatch("me/password")]
        public async Task<IActionResult> ChangeMyPassword([FromBody] ChangePlatformAdminPasswordRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<bool>.FailureResponse("Validation failed.", errors));
            }

            var adminId = User.GetUserId();
            if (adminId is null)
            {
                return BadRequest(ApiResponse<bool>.FailureResponse("Could not identify the requesting admin.", ["Could not identify the requesting admin."]));
            }

            var result = await _platformAdminService.ChangePasswordAsync(adminId.Value, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
