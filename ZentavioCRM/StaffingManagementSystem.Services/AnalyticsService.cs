using System.Globalization;
using System.Text;
using ZentavioCRM.Core.Analytics;
using ZentavioCRM.Core.DTOs.Analytics;
using ZentavioCRM.Core.Security;
using ZentavioCRM.Repositories.Interfaces;
using ZentavioCRM.Services.Common;
using ZentavioCRM.Services.Interfaces;

namespace ZentavioCRM.Services
{
    /// <inheritdoc cref="IAnalyticsService"/>
    public class AnalyticsService : IAnalyticsService
    {
        private const int MaxTimeBuckets = 400;
        private const int MaxRows = 5000;
        private const int MaxGroups = 100;
        private const int MinYear = 1900;
        private const int MaxYear = 2100;

        private static readonly string[] TimeBuckets = ["day", "week", "month", "year"];

        private readonly ILeadRepository _leadRepository;
        private readonly IOpportunityRepository _opportunityRepository;
        private readonly ICustomerRepository _customerRepository;
        private readonly IQuotationRepository _quotationRepository;
        private readonly ISalesOrderRepository _salesOrderRepository;
        private readonly IAccessScopeService _accessScopeService;

        public AnalyticsService(
            ILeadRepository leadRepository,
            IOpportunityRepository opportunityRepository,
            ICustomerRepository customerRepository,
            IQuotationRepository quotationRepository,
            ISalesOrderRepository salesOrderRepository,
            IAccessScopeService accessScopeService)
        {
            _leadRepository = leadRepository;
            _opportunityRepository = opportunityRepository;
            _customerRepository = customerRepository;
            _quotationRepository = quotationRepository;
            _salesOrderRepository = salesOrderRepository;
            _accessScopeService = accessScopeService;
        }

        public IReadOnlyList<AnalyticsCatalogEntityDto> GetCatalog(IReadOnlySet<AnalyticsEntity> allowedEntities)
            => AnalyticsCatalog.All
                .Where(d => allowedEntities.Contains(d.Entity))
                .Select(d => new AnalyticsCatalogEntityDto
                {
                    Entity = d.Entity,
                    Label = d.Label,
                    Fields = d.Fields
                        .Select(f => new AnalyticsCatalogFieldDto { Key = f.Key, Label = f.Label, Kind = f.Kind, Groupable = f.Groupable })
                        .ToList(),
                })
                .ToList();

        // ------------------------------------------------------------------ aggregate query

