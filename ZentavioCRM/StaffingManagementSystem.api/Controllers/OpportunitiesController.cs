using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Api.Extensions;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Opportunities;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    [ApiController]
    [Route("api/opportunities")]
    [Produces("application/json")]
    [Authorize]
    public sealed class OpportunitiesController : ControllerBase
    {
        private readonly IOpportunityService _opportunityService;

        public OpportunitiesController(IOpportunityService opportunityService)
        {
            _opportunityService = opportunityService;
        }

        [HttpGet]
        [Authorize(Policy = PermissionCodes.OpportunitiesView)]
        public async Task<IActionResult> Search(
            [FromQuery] string? search,
            [FromQuery] OpportunityStage? stage,
            [FromQuery] Guid? customerId,
            [FromQuery] Guid? assignedToUserId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _opportunityService.SearchAsync(search, stage, customerId, assignedToUserId, page, pageSize, User.GetUserId());
            return Ok(ApiResponse<PagedResult<OpportunityListItemDto>>.SuccessResponse(result));
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionCodes.OpportunitiesView)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _opportunityService.GetByIdAsync(id, User.GetUserId());
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [Authorize(Policy = PermissionCodes.OpportunitiesCreate)]
        public async Task<IActionResult> Create([FromBody] SaveOpportunityRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<OpportunityDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _opportunityService.CreateAsync(request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionCodes.OpportunitiesEdit)]
        public async Task<IActionResult> Update(Guid id, [FromBody] SaveOpportunityRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<OpportunityDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _opportunityService.UpdateAsync(id, request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPatch("{id:guid}/stage")]
        [Authorize(Policy = PermissionCodes.OpportunitiesEdit)]
        public async Task<IActionResult> UpdateStage(Guid id, [FromBody] UpdateOpportunityStageRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<OpportunityDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _opportunityService.UpdateStageAsync(id, request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:guid}/assign")]
        [Authorize(Policy = PermissionCodes.OpportunitiesAssign)]
        public async Task<IActionResult> Assign(Guid id, [FromBody] AssignOpportunityRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<OpportunityDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _opportunityService.AssignAsync(id, request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionCodes.OpportunitiesDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _opportunityService.DeleteAsync(id, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private List<string> CollectErrors() => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    }
}
