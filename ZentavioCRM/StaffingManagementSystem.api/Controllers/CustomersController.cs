using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Customers;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    [ApiController]
    [Route("api/customers")]
    [Produces("application/json")]
    [Authorize]
    public sealed class CustomersController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet]
        [Authorize(Policy = PermissionCodes.CustomersView)]
        public async Task<IActionResult> Search(
            [FromQuery] string? search,
            [FromQuery] Guid? assignedToUserId,
            [FromQuery] bool? isActive,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var result = await _customerService.SearchAsync(search, assignedToUserId, isActive, page, pageSize);
            return Ok(ApiResponse<PagedResult<CustomerListItemDto>>.SuccessResponse(result));
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionCodes.CustomersView)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _customerService.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [Authorize(Policy = PermissionCodes.CustomersCreate)]
        public async Task<IActionResult> Create([FromBody] SaveCustomerRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<CustomerDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _customerService.CreateAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionCodes.CustomersEdit)]
        public async Task<IActionResult> Update(Guid id, [FromBody] SaveCustomerRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<CustomerDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _customerService.UpdateAsync(id, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionCodes.CustomersDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _customerService.DeleteAsync(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private List<string> CollectErrors() => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    }
}