        public async Task<AnalyticsQueryResult> QueryAsync(AnalyticsQueryRequest request, Guid userId)
        {
            var def = ResolveDefinition(request.Entity);
            var matched = ApplyFilters(await LoadAsync(request, userId), request, def);

            AnalyticsField? measure = null;
            if (request.Metric != AnalyticsMetric.Count)
            {
                measure = def.Find(request.MetricField ?? string.Empty);
                if (measure is null || measure.Kind is not (AnalyticsFieldKind.Number or AnalyticsFieldKind.Money))
                {
                    throw new AnalyticsQueryException($"Sum/Average needs a numeric field; '{request.MetricField}' is not one for {def.Label}.");
                }
            }

            decimal Compute(IReadOnlyCollection<AnalyticsRecord> group)
            {
                switch (request.Metric)
                {
                    case AnalyticsMetric.Sum:
                        return group.Sum(r => (r.Get(measure!.Key) as decimal?) ?? 0m);
                    case AnalyticsMetric.Average:
                        var values = group.Select(r => r.Get(measure!.Key) as decimal?).Where(v => v is not null).Select(v => v!.Value).ToList();
                        return values.Count == 0 ? 0m : Math.Round(values.Average(), 2);
                    default:
                        return group.Count;
                }
            }

            var result = new AnalyticsQueryResult
            {
                Total = Compute(matched),
                TotalCount = matched.Count,
                ValueKind = measure is null ? "count" : (measure.Kind == AnalyticsFieldKind.Money ? "money" : "number"),
            };

            if (string.IsNullOrWhiteSpace(request.GroupBy))
            {
                result.GroupKind = "none";
                result.Rows = [new AnalyticsQueryRow { Key = "total", Label = "Total", Value = result.Total, Count = matched.Count }];
                return result;
            }

            var parts = request.GroupBy.Split(':', 2);
            var groupField = def.Find(parts[0].Trim())
                ?? throw new AnalyticsQueryException($"'{parts[0]}' is not a field of {def.Label}.");

            if (groupField.Kind == AnalyticsFieldKind.Date)
            {
                var bucket = (parts.Length > 1 ? parts[1] : "month").Trim().ToLowerInvariant();
                if (!TimeBuckets.Contains(bucket))
                {
                    throw new AnalyticsQueryException($"Unknown time bucket '{bucket}'. Use day, week, month or year.");
                }

                // The requested From/To only describe the bucket range when the grouped date IS the date the range
                // filtered on; otherwise records can fall outside it, so derive the range from the data instead.
                var filterDateKey = def.Find(string.IsNullOrWhiteSpace(request.DateField) ? "createdAt" : request.DateField)?.Key;
                var rangeMatchesGrouping = string.Equals(filterDateKey, groupField.Key, StringComparison.OrdinalIgnoreCase);

                var (series, seriesTruncated) = BuildTimeSeries(
                    matched, groupField.Key, bucket,
                    rangeMatchesGrouping ? request.From : null,
                    rangeMatchesGrouping ? request.To : null,
                    Compute);

                result.GroupKind = "time";
                result.Bucket = bucket;
                result.Rows = series;
                result.Truncated = seriesTruncated;
                return result;
            }

            if (groupField.Kind != AnalyticsFieldKind.Text || !groupField.Groupable)
            {
                throw new AnalyticsQueryException($"'{groupField.Label}' can't be used to group {def.Label}.");
            }

            var limit = Math.Clamp(request.Limit, 1, MaxGroups);
            var groups = matched
                .GroupBy(r => r.Get(groupField.Key) as string ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var members = g.ToList();
                    return new AnalyticsQueryRow
                    {
                        Key = g.Key,
                        Label = g.Key.Length == 0 ? "(none)" : g.Key,
                        Value = Compute(members),
                        Count = members.Count,
                    };
                });

            var ordered = (request.SortDescending ? groups.OrderByDescending(g => g.Value) : groups.OrderBy(g => g.Value))
                .ThenBy(g => g.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();

            result.GroupKind = "category";
            result.Truncated = ordered.Count > limit;
            result.Rows = ordered.Take(limit).ToList();
            return result;
        }

        /// <summary>Hard stop for the bucket-enumeration loop, independent of MaxTimeBuckets, so pathological data (e.g. year-1 dates grouped by day) can't spin.</summary>
        private const int MaxBucketEnumeration = 20_000;

