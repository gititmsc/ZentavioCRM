using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Tags;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    [ApiController]
    [Route("api/tags")]
    [Produces("application/json")]
    [Authorize]
    public sealed class TagsController : ControllerBase
    {
        private readonly ITagService _tagService;

        public TagsController(ITagService tagService)
        {
            _tagService = tagService;
        }

        /// <summary>Every tag, name-sorted — powers the tag picker on the Lead/Customer forms.</summary>
        [HttpGet]
        [Authorize(Policy = PermissionCodes.TagsView)]
        public async Task<IActionResult> GetAll()
        {
            var tags = await _tagService.GetAllAsync();
            return Ok(ApiResponse<IReadOnlyList<TagDto>>.SuccessResponse(tags));
        }

        /// <summary>Every tag plus how many Leads/Customers carry it — powers the Tag Manager screen.</summary>
        [HttpGet("with-usage")]
        [Authorize(Policy = PermissionCodes.TagsManage)]
        public async Task<IActionResult> GetAllWithUsage()
        {
            var tags = await _tagService.GetAllWithUsageAsync();
            return Ok(ApiResponse<IReadOnlyList<TagWithUsageDto>>.SuccessResponse(tags));
        }

        [HttpPost]
        [Authorize(Policy = PermissionCodes.TagsManage)]
        public async Task<IActionResult> Create([FromBody] SaveTagRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<TagDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _tagService.CreateAsync(request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize(Policy = PermissionCodes.TagsManage)]
        public async Task<IActionResult> Update(Guid id, [FromBody] SaveTagRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<TagDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            var result = await _tagService.UpdateAsync(id, request);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize(Policy = PermissionCodes.TagsManage)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _tagService.DeleteAsync(id);
            return result.Success ? Ok(result) : BadRequest(result);
        }

        private List<string> CollectErrors() => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    }
}
