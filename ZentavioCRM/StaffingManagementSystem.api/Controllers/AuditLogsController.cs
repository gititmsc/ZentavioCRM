using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Audit;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    /// <summary>
    /// History/audit trail for a single record. Any authenticated user can view history for an
    /// entity type/id they know about — this endpoint does not re-check the underlying module's
    /// View permission (e.g. Leads.View) for simplicity in this milestone, matching the same
    /// simplification used by DashboardController.
    /// </summary>
    [ApiController]
    [Route("api/audit-logs")]
    [Produces("application/json")]
    [Authorize]
    public sealed class AuditLogsController : ControllerBase
    {
        private readonly IAuditLogService _auditLogService;

        public AuditLogsController(IAuditLogService auditLogService)
        {
            _auditLogService = auditLogService;
        }

        [HttpGet]
        public async Task<IActionResult> GetForEntity([FromQuery] string entityType, [FromQuery] Guid entityId)
        {
            var logs = await _auditLogService.GetForEntityAsync(entityType, entityId);
            return Ok(ApiResponse<IReadOnlyList<AuditLogDto>>.SuccessResponse(logs));
        }
    }
}
