using System.ComponentModel.DataAnnotations;
using ZentavioCRM.Core.Analytics;

namespace ZentavioCRM.Core.DTOs.Analytics
{
    public enum AnalyticsMetric
    {
        Count = 1,
        Sum = 2,
        Average = 3,
    }

    public class AnalyticsFilter
    {
        [Required]
        public string Field { get; set; } = string.Empty;

        /// <summary>"eq", "neq", "in" (text fields) or "gte", "lte" (number/money fields).</summary>
        [Required]
        public string Op { get; set; } = "eq";

        public List<string> Values { get; set; } = [];
    }

    /// <summary>Shared by the aggregate and row queries: what to look at, over which dates, narrowed how.</summary>
    public abstract class AnalyticsBaseRequest
    {
        public AnalyticsEntity Entity { get; set; }

        /// <summary>Which of the entity's date fields <see cref="From"/>/<see cref="To"/> apply to. Defaults to "createdAt".</summary>
        public string? DateField { get; set; }

        /// <summary>Inclusive start day (UTC date part). Null = unbounded.</summary>
        public DateTime? From { get; set; }

        /// <summary>Inclusive end day (UTC date part). Null = unbounded.</summary>
        public DateTime? To { get; set; }

        /// <summary>Restrict to records assigned to the caller, on top of their visibility scope. Can only narrow.</summary>
        public bool MineOnly { get; set; }

        public List<AnalyticsFilter> Filters { get; set; } = [];
    }

    /// <summary>Group-and-aggregate query — powers dashboard widgets and report summaries.</summary>
    public class AnalyticsQueryRequest : AnalyticsBaseRequest
    {
        public AnalyticsMetric Metric { get; set; } = AnalyticsMetric.Count;

        /// <summary>Required for Sum/Average: a Number/Money field key from the catalog.</summary>
        public string? MetricField { get; set; }

        /// <summary>A groupable text field key ("owner"), or a date field with a bucket ("createdAt:month"; day|week|month|year). Null = one overall total.</summary>
        public string? GroupBy { get; set; }

        /// <summary>Max groups returned for category groupings (1-100). Time series are never truncated.</summary>
        public int Limit { get; set; } = 20;

        /// <summary>Category groupings only: largest value first when true.</summary>
        public bool SortDescending { get; set; } = true;
    }

    public class AnalyticsRowsRequest : AnalyticsBaseRequest
    {
        /// <summary>Catalog field keys to return, in display order. Empty = every field of the entity.</summary>
        public List<string> Columns { get; set; } = [];

        public string? SortBy { get; set; }

        public bool SortDescending { get; set; } = true;

        /// <summary>Max rows (1-5000).</summary>
        public int Limit { get; set; } = 500;
    }

    public class AnalyticsQueryRow
    {
        /// <summary>Stable identifier of the group (the raw value, or the ISO bucket-start date for time series).</summary>
        public string Key { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public decimal Value { get; set; }

        /// <summary>Records in the group regardless of metric.</summary>
        public int Count { get; set; }
    }

    public class AnalyticsQueryResult
    {
        public IReadOnlyList<AnalyticsQueryRow> Rows { get; set; } = [];

        /// <summary>The metric across every record that matched the filters (not just the returned groups).</summary>
        public decimal Total { get; set; }

        public int TotalCount { get; set; }

        /// <summary>"count", "money" or "number" — how the UI should format <see cref="AnalyticsQueryRow.Value"/>.</summary>
        public string ValueKind { get; set; } = "count";

        /// <summary>"none", "category" or "time".</summary>
        public string GroupKind { get; set; } = "none";

        /// <summary>For time groupings: "day" | "week" | "month" | "year".</summary>
        public string? Bucket { get; set; }

        public bool Truncated { get; set; }
    }

    public class AnalyticsColumnDto
    {
        public string Key { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public AnalyticsFieldKind Kind { get; set; }
    }

    public class AnalyticsRowsResult
    {
        public IReadOnlyList<AnalyticsColumnDto> Columns { get; set; } = [];

        public IReadOnlyList<Dictionary<string, object?>> Rows { get; set; } = [];

        /// <summary>Matching records before the row limit was applied.</summary>
        public int TotalCount { get; set; }

        public bool Truncated { get; set; }
    }

    public class AnalyticsCatalogFieldDto
    {
        public string Key { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public AnalyticsFieldKind Kind { get; set; }

        public bool Groupable { get; set; }
    }

    public class AnalyticsCatalogEntityDto
    {
        public AnalyticsEntity Entity { get; set; }

        public string Label { get; set; } = string.Empty;

        public IReadOnlyList<AnalyticsCatalogFieldDto> Fields { get; set; } = [];
    }

    /// <summary>Thrown for a malformed or non-whitelisted query; the controller maps it to a 400.</summary>
    public sealed class AnalyticsQueryException : Exception
    {
        public AnalyticsQueryException(string message) : base(message)
        {
        }
    }
}
