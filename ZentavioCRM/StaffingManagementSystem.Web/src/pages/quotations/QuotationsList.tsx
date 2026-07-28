import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { quotationService, type QuotationListItem, type QuotationStatus } from "@/services/quotationService";
import { PermissionCodes } from "@/services/permissionCodes";

const STATUSES: QuotationStatus[] = ["Draft", "Sent", "Accepted", "Rejected", "Expired"];

const STATUS_BADGE: Record<QuotationStatus, string> = {
  Draft: "text-bg-secondary",
  Sent: "text-bg-info",
  Accepted: "text-bg-success",
  Rejected: "text-bg-danger",
  Expired: "text-bg-dark",
};

export default function QuotationsList() {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const canCreate = hasPermission(PermissionCodes.QuotationsCreate);

  const [quotations, setQuotations] = useState<QuotationListItem[]>([]);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<QuotationStatus | "">("");
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const pageSize = 20;

  const load = async (searchTerm: string, statusFilter: QuotationStatus | "", pageNumber: number) => {
    setIsLoading(true);
    const result = await quotationService.search({
      search: searchTerm || undefined,
      status: statusFilter || undefined,
      page: pageNumber,
      pageSize,
    });
    setIsLoading(false);
    if (!result.success || !result.data) {
      setError(result.message || "Unable to load quotations.");
      return;
    }
    setQuotations(result.data.items);
    setTotalCount(result.data.totalCount);
  };

  useEffect(() => {
    load(search, status, page);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, status]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
    load(search, status, 1);
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h1 className="h4 mb-0">Quotations</h1>
        {canCreate && (
          <button type="button" className="btn btn-primary" onClick={() => navigate("/quotations/new")}>
            <i className="bi bi-plus-lg me-1" aria-hidden="true" />
            New Quotation
          </button>
        )}
      </div>

      <div className="d-flex gap-2 mb-3">
        <form className="d-flex" style={{ maxWidth: 320 }} onSubmit={handleSearchSubmit}>
          <input
            className="form-control me-2"
            placeholder="Search quotations..."
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
          value={status}
          onChange={(e) => {
            setStatus(e.target.value as QuotationStatus | "");
            setPage(1);
          }}
        >
          <option value="">All statuses</option>
          {STATUSES.map((s) => (
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
                <th>Total</th>
                <th>Valid Until</th>
                <th>Owner</th>
                <th>Status</th>
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
              {!isLoading && quotations.length === 0 && (
                <tr>
                  <td colSpan={7} className="text-center text-muted py-4">
                    No quotations found.
                  </td>
                </tr>
              )}
              {quotations.map((quotation) => (
                <tr key={quotation.id} role="button" onClick={() => navigate(`/quotations/${quotation.id}`)}>
                  <td>
                    {quotation.quotationNumber}
                    {quotation.version > 1 && <span className="text-muted"> v{quotation.version}</span>}
                  </td>
                  <td>{quotation.opportunityName}</td>
                  <td>{quotation.customerName}</td>
                  <td>{quotation.grandTotal.toLocaleString()}</td>
                  <td>{quotation.validUntil ? new Date(quotation.validUntil).toLocaleDateString() : "—"}</td>
                  <td>{quotation.assignedToUserName ?? <span className="text-muted">Unassigned</span>}</td>
                  <td>
                    <span className={`badge ${STATUS_BADGE[quotation.status]}`}>{quotation.status}</span>
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
