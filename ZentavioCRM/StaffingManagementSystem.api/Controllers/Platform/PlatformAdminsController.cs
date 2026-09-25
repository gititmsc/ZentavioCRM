using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Api.Authorization;
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
    }
}
