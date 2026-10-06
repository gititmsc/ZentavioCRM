import type { AnalyticsEntity, AnalyticsFilter, AnalyticsMetric } from "@/services/analyticsService";
import type { RangePreset } from "@/utils/dateRange";

/** What a saved Report stores in SavedAnalyticsItem.ConfigJson. */
export interface ReportConfig {
  version: 1;
  mode: "rows" | "summary";
  entity: AnalyticsEntity;
  /** Relative presets stay relative when the report is reopened ("Last 30 days" is always the last 30 days). */
  preset: RangePreset;
  customFrom: string;
  customTo: string;
  dateField: string;
  mineOnly: boolean;
  filters: AnalyticsFilter[];
  // Detail-rows mode
  columns: string[];
  sortBy: string | null;
  sortDescending: boolean;
  rowLimit: number;
  // Summary mode
  groupBy: string | null;
  metric: AnalyticsMetric;
  metricField: string | null;
  groupLimit: number;
}

export function defaultReportConfig(entity: AnalyticsEntity): ReportConfig {
  const today = new Date();
  const iso = (d: Date) => d.toISOString().slice(0, 10);
  return {
    version: 1,
    mode: "rows",
    entity,
    preset: "all",
    customFrom: iso(new Date(today.getFullYear(), today.getMonth(), 1)),
    customTo: iso(today),
    dateField: "createdAt",
    mineOnly: false,
    filters: [],
    columns: [],
    sortBy: null,
    sortDescending: true,
    rowLimit: 500,
    groupBy: null,
    metric: "Count",
    metricField: null,
    groupLimit: 20,
  };
}

/** Null when the JSON is unreadable, so callers can fall back to a fresh default instead of crashing. */
export function parseReportConfig(json: string): ReportConfig | null {
  try {
    const parsed = JSON.parse(json) as Partial<ReportConfig>;
    if (!parsed || typeof parsed !== "object" || !parsed.entity) return null;
    return { ...defaultReportConfig(parsed.entity), ...parsed, version: 1 };
  } catch {
    return null;
  }
}
