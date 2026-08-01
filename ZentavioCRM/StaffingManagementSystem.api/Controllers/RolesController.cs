using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Roles;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    [ApiController]
    [Route("api/roles")]
    [Produces("application/json")]
    [Authorize]
    public sealed class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        [Authorize(Policy = PermissionCodes.RolesView)]
        public async Task<IActionResult> GetAll()
        {
            var roles = await _roleService.GetAllAsync();
            return Ok(ApiResponse<IReadOnlyList<RoleDto>>.SuccessResponse(roles));
        }

        /// <summary>Paged, filterable, sortable role search — powers the Roles administration list grid.</summary>
        [HttpGet("search")]
        [Authorize(Policy = PermissionCodes.RolesView)]
        public async Task<IActionResult> Search(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortBy = null,
            [FromQuery] bool sortDescending = false)
        {
            var result = await _roleService.SearchAsync(search, page, pageSize, sortBy, sortDescending);
            return Ok(ApiResponse<PagedResult<RoleDto>>.SuccessResponse(result));
        }

        [HttpGet("permissions")]
        [Authorize(Policy = PermissionCodes.RolesView)]
        public async Task<IActionResult> GetPermissionCatalog()
        {
            var catalog = await _roleService.GetPermissionCatalogAsync();
            return Ok(ApiResponse<IReadOnlyDictionary<string, List<string>>>.SuccessResponse(catalog));
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionCodes.RolesView)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _roleService.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [Authorize(Policy = PermissionCodes.RolesManage)]
        public async Task<IActionResult> Create([FromBody] SaveRoleRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<RoleDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _roleService.CreateAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionCodes.RolesManage)]
        public async Task<IActionResult> Update(Guid id, [FromBody] SaveRoleRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<RoleDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _roleService.UpdateAsync(id, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionCodes.RolesManage)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _roleService.DeleteAsync(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private List<string> CollectErrors() => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    }
}
