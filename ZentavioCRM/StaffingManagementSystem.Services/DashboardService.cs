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

        /// <summary>Ranges up to this many days bucket weekly; longer ranges bucket monthly.</summary>
        private const int WeeklyBucketMaxDays = 92;

        /// <summary>Hard cap so a typo'd range can't force thousands of empty buckets.</summary>
        private const int MaxRangeDays = 800;

        public async Task<DashboardAnalyticsDto> GetAnalyticsAsync(DateTime? fromDate, DateTime? toDate, bool mineOnly, Guid? currentUserId = null)
        {
            var to = (toDate ?? DateTime.UtcNow).Date;
            var from = (fromDate ?? to.AddDays(-30)).Date;
            if (from > to)
            {
                (from, to) = (to, from);
            }

            if ((to - from).TotalDays > MaxRangeDays)
            {
                from = to.AddDays(-MaxRangeDays);
            }

            var fromUtc = DateTime.SpecifyKind(from, DateTimeKind.Utc);
            var toExclusiveUtc = DateTime.SpecifyKind(to.AddDays(1), DateTimeKind.Utc);

            AccessScope? accessScope = currentUserId is null ? null : await _accessScopeService.GetForUserAsync(currentUserId.Value);
            Guid? mineUserId = mineOnly ? currentUserId : null;

            var leads = await _leadRepository.GetForDashboardAsync(fromUtc, toExclusiveUtc, mineUserId, accessScope);
            var opportunityRows = await _opportunityRepository.GetForDashboardAsync(fromUtc, toExclusiveUtc, mineUserId, accessScope);

            bool InRange(DateTime? d) => d is not null && d >= fromUtc && d < toExclusiveUtc;

            var createdOpportunities = opportunityRows.Where(o => InRange(o.CreatedAtUtc)).ToList();
            var closedOpportunities = opportunityRows
                .Where(o => TerminalStages.Contains(o.Stage) && InRange(o.ClosedAtUtc))
                .ToList();
            var wonClosed = closedOpportunities.Where(o => o.Stage == OpportunityStage.ClosedWon).ToList();
            var lostClosed = closedOpportunities.Where(o => o.Stage == OpportunityStage.ClosedLost).ToList();

            var convertedLeads = leads.Count(l => l.Status == LeadStatus.Converted);

            var funnel = new List<FunnelStep>
            {
                new() { Label = "Leads created", Count = leads.Count },
                new() { Label = "Leads converted", Count = convertedLeads },
                new() { Label = "Opportunities created", Count = createdOpportunities.Count },
                new() { Label = "Opportunities won", Count = createdOpportunities.Count(o => o.Stage == OpportunityStage.ClosedWon) },
            };

            var leadsBySource = leads
                .GroupBy(l => l.Source)
                .Select(g => new LeadSourceItem
                {
                    Source = g.Key,
                    Count = g.Count(),
                    ConvertedCount = g.Count(l => l.Status == LeadStatus.Converted),
                })
                .OrderByDescending(s => s.Count)
                .ToList();

            var rangeDays = (to - from).TotalDays + 1;
            var weekly = rangeDays <= WeeklyBucketMaxDays;
            var trend = BuildTrend(from, to, weekly, wonClosed, lostClosed);

            return new DashboardAnalyticsDto
            {
                FromUtc = fromUtc,
                ToUtc = DateTime.SpecifyKind(to, DateTimeKind.Utc),
                MineOnly = mineOnly,
                Granularity = weekly ? "week" : "month",
                Funnel = funnel,
                LeadsBySource = leadsBySource,
                Trend = trend,
                WonRevenueTotal = wonClosed.Sum(o => o.Value ?? 0m),
                WonCount = wonClosed.Count,
                LostCount = lostClosed.Count,
                OwnerLeaderboard = BuildOwnerLeaderboard(leads, wonClosed, lostClosed),
                TerritoryLeaderboard = BuildTerritoryLeaderboard(leads),
            };
        }

        private static DateTime BucketStart(DateTime date, bool weekly)
        {
            if (!weekly)
            {
                return new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            }

            // Monday-start weeks.
            var daysSinceMonday = ((int)date.DayOfWeek + 6) % 7;
            return DateTime.SpecifyKind(date.Date.AddDays(-daysSinceMonday), DateTimeKind.Utc);
        }

        private static List<TrendPoint> BuildTrend(
            DateTime from, DateTime to, bool weekly,
            List<DashboardOpportunityRow> won, List<DashboardOpportunityRow> lost)
        {
            var points = new SortedDictionary<DateTime, TrendPoint>();

            // Zero-fill every bucket the range touches so charts show gaps as zero rather than skipping them.
            for (var cursor = BucketStart(from, weekly); cursor <= to; cursor = weekly ? cursor.AddDays(7) : cursor.AddMonths(1))
            {
                points[cursor] = new TrendPoint { BucketStartUtc = cursor };
            }

            foreach (var o in won)
            {
                var key = BucketStart(o.ClosedAtUtc!.Value, weekly);
                if (points.TryGetValue(key, out var p))
                {
                    p.WonCount++;
                    p.WonRevenue += o.Value ?? 0m;
                }
            }

            foreach (var o in lost)
            {
                var key = BucketStart(o.ClosedAtUtc!.Value, weekly);
                if (points.TryGetValue(key, out var p))
                {
                    p.LostCount++;
                }
            }

            return points.Values.ToList();
        }

        private const int LeaderboardSize = 10;

        private static string OwnerName(string? first, string? last)
        {
            var name = $"{first} {last}".Trim();
            return name.Length == 0 ? "Unassigned" : name;
        }

        private static List<OwnerLeaderboardItem> BuildOwnerLeaderboard(
            IReadOnlyList<DashboardLeadRow> leads,
            List<DashboardOpportunityRow> won, List<DashboardOpportunityRow> lost)
        {
            // Keyed on Guid (Guid.Empty = unassigned): Dictionary throws on a null key, even for Guid?.
            var owners = new Dictionary<Guid, OwnerLeaderboardItem>();

            OwnerLeaderboardItem Get(Guid? userId, string? first, string? last)
            {
                var key = userId ?? Guid.Empty;
                if (!owners.TryGetValue(key, out var item))
                {
                    item = new OwnerLeaderboardItem { UserId = userId, Name = OwnerName(first, last) };
                    owners[key] = item;
                }

                return item;
            }

            foreach (var o in won)
            {
                var item = Get(o.AssignedToUserId, o.AssignedToFirstName, o.AssignedToLastName);
                item.WonCount++;
                item.WonRevenue += o.Value ?? 0m;
            }

            foreach (var o in lost)
            {
                Get(o.AssignedToUserId, o.AssignedToFirstName, o.AssignedToLastName).LostCount++;
            }

            foreach (var l in leads)
            {
                Get(l.AssignedToUserId, l.AssignedToFirstName, l.AssignedToLastName).LeadsAssigned++;
            }

            return owners.Values
                .OrderByDescending(o => o.WonRevenue)
                .ThenByDescending(o => o.WonCount)
                .ThenByDescending(o => o.LeadsAssigned)
                .Take(LeaderboardSize)
                .ToList();
        }

        private static List<TerritoryLeaderboardItem> BuildTerritoryLeaderboard(IReadOnlyList<DashboardLeadRow> leads)
            => leads
                .GroupBy(l => l.TerritoryId)
                .Select(g =>
                {
                    var total = g.Count();
                    var converted = g.Count(l => l.Status == LeadStatus.Converted);
                    return new TerritoryLeaderboardItem
                    {
                        TerritoryId = g.Key,
                        Name = g.Key is null ? "No territory" : (g.First().TerritoryName ?? "Unknown territory"),
                        LeadsCount = total,
                        ConvertedCount = converted,
                        ConversionRate = total == 0 ? 0m : Math.Round(converted * 100m / total, 1),
                    };
                })
                .OrderByDescending(t => t.LeadsCount)
                .Take(LeaderboardSize)
                .ToList();
    }
}
