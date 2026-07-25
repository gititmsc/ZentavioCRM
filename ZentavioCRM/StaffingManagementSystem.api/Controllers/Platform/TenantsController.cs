using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Api.Filters;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Platform;
using ZentavioCRM.Core.Interfaces;

namespace ZentavioCRM.Api.Controllers.Platform
{
    /// <summary>
    /// Tenant lifecycle management — provisioning new tenants and listing existing ones. Lives
    /// outside the normal per-tenant JWT/permission scheme (see <see cref="RequirePlatformKeyAttribute"/>)
    /// since these operations happen before any tenant — or its users — exist yet.
    /// Route is under /api/platform, which TenantResolutionMiddleware always bypasses.
    /// </summary>
    [ApiController]
    [Route("api/platform/tenants")]
    [Produces("application/json")]
    [RequirePlatformKey]
    public sealed class TenantsController : ControllerBase
    {
        private readonly ITenantProvisioningService _provisioningService;

        public TenantsController(ITenantProvisioningService provisioningService)
        {
            _provisioningService = provisioningService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tenants = await _provisioningService.GetAllAsync();
            return Ok(ApiResponse<IReadOnlyList<TenantDto>>.SuccessResponse(tenants));
        }

        [HttpPost]
        public async Task<IActionResult> Provision([FromBody] ProvisionTenantRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<TenantDto>.FailureResponse("Validation failed.", errors));
            }

            var result = await _provisioningService.ProvisionAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
