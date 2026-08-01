using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Api.Extensions;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Quotations;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    [ApiController]
    [Route("api/quotations")]
    [Produces("application/json")]
    [Authorize]
    public sealed class QuotationsController : ControllerBase
    {
        private readonly IQuotationService _quotationService;

        public QuotationsController(IQuotationService quotationService)
        {
            _quotationService = quotationService;
        }

        [HttpGet]
        [Authorize(Policy = PermissionCodes.QuotationsView)]
        public async Task<IActionResult> Search(
            [FromQuery] string? search,
            [FromQuery] QuotationStatus? status,
            [FromQuery] Guid? opportunityId,
            [FromQuery] Guid? customerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = true)
        {
            var result = await _quotationService.SearchAsync(search, status, opportunityId, customerId, page, pageSize, sortBy, sortDescending);
            return Ok(ApiResponse<PagedResult<QuotationListItemDto>>.SuccessResponse(result));
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionCodes.QuotationsView)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _quotationService.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [Authorize(Policy = PermissionCodes.QuotationsCreate)]
        public async Task<IActionResult> Create([FromBody] CreateQuotationRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<QuotationDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _quotationService.CreateAsync(request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionCodes.QuotationsEdit)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateQuotationRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<QuotationDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _quotationService.UpdateAsync(id, request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPatch("{id:guid}/status")]
        [Authorize(Policy = PermissionCodes.QuotationsEdit)]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateQuotationStatusRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<QuotationDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _quotationService.UpdateStatusAsync(id, request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:guid}/assign")]
        [Authorize(Policy = PermissionCodes.QuotationsAssign)]
        public async Task<IActionResult> Assign(Guid id, [FromBody] AssignQuotationRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<QuotationDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _quotationService.AssignAsync(id, request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:guid}/new-version")]
        [Authorize(Policy = PermissionCodes.QuotationsCreate)]
        public async Task<IActionResult> CreateNewVersion(Guid id)
        {
            var result = await _quotationService.CreateNewVersionAsync(id, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionCodes.QuotationsDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _quotationService.DeleteAsync(id, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private List<string> CollectErrors() => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    }
}
