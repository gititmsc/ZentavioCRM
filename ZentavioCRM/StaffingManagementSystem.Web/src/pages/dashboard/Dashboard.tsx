import { useEffect, useState } from "react";
import { useAuth } from "@/context/AuthContext";
import { dashboardService, type SalesDashboardSummary } from "@/services/dashboardService";

const STAGE_LABELS: Record<string, string> = {
  Qualification: "Qualification",
  Discovery: "Discovery",
  Proposal: "Proposal",
  Negotiation: "Negotiation",
  VerbalCommit: "Verbal Commit",
  ClosedWon: "Closed Won",
  ClosedLost: "Closed Lost",
};

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

  return (
    <div>
      <h1 className="h4 mb-1">Welcome{user ? `, ${user.fullName}` : ""}</h1>
      <p className="text-muted">Here's what's happening across your CRM today.</p>

      {error && <div className="alert alert-danger">{error}</div>}

      <div className="row g-3 mt-2">
        <div className="col-md-3">
          <div className="card shadow-sm border-0">
            <div className="card-body">
              <div className="text-muted small">Open Leads</div>
              <div className="h3 mb-0">{isLoading ? "—" : fmt(summary?.openLeadsCount)}</div>
            </div>
          </div>
        </div>
        <div className="col-md-3">
          <div className="card shadow-sm border-0">
            <div className="card-body">
              <div className="text-muted small">Active Customers</div>
              <div className="h3 mb-0">{isLoading ? "—" : fmt(summary?.activeCustomersCount)}</div>
            </div>
          </div>
        </div>
        <div className="col-md-3">
          <div className="card shadow-sm border-0">
            <div className="card-body">
              <div className="text-muted small">Converted This Month</div>
              <div className="h3 mb-0">{isLoading ? "—" : fmt(summary?.convertedLeadsThisMonthCount)}</div>
            </div>
          </div>
        </div>
        <div className="col-md-3">
          <div className="card shadow-sm border-0">
            <div className="card-body">
              <div className="text-muted small">Pipeline Value</div>
              <div className="h3 mb-0">
                {isLoading ? "—" : summary ? summary.pipelineValue.toLocaleString(undefined, { style: "currency", currency: "USD" }) : "—"}
              </div>
            </div>
          </div>
        </div>
      </div>

      <div className="row g-3 mt-1">
        <div className="col-md-4">
          <div className="card shadow-sm border-0">
            <div className="card-body">
              <div className="text-muted small">Open Opportunities</div>
              <div className="h4 mb-0">{isLoading ? "—" : fmt(summary?.openOpportunitiesCount)}</div>
            </div>
          </div>
        </div>
        <div className="col-md-4">
          <div className="card shadow-sm border-0">
            <div className="card-body">
              <div className="text-muted small">Win Rate</div>
              <div className="h4 mb-0">{isLoading ? "—" : summary ? `${summary.winRatePercentage}%` : "—"}</div>
            </div>
          </div>
        </div>
      </div>

      {!isLoading && summary && summary.stageBreakdown.length > 0 && (
        <div className="card shadow-sm border-0 mt-3">
          <div className="card-header bg-white fw-semibold">Pipeline by Stage</div>
          <div className="card-body">
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
          </div>
        </div>
      )}

      <p className="text-muted small mt-4">
        Use the Leads, Opportunities, and Customers sections to work your pipeline day to day.
      </p>
    </div>
  );
}
