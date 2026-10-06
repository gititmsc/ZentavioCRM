using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Api.Extensions;
using ZentavioCRM.Core.Analytics;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Analytics;
using ZentavioCRM.Core.Entities;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    /// <summary>
    /// Custom dashboards and the report builder: a whitelisted query engine plus saved dashboard/report storage.
    /// Any authenticated user may use it, but each query additionally requires the queried module's own View
    /// permission and is restricted to the caller's Role.VisibilityScope — this endpoint can never show more than
    /// the module's list screen would.
    /// </summary>
    [ApiController]
    [Route("api/analytics")]
    [Produces("application/json")]
    [Authorize]
    public sealed class AnalyticsController : ControllerBase
    {
        private const string XlsxContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        private readonly IAnalyticsService _analyticsService;
        private readonly ISavedAnalyticsService _savedAnalyticsService;

        public AnalyticsController(IAnalyticsService analyticsService, ISavedAnalyticsService savedAnalyticsService)
        {
            _analyticsService = analyticsService;
            _savedAnalyticsService = savedAnalyticsService;
        }

        // ---------------------------------------------------------------- engine

        [HttpGet("catalog")]
        public IActionResult GetCatalog()
        {
            var catalog = _analyticsService.GetCatalog(AllowedEntities());
            return Ok(ApiResponse<IReadOnlyList<AnalyticsCatalogEntityDto>>.SuccessResponse(catalog));
        }

        [HttpPost("query")]
        public async Task<IActionResult> Query([FromBody] AnalyticsQueryRequest request)
        {
            return await RunAsync(request, userId => _analyticsService.QueryAsync(request, userId));
        }

        [HttpPost("rows")]
        public async Task<IActionResult> Rows([FromBody] AnalyticsRowsRequest request)
        {
            return await RunAsync(request, userId => _analyticsService.RowsAsync(request, userId));
        }

        /// <summary>Runs the same row query as <c>rows</c> (up to the maximum row count) and returns it as a download. <c>format</c> is "csv" (default) or "xlsx".</summary>
        [HttpPost("export")]
        public async Task<IActionResult> Export([FromBody] AnalyticsRowsRequest request, [FromQuery] string format = "csv")
        {
            var denied = CheckAccess(request, out var userId);
            if (denied is not null)
            {
                return denied;
            }

            var baseName = $"report-{request.Entity.ToString().ToLowerInvariant()}-{DateTime.UtcNow:yyyyMMdd}";

            try
            {
                if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
                {
                    var bytes = await _analyticsService.ExportXlsxAsync(request, userId);
                    return File(bytes, XlsxContentType, baseName + ".xlsx");
                }

                var csv = await _analyticsService.ExportCsvAsync(request, userId);
                // UTF-8 BOM so Excel opens non-ASCII names correctly.
                var content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
                return File(content, "text/csv", baseName + ".csv");
            }
            catch (AnalyticsQueryException ex)
            {
                return BadRequest(ApiResponse<bool>.FailureResponse(ex.Message));
            }
        }

        // ---------------------------------------------------------------- saved dashboards / reports

        [HttpGet("saved")]
        public async Task<IActionResult> ListSaved([FromQuery] SavedAnalyticsKind kind)
        {
            if (User.GetUserId() is not { } userId)
            {
                return Unauthorized();
            }

            var items = await _savedAnalyticsService.ListAsync(kind, userId, CanManageShared());
            return Ok(ApiResponse<IReadOnlyList<SavedAnalyticsItemDto>>.SuccessResponse(items));
        }

        [HttpGet("saved/{id:guid}")]
        public async Task<IActionResult> GetSaved(Guid id)
        {
            if (User.GetUserId() is not { } userId)
            {
                return Unauthorized();
            }

            var item = await _savedAnalyticsService.GetAsync(id, userId, CanManageShared());
            return item is null
                ? NotFound(ApiResponse<SavedAnalyticsItemDto>.FailureResponse("Item not found."))
                : Ok(ApiResponse<SavedAnalyticsItemDto>.SuccessResponse(item));
        }

        [HttpPost("saved")]
        public async Task<IActionResult> CreateSaved([FromBody] SaveAnalyticsItemRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<SavedAnalyticsItemDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            if (User.GetUserId() is not { } userId)
            {
                return Unauthorized();
            }

            var result = await _savedAnalyticsService.CreateAsync(request, userId, CanManageShared());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpPut("saved/{id:guid}")]
        public async Task<IActionResult> UpdateSaved(Guid id, [FromBody] SaveAnalyticsItemRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<SavedAnalyticsItemDto>.FailureResponse("Validation failed.", CollectErrors()));
            }

            if (User.GetUserId() is not { } userId)
            {
                return Unauthorized();
            }

            var result = await _savedAnalyticsService.UpdateAsync(id, request, userId, CanManageShared());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("saved/{id:guid}")]
        public async Task<IActionResult> DeleteSaved(Guid id)
        {
            if (User.GetUserId() is not { } userId)
            {
                return Unauthorized();
            }

            var result = await _savedAnalyticsService.DeleteAsync(id, userId, CanManageShared());
            return result.Success ? Ok(result) : BadRequest(result);
        }

        // ---------------------------------------------------------------- helpers

        private bool CanManageShared() => User.HasClaim(PermissionCodes.ClaimType, PermissionCodes.AnalyticsManageShared);

        /// <summary>Entities whose module View permission the caller holds (permissions travel as JWT claims).</summary>
        private HashSet<AnalyticsEntity> AllowedEntities()
            => AnalyticsCatalog.All
                .Where(d => User.HasClaim(PermissionCodes.ClaimType, d.ViewPermission))
                .Select(d => d.Entity)
                .ToHashSet();

        /// <summary>Returns a non-null result (401/400/403) when the request must not proceed; otherwise null with the caller's id.</summary>
        private IActionResult? CheckAccess(AnalyticsBaseRequest request, out Guid userId)
        {
            userId = Guid.Empty;

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<bool>.FailureResponse("Validation failed.", CollectErrors()));
            }

            if (User.GetUserId() is not { } id)
            {
                return Unauthorized();
            }

            userId = id;

            if (!Enum.IsDefined(request.Entity))
            {
                return BadRequest(ApiResponse<bool>.FailureResponse("Unknown entity."));
            }

            if (!AllowedEntities().Contains(request.Entity))
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponse<bool>.FailureResponse($"You don't have access to {AnalyticsCatalog.Get(request.Entity).Label} data."));
            }

            return null;
        }

        private async Task<IActionResult> RunAsync<T>(AnalyticsBaseRequest request, Func<Guid, Task<T>> run)
        {
            var denied = CheckAccess(request, out var userId);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                return Ok(ApiResponse<T>.SuccessResponse(await run(userId)));
            }
            catch (AnalyticsQueryException ex)
            {
                return BadRequest(ApiResponse<T>.FailureResponse(ex.Message));
            }
        }

        private List<string> CollectErrors() => ModelState.Values
            .SelectMany(v => v.Errors)
            .Select(e => e.ErrorMessage)
            .ToList();
    }
}
