using ZentavioCRM.Core.DTOs.Dashboard;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="IDashboardService"/>
    public class DashboardService : IDashboardService
    {
        private static readonly OpportunityStage[] TerminalStages = [OpportunityStage.ClosedWon, OpportunityStage.ClosedLost];

        private readonly ILeadRepository _leadRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IOpportunityRepository _opportunityRepository;

        public DashboardService(
            ILeadRepository leadRepository,
            ICustomerRepository customerRepository,
            IOpportunityRepository opportunityRepository)
        {
            _leadRepository = leadRepository;
            _customerRepository = customerRepository;
            _opportunityRepository = opportunityRepository;
        }

        public async Task<SalesDashboardSummaryDto> GetSalesSummaryAsync()
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var nextMonthStart = monthStart.AddMonths(1);

            var openLeadsCount = await _leadRepository.CountOpenAsync();
            var convertedThisMonthCount = await _leadRepository.CountConvertedBetweenAsync(monthStart, nextMonthStart);

            var (_, activeCustomersCount) = await _customerRepository.SearchAsync(null, null, true, 1, 1);

            var opportunities = await _opportunityRepository.GetAllForDashboardAsync();

            var openOpportunities = opportunities.Where(o => !TerminalStages.Contains(o.Stage)).ToList();
            var pipelineValue = openOpportunities.Sum(o => o.Value ?? 0m);

            var closedWonCount = opportunities.Count(o => o.Stage == OpportunityStage.ClosedWon);
            var closedLostCount = opportunities.Count(o => o.Stage == OpportunityStage.ClosedLost);
            var closedCount = closedWonCount + closedLostCount;
            var winRate = closedCount == 0 ? 0m : Math.Round(closedWonCount * 100m / closedCount, 1);

            var stageBreakdown = opportunities
                .GroupBy(o => o.Stage)
                .Select(g => new StageBreakdownItem { Stage = g.Key, Count = g.Count(), Value = g.Sum(o => o.Value ?? 0m) })
                .OrderBy(s => s.Stage)
                .ToList();

            return new SalesDashboardSummaryDto
            {
                OpenLeadsCount = openLeadsCount,
                ActiveCustomersCount = activeCustomersCount,
                ConvertedLeadsThisMonthCount = convertedThisMonthCount,
                PipelineValue = pipelineValue,
                OpenOpportunitiesCount = openOpportunities.Count,
                WinRatePercentage = winRate,
                StageBreakdown = stageBreakdown,
            };
        }
    }
}
