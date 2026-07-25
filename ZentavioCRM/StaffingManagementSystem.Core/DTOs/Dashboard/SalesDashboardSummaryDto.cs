using ZentavioCRM.Core.Enums;

namespace ZentavioCRM.Core.DTOs.Dashboard
{
    /// <summary>Aggregate counters for the landing Dashboard — SRS Phase 6, section 8 "Sales Dashboard", MVP scope.</summary>
    public class SalesDashboardSummaryDto
    {
        public int OpenLeadsCount { get; set; }

        public int ActiveCustomersCount { get; set; }

        public int ConvertedLeadsThisMonthCount { get; set; }

        /// <summary>Sum of Value across every open (not Closed Won/Lost) Opportunity.</summary>
        public decimal PipelineValue { get; set; }

        public int OpenOpportunitiesCount { get; set; }

        /// <summary>Closed Won / (Closed Won + Closed Lost) across all time, as a 0-100 percentage. 0 if nothing has closed yet.</summary>
        public decimal WinRatePercentage { get; set; }

        public IReadOnlyList<StageBreakdownItem> StageBreakdown { get; set; } = [];
    }

    public class StageBreakdownItem
    {
        public OpportunityStage Stage { get; set; }

        public int Count { get; set; }

        public decimal Value { get; set; }
    }
}
