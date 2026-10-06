import type { AnalyticsFilter, AnalyticsMetric, AnalyticsEntity } from "@/services/analyticsService";

export type WidgetType = "kpi" | "bar" | "column" | "line" | "pie" | "table";
export type WidgetWidth = "third" | "half" | "full";

export const WIDGET_TYPES: { value: WidgetType; label: string; icon: string }[] = [
  { value: "kpi", label: "Single number", icon: "bi-123" },
  { value: "bar", label: "Bar list", icon: "bi-list-nested" },
  { value: "column", label: "Column chart", icon: "bi-bar-chart" },
  { value: "line", label: "Line chart", icon: "bi-graph-up" },
  { value: "pie", label: "Donut chart", icon: "bi-pie-chart" },
  { value: "table", label: "Table", icon: "bi-table" },
];

export const WIDGET_WIDTHS: { value: WidgetWidth; label: string; col: string }[] = [
  { value: "third", label: "Small (1/3)", col: "col-lg-4 col-md-6" },
  { value: "half", label: "Medium (1/2)", col: "col-lg-6" },
  { value: "full", label: "Large (full)", col: "col-12" },
];

export interface WidgetQuery {
  entity: AnalyticsEntity;
  dateField?: string | null;
  metric: AnalyticsMetric;
  metricField?: string | null;
  groupBy?: string | null;
  limit: number;
  sortDescending: boolean;
  filters: AnalyticsFilter[];
  /** When true the dashboard's date range is not applied (e.g. "open pipeline value right now"). */
  ignoreDateRange?: boolean;
}

export interface Widget {
  id: string;
  title: string;
  type: WidgetType;
  width: WidgetWidth;
  query: WidgetQuery;
}

export interface DashboardConfig {
  version: 1;
  widgets: Widget[];
}

export const newWidgetId = () =>
  typeof crypto !== "undefined" && "randomUUID" in crypto ? crypto.randomUUID() : `w-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;

export function blankWidget(entity: AnalyticsEntity): Widget {
  return {
    id: newWidgetId(),
    title: "New widget",
    type: "kpi",
    width: "third",
    query: { entity, dateField: "createdAt", metric: "Count", metricField: null, groupBy: null, limit: 10, sortDescending: true, filters: [] },
  };
}

/** Tolerant parse: a corrupt or future-version config yields an empty dashboard rather than crashing the page. */
export function parseDashboardConfig(json: string): DashboardConfig {
  try {
    const parsed = JSON.parse(json) as Partial<DashboardConfig>;
    return { version: 1, widgets: Array.isArray(parsed.widgets) ? parsed.widgets : [] };
  } catch {
    return { version: 1, widgets: [] };
  }
}

/** Starter set for a first dashboard — only uses entities every sales role can view; widgets for entities the viewer can't see just show "no access". */
export function templateWidgets(): Widget[] {
  const closedStages = ["ClosedWon", "ClosedLost"];
  return [
    {
      id: newWidgetId(),
      title: "Open pipeline value",
      type: "kpi",
      width: "third",
      query: {
        entity: "Opportunities", dateField: "createdAt", metric: "Sum", metricField: "value", groupBy: null, limit: 10, sortDescending: true,
        filters: [{ field: "status", op: "neq", values: closedStages }], ignoreDateRange: true,
      },
    },
    {
      id: newWidgetId(),
      title: "New leads",
      type: "kpi",
      width: "third",
      query: { entity: "Leads", dateField: "createdAt", metric: "Count", metricField: null, groupBy: null, limit: 10, sortDescending: true, filters: [] },
    },
    {
      id: newWidgetId(),
      title: "Revenue won",
      type: "kpi",
      width: "third",
      query: {
        entity: "Opportunities", dateField: "closedAt", metric: "Sum", metricField: "value", groupBy: null, limit: 10, sortDescending: true,
        filters: [{ field: "status", op: "eq", values: ["ClosedWon"] }],
      },
    },
    {
      id: newWidgetId(),
      title: "Revenue won over time",
      type: "column",
      width: "half",
      query: {
        entity: "Opportunities", dateField: "closedAt", metric: "Sum", metricField: "value", groupBy: "closedAt:month", limit: 10, sortDescending: true,
        filters: [{ field: "status", op: "eq", values: ["ClosedWon"] }],
      },
    },
    {
      id: newWidgetId(),
      title: "Leads by source",
      type: "pie",
      width: "half",
      query: { entity: "Leads", dateField: "createdAt", metric: "Count", metricField: null, groupBy: "source", limit: 8, sortDescending: true, filters: [] },
    },
    {
      id: newWidgetId(),
      title: "Top owners by revenue won",
      type: "bar",
      width: "half",
      query: {
        entity: "Opportunities", dateField: "closedAt", metric: "Sum", metricField: "value", groupBy: "owner", limit: 8, sortDescending: true,
        filters: [{ field: "status", op: "eq", values: ["ClosedWon"] }],
      },
    },
    {
      id: newWidgetId(),
      title: "Opportunities by stage",
      type: "bar",
      width: "half",
      query: { entity: "Opportunities", dateField: "createdAt", metric: "Count", metricField: null, groupBy: "status", limit: 10, sortDescending: true, filters: [], ignoreDateRange: true },
    },
  ];
}
