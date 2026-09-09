import { useEffect, useState } from "react";
import { useAuth } from "@/context/AuthContext";
import { dashboardService, type SalesDashboardSummary } from "@/services/dashboardService";
import { PageHeader } from "@/components/layout/PageHeader";
import { FormSection } from "@/components/form/FormSection";
import "./Dashboard.css";

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

      <p className="text-muted small mt-2">
        Use the Leads, Opportunities, and Customers sections to work your pipeline day to day.
      </p>
    </div>
  );
}
