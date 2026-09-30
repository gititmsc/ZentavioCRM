import { useEffect, useState } from "react";
import { dashboardService, type UsageSummary } from "@/services/dashboardService";

const WARNING_THRESHOLD = 0.8;

interface MetricStatus {
  label: string;
  pct: number;
  atLimit: boolean;
}

function metricsFor(usage: UsageSummary): MetricStatus[] {
  const metrics: { label: string; value: number; max: number }[] = [
    { label: "users", value: usage.userCount, max: usage.maxUsers },
    { label: "records", value: usage.recordCount, max: usage.maxRecords },
    { label: "storage", value: usage.databaseSizeMB, max: usage.maxStorageMB },
  ];

  return metrics
    .filter((m) => m.max > 0)
    .map((m) => ({
      label: m.label,
      pct: m.value / m.max,
      atLimit: m.value >= m.max,
    }))
    .filter((m) => m.pct >= WARNING_THRESHOLD);
}

/** Warns as the tenant approaches (and reaches) its plan's user/record/storage limits. Fetched
 * once per app load — this is deliberately lightweight (a single GET), not polled, since plan
 * usage doesn't change fast enough to need live updates. */
export function UsageBanner() {
  const [usage, setUsage] = useState<UsageSummary | null>(null);
  const [dismissed, setDismissed] = useState(false);

  useEffect(() => {
    void (async () => {
      const response = await dashboardService.getUsage();
      if (response.success && response.data) {
        setUsage(response.data);
      }
    })();
  }, []);

  if (!usage || dismissed) {
    return null;
  }

  const flagged = metricsFor(usage);
  if (flagged.length === 0) {
    return null;
  }

  const anyAtLimit = flagged.some((m) => m.atLimit);
  const names = flagged.map((m) => m.label).join(", ");

  return (
    <div className={`alert ${anyAtLimit ? "alert-danger" : "alert-warning"} d-flex justify-content-between align-items-center mb-3`}>
      <span>
        <i className={`bi ${anyAtLimit ? "bi-exclamation-octagon-fill" : "bi-exclamation-triangle-fill"} me-2`} aria-hidden="true" />
        {anyAtLimit
          ? `You've reached your plan's limit on ${names}. Contact your account administrator to upgrade your plan.`
          : `You're approaching your plan's limit on ${names}. Consider upgrading your plan soon.`}
      </span>
      <button type="button" className="btn-close" aria-label="Dismiss" onClick={() => setDismissed(true)} />
    </div>
  );
}
