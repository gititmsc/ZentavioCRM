using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Platform;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Core.Interfaces;

namespace ZentavioCRM.Api.Controllers.Platform
{
    /// <summary>
    /// Public, unauthenticated tenant self-service signup — a new company provisions its own
    /// Trial tenant without a platform admin doing it for them. Deliberately has no [Authorize]:
    /// this is the one endpoint under /api/platform meant to be reachable by anyone. Still under
    /// the /api/platform prefix so TenantResolutionMiddleware bypasses tenant resolution for it
    /// (there's no tenant yet — that's the whole point).
    ///
    /// Rate-limited (see the "signup" policy in Program.cs) since this is the only unauthenticated
    /// endpoint in the app that provisions a real database — without a limit, it would be a trivial
    /// resource-exhaustion vector.
    /// </summary>
    [ApiController]
    [Route("api/platform/signup")]
    [Produces("application/json")]
    [EnableRateLimiting("signup")]
    public sealed class SignupController : ControllerBase
    {
        private readonly ITenantProvisioningService _provisioningService;

        public SignupController(ITenantProvisioningService provisioningService)
        {
            _provisioningService = provisioningService;
        }

        [HttpPost]
        public async Task<IActionResult> Signup([FromBody] TenantSignupRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<TenantDto>.FailureResponse("Validation failed.", errors));
            }

            // Forced to Trial regardless of anything the client sends — TenantSignupRequest has no
            // PlanTier field at all, but this stays explicit here as the one place that decides it.
            var provisionRequest = new ProvisionTenantRequest
            {
                PlanTier = PlanTier.Trial,
                CompanyName = request.CompanyName,
                Subdomain = request.Subdomain,
                AdminFirstName = request.AdminFirstName,
                AdminLastName = request.AdminLastName,
                AdminEmail = request.AdminEmail,
                AdminPassword = request.AdminPassword,
            };

            // Null performedByAdminId — ITenantProvisioningService already supports this for
            // system-initiated provisioning; the audit trail records it as such rather than
            // attributing it to any platform admin.
            var result = await _provisioningService.ProvisionAsync(provisionRequest, performedByAdminId: null);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
