using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Api.Extensions;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Common;
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

            var result = await _customerService.CreateAsync(request, User.GetUserId());
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

            var result = await _customerService.UpdateAsync(id, request, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionCodes.CustomersDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _customerService.DeleteAsync(id, User.GetUserId());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpGet("export")]
        [Authorize(Policy = PermissionCodes.CustomersView)]
        public async Task<IActionResult> Export()
        {
            var csv = await _customerService.ExportCsvAsync();
            return File(Encoding.UTF8.GetBytes(csv), "text/csv", "customers.csv");
        }

        [HttpPost("import")]
        [Authorize(Policy = PermissionCodes.CustomersCreate)]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Import(IFormFile file)
        {
            if (file.Length == 0)
            {
                return BadRequest(ApiResponse<ImportResultDto>.FailureResponse("No file was uploaded."));
            }

            using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8);
            var content = await reader.ReadToEndAsync();

            var result = await _customerService.ImportCsvAsync(content, User.GetUserId());
            return Ok(ApiResponse<ImportResultDto>.SuccessResponse(result, $"Imported {result.SuccessCount} of {result.TotalRows} rows."));
        }

        private List<string> CollectErrors() => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    }
}
