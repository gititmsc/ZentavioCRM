using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZentavioCRM.Api.Extensions;
using ZentavioCRM.Core.Common;
using ZentavioCRM.Core.DTOs.Dashboard;
using ZentavioCRM.Core.DTOs.Usage;
using ZentavioCRM.Core.Interfaces;
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
        private readonly IUsageLimitService _usageLimitService;

        public DashboardController(IDashboardService dashboardService, IUsageLimitService usageLimitService)
        {
            _dashboardService = dashboardService;
            _usageLimitService = usageLimitService;
        }

        [HttpGet("sales-summary")]
        public async Task<IActionResult> GetSalesSummary()
        {
            var summary = await _dashboardService.GetSalesSummaryAsync(User.GetUserId());
            return Ok(ApiResponse<SalesDashboardSummaryDto>.SuccessResponse(summary));
        }

        /// <summary>Self-service usage vs. plan limits for the CURRENT tenant — powers the in-app
        /// warning banner as a tenant approaches its plan's user/record/storage limits. Any
        /// authenticated user can view their own tenant's usage; there's nothing sensitive here
        /// that needs a higher permission than "is logged in".</summary>
        [HttpGet("usage")]
        public async Task<IActionResult> GetUsage()
        {
            var usage = await _usageLimitService.GetUsageAsync();
            return usage is null
                ? Ok(ApiResponse<UsageSummaryDto>.FailureResponse("Usage information is not available for this session."))
                : Ok(ApiResponse<UsageSummaryDto>.SuccessResponse(usage));
        }
    }
}
