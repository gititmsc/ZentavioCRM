/**
 * Shared query-editing controls used by both the dashboard widget editor and the report builder:
 * filters, group-by (with time buckets), and metric pickers — all driven by the server's field catalog,
 * so the UI can only ever offer what the engine will accept.
 */
import type {
  AnalyticsCatalogEntity,
  AnalyticsCatalogField,
  AnalyticsFilter,
  AnalyticsMetric,
  FilterOp,
  TimeBucket,
} from "@/services/analyticsService";

export const BUCKETS: { value: TimeBucket; label: string }[] = [
  { value: "day", label: "Day" },
  { value: "week", label: "Week" },
  { value: "month", label: "Month" },
  { value: "year", label: "Year" },
];

/** Known value lists for common enum-backed text fields, offered as type-ahead suggestions only (filters accept any text). */
const SUGGESTIONS: Record<string, string[]> = {
  "Leads.status": ["New", "Assigned", "Contacted", "Qualified", "Nurturing", "ProposalSent", "Converted", "Lost", "Junk"],
  "Leads.source": [
    "Website", "LandingPage", "Referral", "Exhibition", "WhatsApp", "Facebook", "LinkedIn", "EmailCampaign", "GoogleAds", "ManualEntry", "ApiIntegration",
  ],
  "Opportunities.status": ["Qualification", "Discovery", "Proposal", "Negotiation", "VerbalCommit", "ClosedWon", "ClosedLost"],
};

export const findEntity = (catalog: AnalyticsCatalogEntity[], entity: string) => catalog.find((e) => e.entity === entity);

export const numericFields = (def?: AnalyticsCatalogEntity) =>
  def?.fields.filter((f) => f.kind === "Number" || f.kind === "Money") ?? [];

export const dateFields = (def?: AnalyticsCatalogEntity) => def?.fields.filter((f) => f.kind === "Date") ?? [];

export const groupableFields = (def?: AnalyticsCatalogEntity) => def?.fields.filter((f) => f.groupable) ?? [];

/** "createdAt:month" -> { field: "createdAt", bucket: "month" }; "owner" -> { field: "owner", bucket: null }. */
export function splitGroupBy(groupBy: string | null | undefined): { field: string; bucket: TimeBucket | null } {
  if (!groupBy) return { field: "", bucket: null };
  const [field, bucket] = groupBy.split(":");
  return { field, bucket: (bucket as TimeBucket | undefined) ?? null };
}

export const joinGroupBy = (field: string, bucket: TimeBucket | null) =>
  !field ? null : bucket ? `${field}:${bucket}` : field;

const opsFor = (field?: AnalyticsCatalogField): { value: FilterOp; label: string }[] =>
  field?.kind === "Number" || field?.kind === "Money"
    ? [
        { value: "gte", label: "is at least" },
        { value: "lte", label: "is at most" },
      ]
    : [
        { value: "eq", label: "is" },
        { value: "neq", label: "is not" },
        { value: "in", label: "is any of" },
      ];

interface FilterEditorProps {
  def?: AnalyticsCatalogEntity;
  filters: AnalyticsFilter[];
  onChange: (filters: AnalyticsFilter[]) => void;
}

