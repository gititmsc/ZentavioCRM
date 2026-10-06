import { useEffect, useState } from "react";
import { analyticsService, type AnalyticsQueryResult } from "@/services/analyticsService";
import { BarList, ColumnChart, DonutChart, LineChart } from "@/components/charts/SimpleCharts";
import type { Widget } from "./widgets";

export function formatResultValue(kind: AnalyticsQueryResult["valueKind"], value: number, compact = false): string {
  if (kind === "money") {
    return value.toLocaleString(undefined, {
      style: "currency",
      currency: "USD",
      ...(compact ? { notation: "compact" as const, maximumFractionDigits: 1 } : {}),
    });
  }
  return value.toLocaleString(undefined, { maximumFractionDigits: 2 });
}

interface WidgetBodyProps {
  widget: Widget;
  result: AnalyticsQueryResult;
}

/** Pure renderer: a widget's chart/number/table for an already-fetched result. */
export function WidgetBody({ widget, result }: WidgetBodyProps) {
  const full = (n: number) => formatResultValue(result.valueKind, n);
  const compact = (n: number) => formatResultValue(result.valueKind, n, true);

  if (widget.type === "kpi") {
    return (
      <div>
        <div style={{ fontSize: "2rem", fontWeight: 700, lineHeight: 1.1 }}>{full(result.total)}</div>
        <div className="text-muted small mt-1">{result.totalCount.toLocaleString()} matching records</div>
      </div>
    );
  }

  if (result.groupKind === "none") {
    return <div className="text-muted small">Pick a "group by" in this widget's settings to chart it.</div>;
  }

  const points = result.rows.map((r) => ({ label: r.label, value: r.value }));

  switch (widget.type) {
    case "bar":
      return <BarList items={result.rows.map((r) => ({ label: r.label, value: r.value, display: full(r.value) }))} />;
    case "column":
      return <ColumnChart points={points} formatValue={compact} />;
    case "line":
      return <LineChart series={[{ name: widget.title, color: "#2563eb", points }]} formatValue={compact} />;
    case "pie":
      return <DonutChart items={points} formatValue={full} />;
    default:
      return (
        <div className="table-responsive">
          <table className="table table-sm mb-0">
            <thead>
              <tr>
                <th>Group</th>
                <th className="text-end">Value</th>
                <th className="text-end">Records</th>
              </tr>
            </thead>
            <tbody>
              {result.rows.map((r) => (
                <tr key={r.key}>
                  <td>{r.label}</td>
                  <td className="text-end">{full(r.value)}</td>
                  <td className="text-end text-muted">{r.count.toLocaleString()}</td>
                </tr>
              ))}
              {result.rows.length === 0 && (
                <tr>
                  <td colSpan={3} className="text-muted">
                    No data in this range.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
          {result.truncated && <div className="text-muted small mt-1">Showing the top {result.rows.length} groups.</div>}
        </div>
      );
  }
}

interface WidgetViewProps {
  widget: Widget;
  from: string;
  to: string;
  mineOnly: boolean;
}

/** Fetches a widget's data for the dashboard's current range/scope and renders it, with per-widget loading and error states. */
export function WidgetView({ widget, from, to, mineOnly }: WidgetViewProps) {
  const [result, setResult] = useState<AnalyticsQueryResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const queryKey = JSON.stringify(widget.query);

  useEffect(() => {
    let cancelled = false;
    setIsLoading(true);
    (async () => {
      const q = widget.query;
      const response = await analyticsService.query({
        entity: q.entity,
        dateField: q.dateField ?? "createdAt",
        from: q.ignoreDateRange ? null : from || null,
        to: q.ignoreDateRange ? null : to || null,
        mineOnly,
        filters: q.filters,
        metric: q.metric,
        metricField: q.metricField ?? null,
        groupBy: q.groupBy ?? null,
        limit: q.limit,
        sortDescending: q.sortDescending,
      });
      if (cancelled) return;
      setIsLoading(false);
      if (!response.success || !response.data) {
        setResult(null);
        setError(response.message || "Unable to load this widget.");
        return;
      }
      setError(null);
      setResult(response.data);
    })();
    return () => {
      cancelled = true;
    };
    // widget.query is tracked through its serialized form so editing a widget re-fetches but identity churn doesn't.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [queryKey, from, to, mineOnly]);

  if (error) {
    return <div className="text-danger small">{error}</div>;
  }
  if (!result) {
    return <div className="text-muted small">{isLoading ? "Loading..." : "No data."}</div>;
  }
  return (
    <div style={{ opacity: isLoading ? 0.6 : 1, transition: "opacity 120ms" }}>
      <WidgetBody widget={widget} result={result} />
    </div>
  );
}
