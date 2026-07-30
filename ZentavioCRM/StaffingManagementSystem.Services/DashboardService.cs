using ZentavioCRM.Core.DTOs.Dashboard;
using ZentavioCRM.Core.Enums;
using ZentavioCRM.Core.Security;
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
        private readonly IAccessScopeService _accessScopeService;

        public DashboardService(
            ILeadRepository leadRepository,
            ICustomerRepository customerRepository,
            IOpportunityRepository opportunityRepository,
            IAccessScopeService accessScopeService)
        {
            _leadRepository = leadRepository;
            _customerRepository = customerRepository;
            _opportunityRepository = opportunityRepository;
            _accessScopeService = accessScopeService;
        }

        public async Task<SalesDashboardSummaryDto> GetSalesSummaryAsync(Guid? currentUserId = null)
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var nextMonthStart = monthStart.AddMonths(1);

            // Same scope every viewer already gets on the Leads/Customers/Opportunities list screens,
            // so an "Own"/"Team"-scoped user's totals here match what they can actually open there.
            AccessScope? accessScope = currentUserId is null ? null : await _accessScopeService.GetForUserAsync(currentUserId.Value);

            var openLeadsCount = await _leadRepository.CountOpenAsync(accessScope);
            var convertedThisMonthCount = await _leadRepository.CountConvertedBetweenAsync(monthStart, nextMonthStart, accessScope);

            var (_, activeCustomersCount) = await _customerRepository.SearchAsync(null, null, true, 1, 1, accessScope);

            var opportunities = await _opportunityRepository.GetAllForDashboardAsync(accessScope);

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
