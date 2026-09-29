using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Api.Authorization;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Platform;
using ZentavioCRM.Core.Interfaces;

namespace ZentavioCRM.Api.Controllers.Platform
{
    /// <summary>
    /// Read-only view of the platform-level audit trail (admin logins, tenant provisioned/
    /// suspended/reactivated/stopped, and future actions like impersonation). Same Platform Admin
    /// session requirement as every other controller under /api/platform.
    /// </summary>
    [ApiController]
    [Route("api/platform/audit-log")]
    [Produces("application/json")]
    [Authorize(Policy = PlatformAuthorizationPolicies.PlatformAdmin)]
    public sealed class PlatformAuditLogController : ControllerBase
    {
        private readonly IPlatformAuditLogService _auditLog;

        public PlatformAuditLogController(IPlatformAuditLogService auditLog)
        {
            _auditLog = auditLog;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] Guid? tenantId)
        {
            var entries = await _auditLog.GetAllAsync(tenantId);
            return Ok(ApiResponse<IReadOnlyList<PlatformAuditLogDto>>.SuccessResponse(entries));
        }
    }
}
