import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import {
  quotationService,
  type QuotationListItem,
  type QuotationSearchParams,
  type QuotationStatus,
} from "@/services/quotationService";
import { PermissionCodes } from "@/services/permissionCodes";
import { DataTable, type DataTableColumn } from "@/components/datatable/DataTable";
import { Pagination } from "@/components/datatable/Pagination";
import { usePagedList } from "@/hooks/usePagedList";

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

  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<QuotationStatus | "">("");

  const {
    items: quotations,
    totalCount,
    totalPages,
    page,
    pageSize,
    sortBy,
    sortDescending,
    isLoading,
    error,
    setPage,
    setPageSize,
    onSortChange,
    resetToFirstPage,
  } = usePagedList<QuotationListItem, QuotationSearchParams>(
    quotationService.search,
    ({ page, pageSize, sortBy, sortDescending }) => ({
      search: search || undefined,
      status: status || undefined,
      page,
      pageSize,
      sortBy,
      sortDescending,
    }),
    [search, status]
  );

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    resetToFirstPage();
  };

  const columns: DataTableColumn<QuotationListItem>[] = [
    {
      key: "quotationNumber",
      header: "Number",
      render: (q) => (
        <>
          {q.quotationNumber}
          {q.version > 1 && <span className="text-muted"> v{q.version}</span>}
        </>
      ),
    },
    { key: "opportunityName", header: "Opportunity", render: (q) => q.opportunityName },
    { key: "customerName", header: "Customer", render: (q) => q.customerName },
    { key: "grandTotal", header: "Total", align: "end", render: (q) => q.grandTotal.toLocaleString() },
    {
      key: "validUntil",
      header: "Valid Until",
      render: (q) => (q.validUntil ? new Date(q.validUntil).toLocaleDateString() : <span className="text-muted">&mdash;</span>),
    },
    {
      key: "assignedToUserName",
      header: "Owner",
      render: (q) => q.assignedToUserName ?? <span className="text-muted">Unassigned</span>,
    },
    {
      key: "status",
      header: "Status",
      render: (q) => <span className={`badge ${STATUS_BADGE[q.status]}`}>{q.status}</span>,
    },
  ];

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
            resetToFirstPage();
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

      <DataTable
        columns={columns}
        items={quotations}
        rowKey={(q) => q.id}
        isLoading={isLoading}
        emptyMessage="No quotations found."
        emptyIcon="bi-file-earmark-text"
        onRowClick={(q) => navigate(`/quotations/${q.id}`)}
        sortBy={sortBy}
        sortDescending={sortDescending}
        onSortChange={onSortChange}
      />

      <Pagination
        page={page}
        pageSize={pageSize}
        totalCount={totalCount}
        totalPages={totalPages}
        onPageChange={setPage}
        onPageSizeChange={setPageSize}
      />
    </div>
  );
}
