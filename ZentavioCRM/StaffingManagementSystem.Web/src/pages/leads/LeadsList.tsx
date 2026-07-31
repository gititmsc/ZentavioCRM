import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { leadService, type LeadListItem, type LeadStatus } from "@/services/leadService";
import { PermissionCodes } from "@/services/permissionCodes";
import { ImportExportBar } from "@/components/import-export/ImportExportBar";

const STATUSES: LeadStatus[] = [
  "New",
  "Assigned",
  "Contacted",
  "Qualified",
  "Nurturing",
  "ProposalSent",
  "Converted",
  "Lost",
  "Junk",
];

const STATUS_BADGE: Record<LeadStatus, string> = {
  New: "text-bg-secondary",
  Assigned: "text-bg-info",
  Contacted: "text-bg-info",
  Qualified: "text-bg-primary",
  Nurturing: "text-bg-warning",
  ProposalSent: "text-bg-warning",
  Converted: "text-bg-success",
  Lost: "text-bg-danger",
  Junk: "text-bg-dark",
};

export default function LeadsList() {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const canCreate = hasPermission(PermissionCodes.LeadsCreate);

  const [leads, setLeads] = useState<LeadListItem[]>([]);
  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<LeadStatus | "">("");
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const pageSize = 20;

  const load = async (searchTerm: string, statusFilter: LeadStatus | "", pageNumber: number) => {
    setIsLoading(true);
    const result = await leadService.search({
      search: searchTerm || undefined,
      status: statusFilter || undefined,
      page: pageNumber,
      pageSize,
    });
    setIsLoading(false);
    if (!result.success || !result.data) {
      setError(result.message || "Unable to load leads.");
      return;
    }
    setLeads(result.data.items);
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
        <h1 className="h4 mb-0">Leads</h1>
        {canCreate && (
          <button type="button" className="btn btn-primary" onClick={() => navigate("/leads/new")}>
            <i className="bi bi-plus-lg me-1" aria-hidden="true" />
            New Lead
          </button>
        )}
      </div>

      <div className="d-flex gap-2 mb-3 align-items-start">
        <form className="d-flex" style={{ maxWidth: 320 }} onSubmit={handleSearchSubmit}>
          <input
            className="form-control me-2"
            placeholder="Search leads..."
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
            setStatus(e.target.value as LeadStatus | "");
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

        <ImportExportBar
          className="ms-auto"
          entityLabel="Leads"
          onExport={leadService.exportCsv}
          onImport={leadService.importCsv}
          onImportComplete={() => load(search, status, page)}
        />
      </div>

      {error && <div className="alert alert-danger">{error}</div>}

      <div className="card shadow-sm border-0">
        <div className="table-responsive">
          <table className="table table-hover align-middle mb-0">
            <thead className="table-light">
              <tr>
                <th>Number</th>
                <th>Company</th>
                <th>Contact</th>
                <th>Source</th>
                <th>Expected Value</th>
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
              {!isLoading && leads.length === 0 && (
                <tr>
                  <td colSpan={7} className="text-center text-muted py-4">
                    No leads found.
                  </td>
                </tr>
              )}
              {leads.map((lead) => (
                <tr key={lead.id} role="button" onClick={() => navigate(`/leads/${lead.id}`)}>
                  <td>{lead.leadNumber}</td>
                  <td>{lead.companyName}</td>
                  <td>{lead.contactName}</td>
                  <td>{lead.source}</td>
                  <td>{lead.expectedValue != null ? lead.expectedValue.toLocaleString() : "—"}</td>
                  <td>{lead.assignedToUserName ?? <span className="text-muted">Unassigned</span>}</td>
                  <td>
                    <span className={`badge ${STATUS_BADGE[lead.status]}`}>{lead.status}</span>
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
