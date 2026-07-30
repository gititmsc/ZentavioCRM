using ZentavioCRM.Core.DTOs.Dashboard;

namespace ZentavioCRM.Services.Interfaces
{
    public interface IDashboardService
    {
        /// <param name="currentUserId">When provided, the summary's counts are restricted to what this user's Role.VisibilityScope (Own/Team/All, plus any active delegations) allows them to see — matching what they could actually open in Leads/Customers/Opportunities. Null bypasses scoping entirely (system/internal callers only).</param>
        Task<SalesDashboardSummaryDto> GetSalesSummaryAsync(Guid? currentUserId = null);
    }
}
