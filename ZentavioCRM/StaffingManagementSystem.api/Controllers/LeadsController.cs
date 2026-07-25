using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Api.Extensions;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Leads;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    [ApiController]
    [Route("api/leads")]
    [Produces("application/json")]
    [Authorize]
    public sealed class LeadsController : ControllerBase
    {
        private readonly ILeadService _leadService;

        public LeadsController(ILeadService leadService)
        {
            _leadService = leadService;
        }

        [HttpGet]
        [Authorize(Policy = PermissionCodes.LeadsView)]
        public async Task<IActionResult> Search(
            [FromQuery] string? search,
            [FromQuery] LeadStatus? status,
            [FromQuery] Guid? assignedToUserId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _leadService.SearchAsync(search, status, assignedToUserId, page, pageSize);
            return Ok(ApiResponse<PagedResult<LeadListItemDto>>.SuccessResponse(result));
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionCodes.LeadsView)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _leadService.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [Authorize(Policy = PermissionCodes.LeadsCreate)]
        public async Task<IActionResult> Create([FromBody] SaveLeadRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<LeadDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _leadService.CreateAsync(request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionCodes.LeadsEdit)]
        public async Task<IActionResult> Update(Guid id, [FromBody] SaveLeadRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<LeadDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _leadService.UpdateAsync(id, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPatch("{id:guid}/status")]
        [Authorize(Policy = PermissionCodes.LeadsEdit)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateLeadStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<LeadDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _leadService.UpdateStatusAsync(id, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:guid}/assign")]
        [Authorize(Policy = PermissionCodes.LeadsAssign)]
        public async Task<IActionResult> Assign(Guid id, [FromBody] AssignLeadRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<LeadDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _leadService.AssignAsync(id, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:guid}/convert")]
        [Authorize(Policy = PermissionCodes.LeadsConvert)]
        public async Task<IActionResult> Convert(Guid id, [FromBody] ConvertLeadRequest request)
        {
            var result = await _leadService.ConvertAsync(id, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionCodes.LeadsDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _leadService.DeleteAsync(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private List<string> CollectErrors() => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    }
}
