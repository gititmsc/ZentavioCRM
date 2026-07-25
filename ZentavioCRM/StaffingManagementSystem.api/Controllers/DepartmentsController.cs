using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Departments;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    [ApiController]
    [Route("api/departments")]
    [Produces("application/json")]
    [Authorize]
    public sealed class DepartmentsController : ControllerBase
    {
        private readonly IDepartmentService _departmentService;

        public DepartmentsController(IDepartmentService departmentService)
        {
            _departmentService = departmentService;
        }

        [HttpGet]
        [Authorize(Policy = PermissionCodes.DepartmentsView)]
        public async Task<IActionResult> GetAll()
        {
            var departments = await _departmentService.GetAllAsync();
            return Ok(ApiResponse<IReadOnlyList<DepartmentDto>>.SuccessResponse(departments));
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionCodes.DepartmentsView)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _departmentService.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [Authorize(Policy = PermissionCodes.DepartmentsManage)]
        public async Task<IActionResult> Create([FromBody] SaveDepartmentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<DepartmentDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _departmentService.CreateAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionCodes.DepartmentsManage)]
        public async Task<IActionResult> Update(Guid id, [FromBody] SaveDepartmentRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<DepartmentDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _departmentService.UpdateAsync(id, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionCodes.DepartmentsManage)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _departmentService.DeleteAsync(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private List<string> CollectErrors() => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    }
}
