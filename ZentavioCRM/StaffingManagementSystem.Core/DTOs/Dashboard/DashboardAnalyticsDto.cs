using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.DTOs.Dashboard
{
    /// <summary>
    /// Date-range analytics for the Dashboard's chart sections. Every figure is already restricted by the
    /// caller's Role.VisibilityScope (and optionally narrowed further to "my records only").
    /// </summary>
    public class DashboardAnalyticsDto
    {
        public DateTime FromUtc { get; set; }

        /// <summary>Inclusive end of the range as requested (the date the user picked, not the exclusive upper bound used in queries).</summary>
        public DateTime ToUtc { get; set; }

        public bool MineOnly { get; set; }

        /// <summary>"week" or "month" — the bucket size used for <see cref="Trend"/>. Weekly for ranges up to 92 days, monthly beyond that.</summary>
        public string Granularity { get; set; } = "month";

        /// <summary>Leads created -> converted -> opportunities created -> won, all for records created inside the range.</summary>
        public IReadOnlyList<FunnelStep> Funnel { get; set; } = [];

        public IReadOnlyList<LeadSourceItem> LeadsBySource { get; set; } = [];

        /// <summary>One entry per bucket across the whole range (zero-filled), oldest first.</summary>
        public IReadOnlyList<TrendPoint> Trend { get; set; } = [];

        public decimal WonRevenueTotal { get; set; }

        public int WonCount { get; set; }

        public int LostCount { get; set; }

        public IReadOnlyList<OwnerLeaderboardItem> OwnerLeaderboard { get; set; } = [];

        public IReadOnlyList<TerritoryLeaderboardItem> TerritoryLeaderboard { get; set; } = [];
    }

    public class FunnelStep
    {
        public string Label { get; set; } = string.Empty;

        public int Count { get; set; }
    }

    public class LeadSourceItem
    {
        public LeadSource Source { get; set; }

        public int Count { get; set; }

        public int ConvertedCount { get; set; }
    }

    public class TrendPoint
    {
        /// <summary>First day (UTC) of the bucket — Monday for weekly buckets, the 1st for monthly.</summary>
        public DateTime BucketStartUtc { get; set; }

        public int WonCount { get; set; }

        public int LostCount { get; set; }

        public decimal WonRevenue { get; set; }
    }

    public class OwnerLeaderboardItem
    {
        public Guid? UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public int WonCount { get; set; }

        public int LostCount { get; set; }

        public decimal WonRevenue { get; set; }

        public int LeadsAssigned { get; set; }
    }

    public class TerritoryLeaderboardItem
    {
        public Guid? TerritoryId { get; set; }

        /// <summary>Territory name, or "No territory" for leads with none set.</summary>
        public string Name { get; set; } = string.Empty;

        public int LeadsCount { get; set; }

        public int ConvertedCount { get; set; }

        /// <summary>Converted / Leads, 0-100, one decimal.</summary>
        public decimal ConversionRate { get; set; }
    }

    /// <summary>Minimal lead projection for dashboard aggregation — avoids loading full entities.</summary>
    public class DashboardLeadRow
    {
        public LeadSource Source { get; set; }

        public LeadStatus Status { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public string? AssignedToFirstName { get; set; }

        public string? AssignedToLastName { get; set; }

        public Guid? TerritoryId { get; set; }

        public string? TerritoryName { get; set; }
    }

    /// <summary>Minimal opportunity projection for dashboard aggregation.</summary>
    public class DashboardOpportunityRow
    {
        public OpportunityStage Stage { get; set; }

        public decimal? Value { get; set; }

        public Guid? AssignedToUserId { get; set; }

        public string? AssignedToFirstName { get; set; }

        public string? AssignedToLastName { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? ClosedAtUtc { get; set; }
    }
}