/** Add/remove rows of "field · operator · value(s)". Date fields are excluded — dates use the range picker. */
export function FilterEditor({ def, filters, onChange }: FilterEditorProps) {
  const filterable = def?.fields.filter((f) => f.kind !== "Date") ?? [];

  const update = (index: number, patch: Partial<AnalyticsFilter>) =>
    onChange(filters.map((f, i) => (i === index ? { ...f, ...patch } : f)));

  const add = () => {
    const first = filterable[0];
    if (!first) return;
    onChange([...filters, { field: first.key, op: opsFor(first)[0].value, values: [] }]);
  };

  return (
    <div>
      {filters.map((filter, index) => {
        const field = filterable.find((f) => f.key === filter.field);
        const listId = `suggest-${def?.entity}-${filter.field}-${index}`;
        const suggestions = SUGGESTIONS[`${def?.entity}.${filter.field}`];
        return (
          <div key={index} className="d-flex flex-wrap gap-2 mb-2 align-items-center">
            <select
              className="form-select form-select-sm"
              style={{ width: "auto" }}
              value={filter.field}
              onChange={(e) => {
                const next = filterable.find((f) => f.key === e.target.value);
                update(index, { field: e.target.value, op: opsFor(next)[0].value, values: [] });
              }}
              aria-label="Filter field"
            >
              {filterable.map((f) => (
                <option key={f.key} value={f.key}>
                  {f.label}
                </option>
              ))}
            </select>
            <select
              className="form-select form-select-sm"
              style={{ width: "auto" }}
              value={filter.op}
              onChange={(e) => update(index, { op: e.target.value as FilterOp })}
              aria-label="Filter operator"
            >
              {opsFor(field).map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
            <input
              className="form-control form-control-sm"
              style={{ width: 220 }}
              list={suggestions ? listId : undefined}
              placeholder={filter.op === "in" ? "value, value, ..." : "value"}
              value={filter.values.join(", ")}
              onChange={(e) =>
                update(index, {
                  values: e.target.value
                    .split(",")
                    .map((v) => v.trim())
                    .filter(Boolean),
                })
              }
              aria-label="Filter value"
            />
            {suggestions && (
              <datalist id={listId}>
                {suggestions.map((s) => (
                  <option key={s} value={s} />
                ))}
              </datalist>
            )}
            <button
              type="button"
              className="btn btn-sm btn-outline-danger"
              onClick={() => onChange(filters.filter((_, i) => i !== index))}
              aria-label="Remove filter"
            >
              <i className="bi bi-x-lg" aria-hidden="true" />
            </button>
          </div>
        );
      })}
      <button type="button" className="btn btn-sm btn-outline-secondary" onClick={add} disabled={filterable.length === 0}>
        <i className="bi bi-plus-lg me-1" aria-hidden="true" />
        Add filter
      </button>
    </div>
  );
}

interface GroupByControlProps {
  def?: AnalyticsCatalogEntity;
  value: string | null | undefined;
  onChange: (groupBy: string | null) => void;
  /** Label of the empty option, e.g. "None (single total)". */
  noneLabel?: string;
  allowNone?: boolean;
}

/** Group-by select: groupable text fields, or a date field plus a bucket size. */
export function GroupByControl({ def, value, onChange, noneLabel = "None (single total)", allowNone = true }: GroupByControlProps) {
  const { field, bucket } = splitGroupBy(value);
  const selected = def?.fields.find((f) => f.key === field);
  const isDate = selected?.kind === "Date";

  return (
    <div className="d-flex gap-2">
      <select
        className="form-select form-select-sm"
        value={field}
        onChange={(e) => {
          const next = def?.fields.find((f) => f.key === e.target.value);
          onChange(joinGroupBy(e.target.value, next?.kind === "Date" ? "month" : null));
        }}
        aria-label="Group by"
      >
        {allowNone && <option value="">{noneLabel}</option>}
        {groupableFields(def).map((f) => (
          <option key={f.key} value={f.key}>
            {f.label}
          </option>
        ))}
        {dateFields(def).map((f) => (
          <option key={f.key} value={f.key}>
            {f.label} (over time)
          </option>
        ))}
      </select>
      {isDate && (
        <select
          className="form-select form-select-sm"
          style={{ width: "auto" }}
          value={bucket ?? "month"}
          onChange={(e) => onChange(joinGroupBy(field, e.target.value as TimeBucket))}
          aria-label="Time bucket"
        >
          {BUCKETS.map((b) => (
            <option key={b.value} value={b.value}>
              by {b.label.toLowerCase()}
            </option>
          ))}
        </select>
      )}
    </div>
  );
}

interface MetricControlProps {
  def?: AnalyticsCatalogEntity;
  metric: AnalyticsMetric;
  metricField: string | null | undefined;
  onChange: (metric: AnalyticsMetric, metricField: string | null) => void;
}

/** "Count of records", or "Sum / Average of <numeric field>". */
export function MetricControl({ def, metric, metricField, onChange }: MetricControlProps) {
  const numeric = numericFields(def);
  return (
    <div className="d-flex gap-2">
      <select
        className="form-select form-select-sm"
        style={{ width: "auto" }}
        value={metric}
        onChange={(e) => {
          const next = e.target.value as AnalyticsMetric;
          onChange(next, next === "Count" ? null : (metricField ?? numeric[0]?.key ?? null));
        }}
        aria-label="Metric"
      >
        <option value="Count">Count of records</option>
        <option value="Sum" disabled={numeric.length === 0}>
          Sum of
        </option>
        <option value="Average" disabled={numeric.length === 0}>
          Average of
        </option>
      </select>
      {metric !== "Count" && (
        <select
          className="form-select form-select-sm"
          value={metricField ?? ""}
          onChange={(e) => onChange(metric, e.target.value || null)}
          aria-label="Metric field"
        >
          {numeric.map((f) => (
            <option key={f.key} value={f.key}>
              {f.label}
            </option>
          ))}
        </select>
      )}
    </div>
  );
}
