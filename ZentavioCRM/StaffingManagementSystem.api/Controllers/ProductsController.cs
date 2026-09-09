using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Products;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    [ApiController]
    [Route("api/products")]
    [Produces("application/json")]
    [Authorize]
    public sealed class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService)
        {
            _productService = productService;
        }

        /// <summary>Unpaged, active-only list — powers the "pick from catalog" selector on Opportunity/Quotation line items.</summary>
        [HttpGet]
        [Authorize(Policy = PermissionCodes.ProductsView)]
        public async Task<IActionResult> GetAllActive()
        {
            var products = await _productService.GetAllActiveAsync();
            return Ok(ApiResponse<IReadOnlyList<ProductDto>>.SuccessResponse(products));
        }

        /// <summary>Paged, filterable, sortable product search — powers the Product Catalog administration list grid.</summary>
        [HttpGet("search")]
        [Authorize(Policy = PermissionCodes.ProductsView)]
        public async Task<IActionResult> Search(
            [FromQuery] string? search,
            [FromQuery] string? type = null,
            [FromQuery] string? category = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = false)
        {
            var result = await _productService.SearchAsync(search, type, category, isActive, page, pageSize, sortBy, sortDescending);
            return Ok(ApiResponse<PagedResult<ProductDto>>.SuccessResponse(result));
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionCodes.ProductsView)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _productService.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [Authorize(Policy = PermissionCodes.ProductsCreate)]
        public async Task<IActionResult> Create([FromBody] SaveProductRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<ProductDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _productService.CreateAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionCodes.ProductsEdit)]
        public async Task<IActionResult> Update(Guid id, [FromBody] SaveProductRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<ProductDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _productService.UpdateAsync(id, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionCodes.ProductsDelete)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _productService.DeleteAsync(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private List<string> CollectErrors() => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    }
}
