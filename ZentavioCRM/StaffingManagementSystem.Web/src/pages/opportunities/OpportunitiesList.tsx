import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { opportunityService, type OpportunityListItem, type OpportunityStage } from "@/services/opportunityService";
import { PermissionCodes } from "@/services/permissionCodes";

const STAGES: OpportunityStage[] = [
  "Qualification",
  "Discovery",
  "Proposal",
  "Negotiation",
  "VerbalCommit",
  "ClosedWon",
  "ClosedLost",
];

const STAGE_BADGE: Record<OpportunityStage, string> = {
  Qualification: "text-bg-secondary",
  Discovery: "text-bg-info",
  Proposal: "text-bg-info",
  Negotiation: "text-bg-warning",
  VerbalCommit: "text-bg-warning",
  ClosedWon: "text-bg-success",
  ClosedLost: "text-bg-danger",
};

export default function OpportunitiesList() {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const canCreate = hasPermission(PermissionCodes.OpportunitiesCreate);

  const [opportunities, setOpportunities] = useState<OpportunityListItem[]>([]);
  const [search, setSearch] = useState("");
  const [stage, setStage] = useState<OpportunityStage | "">("");
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const pageSize = 20;

  const load = async (searchTerm: string, stageFilter: OpportunityStage | "", pageNumber: number) => {
    setIsLoading(true);
    const result = await opportunityService.search({
      search: searchTerm || undefined,
      stage: stageFilter || undefined,
      page: pageNumber,
      pageSize,
    });
    setIsLoading(false);
    if (!result.success || !result.data) {
      setError(result.message || "Unable to load opportunities.");
      return;
    }
    setOpportunities(result.data.items);
    setTotalCount(result.data.totalCount);
  };

  useEffect(() => {
    load(search, stage, page);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, stage]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
    load(search, stage, 1);
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h1 className="h4 mb-0">Opportunities</h1>
        {canCreate && (
          <button type="button" className="btn btn-primary" onClick={() => navigate("/opportunities/new")}>
            <i className="bi bi-plus-lg me-1" aria-hidden="true" />
            New Opportunity
          </button>
        )}
      </div>

      <div className="d-flex gap-2 mb-3">
        <form className="d-flex" style={{ maxWidth: 320 }} onSubmit={handleSearchSubmit}>
          <input
            className="form-control me-2"
            placeholder="Search opportunities..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <button type="submit" className="btn btn-outline-secondary">
            <i className="bi bi-search" aria-hidden="true" />
          </button>
        </form>

        <select
          className="form-select"
          style={{ maxWidth: 200 }}
          value={stage}
          onChange={(e) => {
            setStage(e.target.value as OpportunityStage | "");
            setPage(1);
          }}
        >
          <option value="">All stages</option>
          {STAGES.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </select>
      </div>

      {error && <div className="alert alert-danger">{error}</div>}

      <div className="card shadow-sm border-0">
        <div className="table-responsive">
          <table className="table table-hover align-middle mb-0">
            <thead className="table-light">
              <tr>
                <th>Number</th>
                <th>Opportunity</th>
                <th>Customer</th>
                <th>Value</th>
                <th>Close Date</th>
                <th>Owner</th>
                <th>Stage</th>
              </tr>
            </thead>
            <tbody>
              {isLoading && (
                <tr>
                  <td colSpan={7} className="text-center text-muted py-4">
                    Loading...
                  </td>
                </tr>
              )}
              {!isLoading && opportunities.length === 0 && (
                <tr>
                  <td colSpan={7} className="text-center text-muted py-4">
                    No opportunities found.
                  </td>
                </tr>
              )}
              {opportunities.map((opportunity) => (
                <tr key={opportunity.id} role="button" onClick={() => navigate(`/opportunities/${opportunity.id}`)}>
                  <td>{opportunity.opportunityNumber}</td>
                  <td>{opportunity.name}</td>
                  <td>{opportunity.customerName}</td>
                  <td>
                    {opportunity.value != null
                      ? `${opportunity.currencyCode} ${opportunity.value.toLocaleString()}`
                      : "—"}
                  </td>
                  <td>{opportunity.expectedCloseDate ? new Date(opportunity.expectedCloseDate).toLocaleDateString() : "—"}</td>
                  <td>{opportunity.assignedToUserName ?? <span className="text-muted">Unassigned</span>}</td>
                  <td>
                    <span className={`badge ${STAGE_BADGE[opportunity.stage]}`}>{opportunity.stage}</span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {totalPages > 1 && (
        <div className="d-flex justify-content-between align-items-center mt-3">
          <span className="text-muted small">
            Page {page} of {totalPages} &middot; {totalCount} total
          </span>
          <div className="btn-group">
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
            >
              Previous
            </button>
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              Next
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
