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
    /// Tenant lifecycle management — provisioning, listing, and suspending/reactivating/stopping
    /// existing tenants. Lives outside the normal per-tenant JWT/permission scheme — requires a
    /// Platform Admin session (see <see cref="PlatformAuthorizationPolicies.PlatformAdmin"/>)
    /// instead, since these operations happen before any tenant — or its users — exist yet, or
    /// act on a tenant that shouldn't be able to act on itself.
    /// Route is under /api/platform, which TenantResolutionMiddleware always bypasses.
    /// </summary>
    [ApiController]
    [Route("api/platform/tenants")]
    [Produces("application/json")]
    [Authorize(Policy = PlatformAuthorizationPolicies.PlatformAdmin)]
    public sealed class TenantsController : ControllerBase
    {
        private readonly ITenantProvisioningService _provisioningService;
        private readonly ITenantUsageService _usageService;
        private readonly IImpersonationService _impersonationService;
        private readonly ITenantBillingService _billingService;
        private readonly ITenantNoteService _noteService;

        public TenantsController(
            ITenantProvisioningService provisioningService,
            ITenantUsageService usageService,
            IImpersonationService impersonationService,
            ITenantBillingService billingService,
            ITenantNoteService noteService)
        {
            _provisioningService = provisioningService;
            _usageService = usageService;
            _impersonationService = impersonationService;
            _billingService = billingService;
            _noteService = noteService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var tenants = await _provisioningService.GetAllAsync();
            return Ok(ApiResponse<IReadOnlyList<TenantDto>>.SuccessResponse(tenants));
        }

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _provisioningService.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        public async Task<IActionResult> Provision([FromBody] ProvisionTenantRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<TenantDto>.FailureResponse("Validation failed.", errors));
            }

            var result = await _provisioningService.ProvisionAsync(request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPatch("{id:guid}/suspend")]
        public async Task<IActionResult> Suspend(Guid id, [FromBody] SuspendTenantRequest? request)
        {
            var result = await _provisioningService.SuspendAsync(id, request?.Reason, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPatch("{id:guid}/reactivate")]
        public async Task<IActionResult> Reactivate(Guid id)
        {
            var result = await _provisioningService.ReactivateAsync(id, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPatch("{id:guid}/stop")]
        public async Task<IActionResult> Stop(Guid id, [FromBody] StopTenantRequest? request)
        {
            var result = await _provisioningService.StopAsync(id, request?.Reason, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPatch("{id:guid}/plan")]
        public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] UpdateTenantPlanRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<TenantDto>.FailureResponse("Validation failed.", errors));
            }

            var result = await _provisioningService.UpdatePlanAsync(id, request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id:guid}/usage")]
        public async Task<IActionResult> GetUsage(Guid id)
        {
            var result = await _usageService.GetUsageAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        /// <summary>Every Active tenant/metric pair at or above 80% of its plan limit — powers the
        /// "Tenants nearing limits" Dashboard widget.</summary>
        [HttpGet("usage-alerts")]
        public async Task<IActionResult> GetUsageAlerts()
        {
            var result = await _usageService.GetTenantsNearLimitsAsync();
            return Ok(result);
        }

        /// <summary>Mints a short-lived tenant session token for this tenant's admin user. See
        /// <see cref="IImpersonationService"/> for the safeguards — this is the single most
        /// security-sensitive action in the whole Platform Admin surface.</summary>
        [HttpPost("{id:guid}/impersonate")]
        public async Task<IActionResult> Impersonate(Guid id, [FromBody] ImpersonateTenantRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<ImpersonateTenantResponseDto>.FailureResponse("Validation failed.", errors));
            }

            // Defense in depth: the PlatformAdmin policy already guarantees an authenticated
            // admin, so GetUserId() should never be null here — but impersonation is sensitive
            // enough to fail closed rather than proceed with an unattributed audit entry if that
            // assumption is ever wrong.
            var performedByAdminId = User.GetUserId();
            if (performedByAdminId is null)
            {
                return BadRequest(ApiResponse<ImpersonateTenantResponseDto>.FailureResponse(
                    "Could not identify the requesting admin.",
                    ["Could not identify the requesting admin."]));
            }

            var result = await _impersonationService.ImpersonateAsync(id, request.Reason, performedByAdminId);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Edits company name + the denormalized directory admin-email field. Does not
        /// touch the tenant's own database or its real admin user's sign-in email.</summary>
        [HttpPatch("{id:guid}/metadata")]
        public async Task<IActionResult> UpdateMetadata(Guid id, [FromBody] UpdateTenantMetadataRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<TenantDto>.FailureResponse("Validation failed.", errors));
            }

            var result = await _provisioningService.UpdateMetadataAsync(id, request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        /// <summary>Manual billing fields only — no payment gateway involved. Setting
        /// PaymentStatus to Overdue on an Active tenant auto-suspends it.</summary>
        [HttpPatch("{id:guid}/billing")]
        public async Task<IActionResult> UpdateBilling(Guid id, [FromBody] UpdateTenantBillingRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<TenantDto>.FailureResponse("Validation failed.", errors));
            }

            var result = await _billingService.UpdateBillingAsync(id, request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id:guid}/payments")]
        public async Task<IActionResult> GetPayments(Guid id)
        {
            var result = await _billingService.GetPaymentsAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        /// <summary>Records a manual payment-history entry — never a real charge. Sets
        /// PaymentStatus to Paid and advances NextDueDateUtc by one BillingCycle when set.</summary>
        [HttpPost("{id:guid}/payments")]
        public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordTenantPaymentRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<TenantPaymentDto>.FailureResponse("Validation failed.", errors));
            }

            var result = await _billingService.RecordPaymentAsync(id, request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id:guid}/notes")]
        public async Task<IActionResult> GetNotes(Guid id)
        {
            var result = await _noteService.GetNotesAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost("{id:guid}/notes")]
        public async Task<IActionResult> AddNote(Guid id, [FromBody] CreateTenantNoteRequest request)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<TenantNoteDto>.FailureResponse("Validation failed.", errors));
            }

            var result = await _noteService.AddNoteAsync(id, request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}
