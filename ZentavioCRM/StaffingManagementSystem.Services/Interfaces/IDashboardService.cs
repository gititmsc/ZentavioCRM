using ZentavioCRM.Core.DTOs.Dashboard;

namespace ZentavioCRM.Services.Interfaces
{
    public interface IDashboardService
    {
        /// <param name="currentUserId">When provided, the summary's counts are restricted to what this user's Role.VisibilityScope (Own/Team/All, plus any active delegations) allows them to see — matching what they could actually open in Leads/Customers/Opportunities. Null bypasses scoping entirely (system/internal callers only).</param>
        Task<SalesDashboardSummaryDto> GetSalesSummaryAsync(Guid? currentUserId = null);

        /// <summary>Date-range analytics (funnel, lead sources, win/loss + revenue trend, owner/territory leaderboards).</summary>
        /// <param name="fromDate">Start of the range (date part used, UTC). Defaults to 30 days before <paramref name="toDate"/>.</param>
        /// <param name="toDate">Inclusive end of the range (date part used, UTC). Defaults to today.</param>
        /// <param name="mineOnly">When true, restricts to records assigned to <paramref name="currentUserId"/> on top of the role's visibility scope ("My" vs "Team" view).</param>
        Task<DashboardAnalyticsDto> GetAnalyticsAsync(DateTime? fromDate, DateTime? toDate, bool mineOnly, Guid? currentUserId = null);
    }
}
