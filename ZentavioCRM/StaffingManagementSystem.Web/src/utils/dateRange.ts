/** Date-range presets shared by the Dashboard analytics section, custom dashboards, and the report builder. */

export type BasePreset = "last7" | "last30" | "last90" | "thisMonth" | "thisYear" | "custom";

/** Reports (and dashboard widgets) can also look at everything, with no date bounds. */
export type RangePreset = BasePreset | "all";

export const PRESET_LABELS: Record<BasePreset, string> = {
  last7: "Last 7 days",
  last30: "Last 30 days",
  last90: "Last 90 days",
  thisMonth: "This month",
  thisYear: "This year",
  custom: "Custom range",
};

export const RANGE_PRESET_LABELS: Record<RangePreset, string> = {
  ...PRESET_LABELS,
  all: "All time",
};

export const addDays = (d: Date, n: number) => {
  const copy = new Date(d);
  copy.setDate(copy.getDate() + n);
  return copy;
};

/** yyyy-MM-dd in the viewer's local calendar (what a date input produces/expects). */
export const toDateInput = (d: Date) =>
  `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;

/** Resolves a preset to inclusive yyyy-MM-dd bounds. "all" yields empty strings (no bounds). */
export function resolveRange(preset: RangePreset, customFrom: string, customTo: string): { from: string; to: string } {
  const today = new Date();
  switch (preset) {
    case "last7":
      return { from: toDateInput(addDays(today, -6)), to: toDateInput(today) };
    case "last30":
      return { from: toDateInput(addDays(today, -29)), to: toDateInput(today) };
    case "last90":
      return { from: toDateInput(addDays(today, -89)), to: toDateInput(today) };
    case "thisMonth":
      return { from: toDateInput(new Date(today.getFullYear(), today.getMonth(), 1)), to: toDateInput(today) };
    case "thisYear":
      return { from: toDateInput(new Date(today.getFullYear(), 0, 1)), to: toDateInput(today) };
    case "all":
      return { from: "", to: "" };
    default:
      return { from: customFrom, to: customTo };
  }
}
