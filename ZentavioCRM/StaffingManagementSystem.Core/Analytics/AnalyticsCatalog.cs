using ZentavioCRM.Core.Common;

namespace ZentavioCRM.Core.Analytics
{
    public enum AnalyticsEntity
    {
        Leads = 1,
        Opportunities = 2,
        Customers = 3,
        Quotations = 4,
        SalesOrders = 5,
    }

    public enum AnalyticsFieldKind
    {
        Text = 1,
        Number = 2,
        Money = 3,
        Date = 4,
    }

    /// <summary>
    /// One whitelisted field a client may group by, filter on, aggregate, or show as a report column.
    /// <see cref="Key"/> is the only identifier that ever crosses the API boundary — it maps to a property on
    /// <see cref="AnalyticsRecord"/> via a fixed switch, never to a column name, so arbitrary client input
    /// cannot reach SQL or reflection.
    /// </summary>
    public sealed record AnalyticsField(string Key, string Label, AnalyticsFieldKind Kind, bool Groupable = false);

    public sealed class AnalyticsEntityDefinition
    {
        public AnalyticsEntityDefinition(AnalyticsEntity entity, string label, string viewPermission, params AnalyticsField[] fields)
        {
            Entity = entity;
            Label = label;
            ViewPermission = viewPermission;
            Fields = fields;
        }

        public AnalyticsEntity Entity { get; }

        public string Label { get; }

        /// <summary>The module's own View permission — required to query this entity at all, on top of Role.VisibilityScope.</summary>
        public string ViewPermission { get; }

        public IReadOnlyList<AnalyticsField> Fields { get; }

        public AnalyticsField? Find(string key)
            => Fields.FirstOrDefault(f => string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase));

        public IEnumerable<AnalyticsField> DateFields => Fields.Where(f => f.Kind == AnalyticsFieldKind.Date);
    }

    /// <summary>The single source of truth for what the analytics engine and report builder can touch. The UI reads this via GET /api/analytics/catalog.</summary>
    public static class AnalyticsCatalog
    {
        public static readonly IReadOnlyList<AnalyticsEntityDefinition> All =
        [
            new(AnalyticsEntity.Leads, "Leads", PermissionCodes.LeadsView,
                new("number", "Lead #", AnalyticsFieldKind.Text),
                new("name", "Company", AnalyticsFieldKind.Text),
                new("status", "Status", AnalyticsFieldKind.Text, Groupable: true),
                new("source", "Source", AnalyticsFieldKind.Text, Groupable: true),
                new("industry", "Industry", AnalyticsFieldKind.Text, Groupable: true),
                new("owner", "Owner", AnalyticsFieldKind.Text, Groupable: true),
                new("territory", "Territory", AnalyticsFieldKind.Text, Groupable: true),
                new("value", "Expected value", AnalyticsFieldKind.Money),
                new("score", "Lead score", AnalyticsFieldKind.Number),
                new("createdAt", "Created", AnalyticsFieldKind.Date),
                new("convertedAt", "Converted", AnalyticsFieldKind.Date)),

            new(AnalyticsEntity.Opportunities, "Opportunities", PermissionCodes.OpportunitiesView,
                new("number", "Opportunity #", AnalyticsFieldKind.Text),
                new("name", "Name", AnalyticsFieldKind.Text),
                new("customer", "Customer", AnalyticsFieldKind.Text, Groupable: true),
                new("status", "Stage", AnalyticsFieldKind.Text, Groupable: true),
                new("owner", "Owner", AnalyticsFieldKind.Text, Groupable: true),
                new("value", "Value", AnalyticsFieldKind.Money),
                new("probability", "Probability %", AnalyticsFieldKind.Number),
                new("createdAt", "Created", AnalyticsFieldKind.Date),
                new("closedAt", "Closed", AnalyticsFieldKind.Date)),

            new(AnalyticsEntity.Customers, "Customers", PermissionCodes.CustomersView,
                new("number", "Customer #", AnalyticsFieldKind.Text),
                new("name", "Name", AnalyticsFieldKind.Text),
                new("status", "Type", AnalyticsFieldKind.Text, Groupable: true),
                new("source", "Acquisition source", AnalyticsFieldKind.Text, Groupable: true),
                new("industry", "Industry", AnalyticsFieldKind.Text, Groupable: true),
                new("owner", "Owner", AnalyticsFieldKind.Text, Groupable: true),
                new("value", "Annual revenue", AnalyticsFieldKind.Money),
                new("createdAt", "Created", AnalyticsFieldKind.Date)),

            new(AnalyticsEntity.Quotations, "Quotations", PermissionCodes.QuotationsView,
                new("number", "Quotation #", AnalyticsFieldKind.Text),
                new("name", "Opportunity", AnalyticsFieldKind.Text),
                new("customer", "Customer", AnalyticsFieldKind.Text, Groupable: true),
                new("status", "Status", AnalyticsFieldKind.Text, Groupable: true),
                new("owner", "Owner", AnalyticsFieldKind.Text, Groupable: true),
                new("value", "Grand total", AnalyticsFieldKind.Money),
                new("createdAt", "Created", AnalyticsFieldKind.Date),
                new("validUntil", "Valid until", AnalyticsFieldKind.Date)),

            new(AnalyticsEntity.SalesOrders, "Sales Orders", PermissionCodes.SalesOrdersView,
                new("number", "Order #", AnalyticsFieldKind.Text),
                new("customer", "Customer", AnalyticsFieldKind.Text, Groupable: true),
                new("status", "Status", AnalyticsFieldKind.Text, Groupable: true),
                new("owner", "Owner", AnalyticsFieldKind.Text, Groupable: true),
                new("value", "Grand total", AnalyticsFieldKind.Money),
                new("createdAt", "Created", AnalyticsFieldKind.Date),
                new("orderDate", "Order date", AnalyticsFieldKind.Date)),
        ];

        public static AnalyticsEntityDefinition Get(AnalyticsEntity entity)
            => All.First(d => d.Entity == entity);
    }

    /// <summary>
    /// Flat, entity-agnostic row the engine aggregates over. Each repository projects its own entity into this
    /// shape (after applying the caller's access scope); the engine then groups/filters/sums purely in memory —
    /// the same SMB-scale trade-off the existing dashboard aggregation already makes.
    /// </summary>
    public sealed class AnalyticsRecord
    {
        public string? Number { get; set; }

        public string? Name { get; set; }

        public string? Status { get; set; }

        public string? Source { get; set; }

        public string? Industry { get; set; }

        public Guid? OwnerId { get; set; }

        public string? Owner { get; set; }

        public string? Territory { get; set; }

        public string? Customer { get; set; }

        public decimal? Value { get; set; }

        public decimal? Score { get; set; }

        public decimal? Probability { get; set; }

        public DateTime CreatedAt { get; set; }

        /// <summary>The entity's second date (converted / closed / valid-until / order date), whichever its catalog entry exposes.</summary>
        public DateTime? Date2 { get; set; }

        /// <summary>Fixed key->value map. Returns null for unknown keys — callers validate keys against the catalog first.</summary>
        public object? Get(string key) => key.ToLowerInvariant() switch
        {
            "number" => Number,
            "name" => Name,
            "status" => Status,
            "source" => Source,
            "industry" => Industry,
            "owner" => Owner,
            "territory" => Territory,
            "customer" => Customer,
            "value" => Value,
            "score" => Score,
            "probability" => Probability,
            "createdat" => CreatedAt,
            "convertedat" or "closedat" or "validuntil" or "orderdate" => Date2,
            _ => null,
        };
    }
}
