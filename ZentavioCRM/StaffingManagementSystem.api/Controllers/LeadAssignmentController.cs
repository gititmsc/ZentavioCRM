using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Api.Extensions;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Leads;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    /// <summary>Lead auto-assignment: the tenant-wide on/off switch plus round-robin/territory routing rules. Entirely gated by Leads.ManageAssignment — a settings-level capability, not a day-to-day one.</summary>
    [ApiController]
    [Route("api/lead-assignment")]
    [Produces("application/json")]
    [Authorize(Policy = PermissionCodes.LeadsManageAssignment)]
    public sealed class LeadAssignmentController : ControllerBase
    {
        private readonly ILeadAssignmentService _leadAssignmentService;

        public LeadAssignmentController(ILeadAssignmentService leadAssignmentService)
        {
            _leadAssignmentService = leadAssignmentService;
        }

        [HttpGet("settings")]
        public async Task<IActionResult> GetSettings()
        {
            var settings = await _leadAssignmentService.GetSettingsAsync();
            return Ok(ApiResponse<LeadAssignmentSettingsDto>.SuccessResponse(settings));
        }

        [HttpPut("settings")]
        public async Task<IActionResult> UpdateSettings([FromBody] LeadAssignmentSettingsDto request)
        {
            var result = await _leadAssignmentService.UpdateSettingsAsync(request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("rules")]
        public async Task<IActionResult> GetRules()
        {
            var rules = await _leadAssignmentService.GetRulesAsync();
            return Ok(ApiResponse<IReadOnlyList<LeadAssignmentRuleDto>>.SuccessResponse(rules));
        }

        [HttpPost("rules")]
        public async Task<IActionResult> CreateRule([FromBody] SaveLeadAssignmentRuleRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<LeadAssignmentRuleDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _leadAssignmentService.CreateRuleAsync(request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("rules/{id:guid}")]
        public async Task<IActionResult> UpdateRule(Guid id, [FromBody] SaveLeadAssignmentRuleRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<LeadAssignmentRuleDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _leadAssignmentService.UpdateRuleAsync(id, request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("rules/{id:guid}")]
        public async Task<IActionResult> DeleteRule(Guid id)
        {
            var result = await _leadAssignmentService.DeleteRuleAsync(id, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private List<string> CollectErrors() => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    }
}
