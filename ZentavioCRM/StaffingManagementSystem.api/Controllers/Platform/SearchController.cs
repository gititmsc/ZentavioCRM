using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Api.Authorization;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Platform;
using ZentavioCRM.Core.Interfaces;

namespace ZentavioCRM.Api.Controllers.Platform
{
    /// <summary>
    /// Backs the Platform Admin console's topbar search box — read-only, so available to both
    /// PlatformAdmin roles (SuperAdmin and Support), unlike most other controllers under
    /// /api/platform.
    /// </summary>
    [ApiController]
    [Route("api/platform/search")]
    [Produces("application/json")]
    [Authorize(Policy = PlatformAuthorizationPolicies.PlatformAdmin)]
    public sealed class SearchController : ControllerBase
    {
        private readonly IGlobalSearchService _searchService;

        public SearchController(IGlobalSearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] string? q)
        {
            var results = await _searchService.SearchAsync(q ?? string.Empty);
            return Ok(ApiResponse<IReadOnlyList<GlobalSearchResultDto>>.SuccessResponse(results));
        }
    }
}
