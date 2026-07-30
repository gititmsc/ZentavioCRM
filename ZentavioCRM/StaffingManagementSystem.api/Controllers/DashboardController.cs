using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Api.Extensions;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Dashboard;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Api.Controllers
{
    /// <summary>Aggregate counters for the landing Dashboard. Any authenticated user can view — the
    /// underlying counts are already scoped to what their permissions would let them see individually,
    /// but this summary endpoint does not re-check per-module view permissions (MVP scope). Counts ARE
    /// restricted by the caller's Leads/Customers/Opportunities Role.VisibilityScope (Own/Team/All), so
    /// they match what that user can actually open on those list screens.</summary>
    [ApiController]
    [Route("api/dashboard")]
    [Produces("application/json")]
    [Authorize]
    public sealed class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("sales-summary")]
        public async Task<IActionResult> GetSalesSummary()
        {
            var summary = await _dashboardService.GetSalesSummaryAsync(User.GetUserId());
            return Ok(ApiResponse<SalesDashboardSummaryDto>.SuccessResponse(summary));
        }
    }
}