        private static (List<AnalyticsQueryRow> Rows, bool Truncated) BuildTimeSeries(
            List<AnalyticsRecord> matched, string dateKey, string bucket,
            DateTime? from, DateTime? to, Func<IReadOnlyCollection<AnalyticsRecord>, decimal> compute)
        {
            var byBucket = matched
                .Select(r => new { Record = r, Date = r.Get(dateKey) as DateTime? })
                .Where(x => x.Date is not null)
                .GroupBy(x => BucketStart(x.Date!.Value, bucket))
                .ToDictionary(g => g.Key, g => g.Select(x => x.Record).ToList());

            // Zero-fill across the requested range when both ends are given (an explicit range that's too wide is an
            // error the user can fix), otherwise across the data actually present (an over-long span is clamped to the
            // most recent buckets instead — a default "all time by day" widget should still render).
            var explicitRange = from is not null && to is not null;
            DateTime first;
            DateTime last;
            if (explicitRange)
            {
                first = BucketStart(from!.Value.Date, bucket);
                last = to!.Value.Date;
            }
            else if (byBucket.Count > 0)
            {
                first = byBucket.Keys.Min();
                last = byBucket.Keys.Max();
            }
            else
            {
                return ([], false);
            }

            var cursors = new List<DateTime>();
            for (var cursor = first; cursor <= last; cursor = Advance(cursor, bucket))
            {
                if (cursors.Count >= MaxBucketEnumeration)
                {
                    throw new AnalyticsQueryException($"That range spans too many {bucket} buckets. Pick a coarser bucket or a shorter range.");
                }

                cursors.Add(cursor);
            }

            var truncated = false;
            if (cursors.Count > MaxTimeBuckets)
            {
                if (explicitRange)
                {
                    throw new AnalyticsQueryException($"That range has too many {bucket} buckets (max {MaxTimeBuckets}). Pick a coarser bucket or a shorter range.");
                }

                cursors = cursors.TakeLast(MaxTimeBuckets).ToList();
                truncated = true;
            }

            var rows = cursors.Select(cursor =>
            {
                var members = byBucket.TryGetValue(cursor, out var found) ? found : [];
                return new AnalyticsQueryRow
                {
                    Key = cursor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    Label = BucketLabel(cursor, bucket),
                    Value = members.Count == 0 ? 0m : compute(members),
                    Count = members.Count,
                };
            }).ToList();

            return (rows, truncated);
        }

