import { useEffect, useState } from "react";
import { useAuth } from "@/context/AuthContext";
import { dashboardService, type DashboardAnalytics, type SalesDashboardSummary } from "@/services/dashboardService";
import { PageHeader } from "@/components/layout/PageHeader";
import { FormSection } from "@/components/form/FormSection";
import { BarList, ColumnChart, LineChart } from "@/components/charts/SimpleCharts";
import "./Dashboard.css";

type Preset = "last7" | "last30" | "last90" | "thisMonth" | "thisYear" | "custom";

const PRESET_LABELS: Record<Preset, string> = {
  last7: "Last 7 days",
  last30: "Last 30 days",
  last90: "Last 90 days",
  thisMonth: "This month",
  thisYear: "This year",
  custom: "Custom range",
};

const addDays = (d: Date, n: number) => {
  const copy = new Date(d);
  copy.setDate(copy.getDate() + n);
  return copy;
};

/** yyyy-MM-dd in the viewer's local calendar (what a date input produces/expects). */
const toDateInput = (d: Date) =>
  `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;

function resolveRange(preset: Preset, customFrom: string, customTo: string): { from: string; to: string } {
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
    default:
      return { from: customFrom, to: customTo };
  }
}

/** "EmailCampaign" -> "Email Campaign". */
const humanize = (value: string) => value.replace(/([a-z])([A-Z])/g, "$1 $2");

function formatBucket(iso: string, granularity: "week" | "month") {
  const d = new Date(iso);
  return granularity === "week"
    ? d.toLocaleDateString(undefined, { day: "numeric", month: "short", timeZone: "UTC" })
    : d.toLocaleDateString(undefined, { month: "short", year: "2-digit", timeZone: "UTC" });
}

const compactCurrency = (n: number) =>
  n.toLocaleString(undefined, { style: "currency", currency: "USD", notation: "compact", maximumFractionDigits: 1 });

const STAGE_LABELS: Record<string, string> = {
  Qualification: "Qualification",
  Discovery: "Discovery",
  Proposal: "Proposal",
  Negotiation: "Negotiation",
  VerbalCommit: "Verbal Commit",
  ClosedWon: "Closed Won",
  ClosedLost: "Closed Lost",
};

interface StatCardProps {
  icon: string;
  label: string;
  value: string;
  variant?: "accent" | "teal" | "primary";
}

function StatCard({ icon, label, value, variant = "accent" }: StatCardProps) {
  return (
    <div className={`itm-stat-card ${variant !== "accent" ? `itm-stat-card--${variant}` : ""}`}>
      <span className="itm-stat-card__icon">
        <i className={`bi ${icon}`} aria-hidden="true" />
      </span>
      <div>
        <div className="itm-stat-card__label">{label}</div>
        <div className="itm-stat-card__value">{value}</div>
      </div>
    </div>
  );
}

/** Landing page reached after a successful login. */
export default function Dashboard() {
  const { user } = useAuth();
  const [summary, setSummary] = useState<SalesDashboardSummary | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      const result = await dashboardService.getSalesSummary();
      setIsLoading(false);
      if (!result.success || !result.data) {
        setError(result.message || "Unable to load dashboard.");
        return;
      }
      setSummary(result.data);
    })();
  }, []);

  // ---- Analytics (date range + My/Team scope) ----
  const [preset, setPreset] = useState<Preset>("last30");
  const [customFrom, setCustomFrom] = useState(() => toDateInput(addDays(new Date(), -30)));
  const [customTo, setCustomTo] = useState(() => toDateInput(new Date()));
  const [mineOnly, setMineOnly] = useState(false);
  const [analytics, setAnalytics] = useState<DashboardAnalytics | null>(null);
  const [isAnalyticsLoading, setIsAnalyticsLoading] = useState(true);
  const [analyticsError, setAnalyticsError] = useState<string | null>(null);

  const range = resolveRange(preset, customFrom, customTo);

  useEffect(() => {
    if (!range.from || !range.to || range.from > range.to) return;
    let cancelled = false;
    setIsAnalyticsLoading(true);
    (async () => {
      const result = await dashboardService.getAnalytics({ from: range.from, to: range.to, mineOnly });
      if (cancelled) return;
      setIsAnalyticsLoading(false);
      if (!result.success || !result.data) {
        setAnalyticsError(result.message || "Unable to load analytics.");
        return;
      }
      setAnalyticsError(null);
      setAnalytics(result.data);
    })();
    return () => {
      cancelled = true;
    };
  }, [range.from, range.to, mineOnly]);

  const trendPoints = (analytics?.trend ?? []).map((p) => ({
    ...p,
    label: formatBucket(p.bucketStartUtc, analytics?.granularity ?? "month"),
  }));

  const fmt = (n: number | undefined) => (n != null ? n.toLocaleString() : "—");
  const currency = (n: number | undefined) =>
    n != null ? n.toLocaleString(undefined, { style: "currency", currency: "USD" }) : "—";

  return (
    <div>
      <PageHeader
        title={`Welcome${user ? `, ${user.fullName}` : ""}`}
        subtitle="Here's what's happening across your CRM today."
      />

      {error && <div className="alert alert-danger">{error}</div>}

      <div className="itm-stat-grid">
        <StatCard icon="bi-funnel-fill" label="Open Leads" value={isLoading ? "—" : fmt(summary?.openLeadsCount)} />
        <StatCard
          icon="bi-building"
          label="Active Customers"
          value={isLoading ? "—" : fmt(summary?.activeCustomersCount)}
          variant="teal"
        />
        <StatCard
          icon="bi-arrow-repeat"
          label="Converted This Month"
          value={isLoading ? "—" : fmt(summary?.convertedLeadsThisMonthCount)}
          variant="primary"
        />
        <StatCard
          icon="bi-cash-stack"
          label="Pipeline Value"
          value={isLoading ? "—" : currency(summary?.pipelineValue)}
        />
      </div>

      <div className="itm-stat-grid" style={{ gridTemplateColumns: "repeat(2, 1fr)" }}>
        <StatCard
          icon="bi-graph-up-arrow"
          label="Open Opportunities"
          value={isLoading ? "—" : fmt(summary?.openOpportunitiesCount)}
          variant="teal"
        />
        <StatCard
          icon="bi-trophy"
          label="Win Rate"
          value={isLoading ? "—" : summary ? `${summary.winRatePercentage}%` : "—"}
          variant="primary"
        />
      </div>

      {!isLoading && summary && summary.stageBreakdown.length > 0 && (
        <FormSection icon="bi-bar-chart-steps" title="Pipeline by Stage" description="Open opportunities grouped by their current stage.">
          <div className="row g-3">
            {summary.stageBreakdown.map((item) => (
              <div key={item.stage} className="col-md-3 col-6">
                <div className="text-muted small">{STAGE_LABELS[item.stage] ?? item.stage}</div>
                <div className="fw-semibold">
                  {item.count} &middot; {item.value.toLocaleString(undefined, { style: "currency", currency: "USD" })}
                </div>
              </div>
            ))}
          </div>
        </FormSection>
      )}

      <div className="d-flex flex-wrap align-items-end gap-2 mt-4 mb-3">
        <h5 className="mb-0 me-auto">Analytics</h5>

        <div className="btn-group btn-group-sm" role="group" aria-label="Record scope">
          <button
            type="button"
            className={`btn ${mineOnly ? "btn-primary" : "btn-outline-secondary"}`}
            onClick={() => setMineOnly(true)}
          >
            My records
          </button>
          <button
            type="button"
            className={`btn ${!mineOnly ? "btn-primary" : "btn-outline-secondary"}`}
            onClick={() => setMineOnly(false)}
          >
            Team / all I can see
          </button>
        </div>

        <select
          className="form-select form-select-sm"
          style={{ width: "auto" }}
          value={preset}
          onChange={(e) => setPreset(e.target.value as Preset)}
          aria-label="Date range"
        >
          {(Object.keys(PRESET_LABELS) as Preset[]).map((p) => (
            <option key={p} value={p}>
              {PRESET_LABELS[p]}
            </option>
          ))}
        </select>

        {preset === "custom" && (
          <>
            <input
              type="date"
              className="form-control form-control-sm"
              style={{ width: "auto" }}
              value={customFrom}
              max={customTo}
              onChange={(e) => setCustomFrom(e.target.value)}
              aria-label="From date"
            />
            <input
              type="date"
              className="form-control form-control-sm"
              style={{ width: "auto" }}
              value={customTo}
              min={customFrom}
              onChange={(e) => setCustomTo(e.target.value)}
              aria-label="To date"
            />
          </>
        )}
      </div>

      {analyticsError && <div className="alert alert-danger">{analyticsError}</div>}

      {isAnalyticsLoading && !analytics && <div className="text-muted small">Loading analytics...</div>}

      {analytics && (
        <div style={{ opacity: isAnalyticsLoading ? 0.6 : 1, transition: "opacity 120ms" }}>
          <div className="itm-stat-grid" style={{ gridTemplateColumns: "repeat(3, 1fr)" }}>
            <StatCard icon="bi-cash-coin" label="Won Revenue" value={currency(analytics.wonRevenueTotal)} />
            <StatCard icon="bi-trophy" label="Deals Won" value={fmt(analytics.wonCount)} variant="teal" />
            <StatCard icon="bi-x-octagon" label="Deals Lost" value={fmt(analytics.lostCount)} variant="primary" />
          </div>

          <div className="row g-3 mb-3">
            <div className="col-lg-6">
              <FormSection icon="bi-funnel" title="Conversion Funnel" description="Records created inside the selected range.">
                <BarList items={analytics.funnel.map((s) => ({ label: s.label, value: s.count }))} />
              </FormSection>
            </div>
            <div className="col-lg-6">
              <FormSection icon="bi-pie-chart" title="Leads by Source" description="Where leads created in this range came from.">
                <BarList
                  items={analytics.leadsBySource.map((s) => ({
                    label: humanize(s.source),
                    value: s.count,
                    sublabel: `${s.convertedCount} converted`,
                  }))}
                />
              </FormSection>
            </div>
          </div>

          <div className="row g-3 mb-3">
            <div className="col-lg-6">
              <FormSection
                icon="bi-graph-up"
                title="Won vs Lost"
                description={`Deals closed per ${analytics.granularity}.`}
              >
                <LineChart
                  series={[
                    { name: "Won", color: "#16a34a", points: trendPoints.map((p) => ({ label: p.label, value: p.wonCount })) },
                    { name: "Lost", color: "#dc2626", points: trendPoints.map((p) => ({ label: p.label, value: p.lostCount })) },
                  ]}
                />
              </FormSection>
            </div>
            <div className="col-lg-6">
              <FormSection
                icon="bi-bar-chart"
                title="Revenue Won"
                description={`Closed-won value per ${analytics.granularity}.`}
              >
                <ColumnChart
                  points={trendPoints.map((p) => ({ label: p.label, value: p.wonRevenue }))}
                  formatValue={compactCurrency}
                />
              </FormSection>
            </div>
          </div>

          <div className="row g-3 mb-3">
            <div className="col-lg-6">
              <FormSection icon="bi-person-badge" title="Leaderboard by Owner" description="Ranked by revenue won in this range.">
                <BarList
                  emptyText="No owner activity in this range."
                  items={analytics.ownerLeaderboard.map((o) => ({
                    label: o.name,
                    value: o.wonRevenue,
                    display: currency(o.wonRevenue),
                    sublabel: `${o.wonCount} won · ${o.lostCount} lost · ${o.leadsAssigned} leads`,
                  }))}
                />
              </FormSection>
            </div>
            <div className="col-lg-6">
              <FormSection icon="bi-geo-alt" title="Leaderboard by Territory" description="Leads created and converted, by territory.">
                <BarList
                  emptyText="No territory activity in this range."
                  items={analytics.territoryLeaderboard.map((t) => ({
                    label: t.name,
                    value: t.leadsCount,
                    display: `${t.leadsCount} leads`,
                    sublabel: `${t.convertedCount} converted (${t.conversionRate}%)`,
                  }))}
                />
              </FormSection>
            </div>
          </div>
        </div>
      )}

      <p className="text-muted small mt-2">
        Use the Leads, Opportunities, and Customers sections to work your pipeline day to day.
      </p>
    </div>
  );
}
