using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Territories;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    [ApiController]
    [Route("api/territories")]
    [Produces("application/json")]
    [Authorize]
    public sealed class TerritoriesController : ControllerBase
    {
        private readonly ITerritoryService _territoryService;

        public TerritoriesController(ITerritoryService territoryService)
        {
            _territoryService = territoryService;
        }

        [HttpGet]
        [Authorize(Policy = PermissionCodes.TerritoriesView)]
        public async Task<IActionResult> GetAll()
        {
            var territories = await _territoryService.GetAllAsync();
            return Ok(ApiResponse<IReadOnlyList<TerritoryDto>>.SuccessResponse(territories));
        }

        [HttpGet("{id:guid}")]
        [Authorize(Policy = PermissionCodes.TerritoriesView)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _territoryService.GetByIdAsync(id);
            return result.Success ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [Authorize(Policy = PermissionCodes.TerritoriesManage)]
        public async Task<IActionResult> Create([FromBody] SaveTerritoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<TerritoryDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _territoryService.CreateAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionCodes.TerritoriesManage)]
        public async Task<IActionResult> Update(Guid id, [FromBody] SaveTerritoryRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<TerritoryDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _territoryService.UpdateAsync(id, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionCodes.TerritoriesManage)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _territoryService.DeleteAsync(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private List<string> CollectErrors() => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    }
}