        private static DateTime BucketStart(DateTime date, string bucket) => bucket switch
        {
            "day" => DateTime.SpecifyKind(date.Date, DateTimeKind.Utc),
            "week" => DateTime.SpecifyKind(date.Date.AddDays(-(((int)date.DayOfWeek + 6) % 7)), DateTimeKind.Utc), // Monday-start
            "month" => new DateTime(date.Year, date.Month, 1, 0, 0, 0, DateTimeKind.Utc),
            _ => new DateTime(date.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };

        private static DateTime Advance(DateTime cursor, string bucket)
        {
            try
            {
                return bucket switch
                {
                    "day" => cursor.AddDays(1),
                    "week" => cursor.AddDays(7),
                    "month" => cursor.AddMonths(1),
                    _ => cursor.AddYears(1),
                };
            }
            catch (ArgumentOutOfRangeException)
            {
                // Past DateTime.MaxValue: step to MaxValue so the caller's loop condition/enumeration cap ends it.
                return DateTime.MaxValue;
            }
        }

        private static string BucketLabel(DateTime start, string bucket) => bucket switch
        {
            "day" or "week" => start.ToString("d MMM yyyy", CultureInfo.InvariantCulture),
            "month" => start.ToString("MMM yyyy", CultureInfo.InvariantCulture),
            _ => start.ToString("yyyy", CultureInfo.InvariantCulture),
        };

        // ------------------------------------------------------------------ row query + export

        public async Task<AnalyticsRowsResult> RowsAsync(AnalyticsRowsRequest request, Guid userId)
        {
            var def = ResolveDefinition(request.Entity);
            var matched = ApplyFilters(await LoadAsync(request, userId), request, def);

            var columns = new List<AnalyticsField>();
            if (request.Columns.Count == 0)
            {
                columns.AddRange(def.Fields);
            }
            else
            {
                foreach (var key in request.Columns)
                {
                    var field = def.Find(key) ?? throw new AnalyticsQueryException($"'{key}' is not a column of {def.Label}.");
                    if (!columns.Contains(field))
                    {
                        columns.Add(field);
                    }
                }
            }

            var sortField = def.Find(string.IsNullOrWhiteSpace(request.SortBy) ? "createdAt" : request.SortBy)
                ?? throw new AnalyticsQueryException($"'{request.SortBy}' is not a field of {def.Label}.");

            var withValue = matched.Where(r => r.Get(sortField.Key) is not null);
            var withoutValue = matched.Where(r => r.Get(sortField.Key) is null);
            var sorted = (request.SortDescending
                    ? withValue.OrderByDescending(r => r.Get(sortField.Key), ValueComparer.Instance)
                    : withValue.OrderBy(r => r.Get(sortField.Key), ValueComparer.Instance))
                .Concat(withoutValue)   // blanks always last, whichever direction
                .ToList();

            var limit = Math.Clamp(request.Limit, 1, MaxRows);

            return new AnalyticsRowsResult
            {
                Columns = columns.Select(c => new AnalyticsColumnDto { Key = c.Key, Label = c.Label, Kind = c.Kind }).ToList(),
                Rows = sorted.Take(limit)
                    .Select(r => columns.ToDictionary(c => c.Key, c => Normalize(r.Get(c.Key))))
                    .ToList(),
                TotalCount = matched.Count,
                Truncated = matched.Count > limit,
            };
        }

        public async Task<byte[]> ExportXlsxAsync(AnalyticsRowsRequest request, Guid userId)
        {
            request.Limit = MaxRows;
            var result = await RowsAsync(request, userId);
            var headers = result.Columns.Select(c => c.Label).ToList();
            // ClosedXML stores strings as inert text (never formulas), so no injection prefix is needed — and it would show literally.
            var rows = result.Rows.Select(row => (IReadOnlyList<string?>)result.Columns.Select(c => FormatCell(row[c.Key], c.Kind, neutralizeFormulas: false)).ToList());
            return ExcelUtility.Write(headers, rows);
        }

        public async Task<string> ExportCsvAsync(AnalyticsRowsRequest request, Guid userId)
        {
            request.Limit = MaxRows;
            var result = await RowsAsync(request, userId);

            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", result.Columns.Select(c => CsvEscape(c.Label))));
            foreach (var row in result.Rows)
            {
                sb.AppendLine(string.Join(",", result.Columns.Select(c => CsvEscape(FormatCell(row[c.Key], c.Kind)))));
            }

            return sb.ToString();
        }

        private static object? Normalize(object? value)
            => value is DateTime d ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : value;

        private static string? FormatCell(object? value, AnalyticsFieldKind kind, bool neutralizeFormulas = true)
        {
            switch (value)
            {
                case null:
                    return null;
                case DateTime d:
                    return d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                case decimal m:
                    return kind == AnalyticsFieldKind.Money
                        ? m.ToString("0.00", CultureInfo.InvariantCulture)
                        : m.ToString("0.##", CultureInfo.InvariantCulture);
                default:
                    var text = Convert.ToString(value, CultureInfo.InvariantCulture);
                    // Free-text columns (names, owners...) are user-controlled: neutralise spreadsheet formula injection.
                    return neutralizeFormulas && kind == AnalyticsFieldKind.Text && text is { Length: > 0 } && "=+-@\t\r".Contains(text[0])
                        ? "'" + text
                        : text;
            }
        }

        private static string CsvEscape(string? value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return value.IndexOfAny([',', '"', '\r', '\n']) >= 0
                ? "\"" + value.Replace("\"", "\"\"") + "\""
                : value;
        }

        // ------------------------------------------------------------------ shared plumbing

        private static AnalyticsEntityDefinition ResolveDefinition(AnalyticsEntity entity)
        {
            if (!Enum.IsDefined(entity))
            {
                throw new AnalyticsQueryException("Unknown entity.");
            }

            return AnalyticsCatalog.Get(entity);
        }

        private async Task<IReadOnlyList<AnalyticsRecord>> LoadAsync(AnalyticsBaseRequest request, Guid userId)
        {
            AccessScope scope = await _accessScopeService.GetForUserAsync(userId);
            Guid? mine = request.MineOnly ? userId : null;

            switch (request.Entity)
            {
                case AnalyticsEntity.Leads:
                    return await _leadRepository.GetAnalyticsRecordsAsync(mine, scope);
                case AnalyticsEntity.Opportunities:
                    return await _opportunityRepository.GetAnalyticsRecordsAsync(mine, scope);
                case AnalyticsEntity.Customers:
                    return await _customerRepository.GetAnalyticsRecordsAsync(mine, scope);
                case AnalyticsEntity.Quotations:
                    return await _quotationRepository.GetAnalyticsRecordsAsync(mine, scope);
                case AnalyticsEntity.SalesOrders:
                    return await _salesOrderRepository.GetAnalyticsRecordsAsync(mine, scope);
                default:
                    throw new AnalyticsQueryException("Unknown entity.");
            }
        }

        private static List<AnalyticsRecord> ApplyFilters(IReadOnlyList<AnalyticsRecord> records, AnalyticsBaseRequest request, AnalyticsEntityDefinition def)
        {
            IEnumerable<AnalyticsRecord> query = records;

            var dateKey = string.IsNullOrWhiteSpace(request.DateField) ? "createdAt" : request.DateField;
            var dateField = def.Find(dateKey);
            if (dateField is null || dateField.Kind != AnalyticsFieldKind.Date)
            {
                throw new AnalyticsQueryException($"'{dateKey}' is not a date field of {def.Label}.");
            }

            if (request.From is { Year: < MinYear } || request.To is { Year: < MinYear } || request.From is { Year: > MaxYear } || request.To is { Year: > MaxYear })
            {
                throw new AnalyticsQueryException($"Dates must be between {MinYear} and {MaxYear}.");
            }

            if (request.From is not null || request.To is not null)
            {
                DateTime? from = request.From?.Date;
                DateTime? toExclusive = request.To?.Date.AddDays(1);
                query = query.Where(r =>
                    r.Get(dateField.Key) is DateTime d &&
                    (from is null || d >= from) &&
                    (toExclusive is null || d < toExclusive));
            }

            foreach (var filter in request.Filters)
            {
                if (filter is null)
                {
                    throw new AnalyticsQueryException("Filters can't contain empty entries.");
                }

                var rawValues = filter.Values ?? [];
                var field = def.Find(filter.Field) ?? throw new AnalyticsQueryException($"'{filter.Field}' is not a field of {def.Label}.");
                var op = filter.Op.Trim().ToLowerInvariant();

                if (field.Kind == AnalyticsFieldKind.Text)
                {
                    if (op is not ("eq" or "neq" or "in"))
                    {
                        throw new AnalyticsQueryException($"'{filter.Op}' isn't valid for the text field '{field.Label}'. Use eq, neq or in.");
                    }

                    var values = rawValues.Select(v => (v ?? string.Empty).Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    if (values.Count == 0)
                    {
                        throw new AnalyticsQueryException($"The filter on '{field.Label}' needs at least one value.");
                    }

                    var negate = op == "neq";
                    query = query.Where(r => values.Contains(r.Get(field.Key) as string ?? string.Empty) != negate);
                }
                else if (field.Kind is AnalyticsFieldKind.Number or AnalyticsFieldKind.Money)
                {
                    if (op is not ("gte" or "lte"))
                    {
                        throw new AnalyticsQueryException($"'{filter.Op}' isn't valid for the numeric field '{field.Label}'. Use gte or lte.");
                    }

                    if (!decimal.TryParse(rawValues.FirstOrDefault(), NumberStyles.Number, CultureInfo.InvariantCulture, out var threshold))
                    {
                        throw new AnalyticsQueryException($"The filter on '{field.Label}' needs a numeric value.");
                    }

                    var atLeast = op == "gte";
                    query = query.Where(r => r.Get(field.Key) is decimal v && (atLeast ? v >= threshold : v <= threshold));
                }
                else
                {
                    throw new AnalyticsQueryException($"'{field.Label}' is a date field — use the From/To date range instead of a filter.");
                }
            }

            return query.ToList();
        }

        /// <summary>Orders the mixed string/decimal/DateTime values the catalog can produce (nulls are handled before sorting).</summary>
        private sealed class ValueComparer : IComparer<object?>
        {
            public static readonly ValueComparer Instance = new();

            public int Compare(object? x, object? y)
            {
                if (x is string a && y is string b)
                {
                    return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
                }

                return x is IComparable c && y is not null && x.GetType() == y.GetType() ? c.CompareTo(y) : 0;
            }
        }
    }
}
