using ZentavioCRM.Core.DTOs.Dashboard;

namespace ZentavioCRM.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<SalesDashboardSummaryDto> GetSalesSummaryAsync();
    }
}
