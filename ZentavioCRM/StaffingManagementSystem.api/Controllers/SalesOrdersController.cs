using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Api.Extensions;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.SalesOrders;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    [ApiController]
    [Route("api/sales-orders")]
    [Produces("application/json")]
    [Authorize]
    public sealed class SalesOrdersController : ControllerBase
    {
        private readonly ISalesOrderService _salesOrderService;

        public SalesOrdersController(ISalesOrderService salesOrderService)
        {
            _salesOrderService = salesOrderService;
        }

        [HttpGet]
        [Authorize(Policy = PermissionCodes.SalesOrdersView)]
        public async Task<IActionResult> Search(
            [FromQuery] string? search,
            [FromQuery] SalesOrderStatus? status,
            [FromQuery] Guid? customerId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _salesOrderService.SearchAsync(search, status, customerId, page, pageSize);
            return Ok(ApiResponse<PagedResult<SalesOrderListItemDto>>.SuccessResponse(result));
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionCodes.SalesOrdersView)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _salesOrderService.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost("from-quotation")]
        [Authorize(Policy = PermissionCodes.SalesOrdersCreate)]
        public async Task<IActionResult> ConvertFromQuotation([FromBody] ConvertQuotationToSalesOrderRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<SalesOrderDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _salesOrderService.ConvertFromQuotationAsync(request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionCodes.SalesOrdersEdit)]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSalesOrderRequest request)
        {
            var result = await _salesOrderService.UpdateAsync(id, request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:guid}/assign")]
        [Authorize(Policy = PermissionCodes.SalesOrdersAssign)]
        public async Task<IActionResult> Assign(Guid id, [FromBody] AssignSalesOrderRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<SalesOrderDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _salesOrderService.AssignAsync(id, request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:guid}/deliveries")]
        [Authorize(Policy = PermissionCodes.SalesOrdersEdit)]
        public async Task<IActionResult> RecordDelivery(Guid id, [FromBody] RecordDeliveryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<SalesOrderDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _salesOrderService.RecordDeliveryAsync(id, request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:guid}/cancel")]
        [Authorize(Policy = PermissionCodes.SalesOrdersEdit)]
        public async Task<IActionResult> Cancel(Guid id)
        {
            var result = await _salesOrderService.CancelAsync(id, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private List<string> CollectErrors() => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    }
}
