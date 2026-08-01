import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { leadService, type LeadListItem, type LeadSearchParams, type LeadStatus } from "@/services/leadService";
import { PermissionCodes } from "@/services/permissionCodes";
import { ImportExportBar } from "@/components/import-export/ImportExportBar";
import { PageHeader } from "@/components/layout/PageHeader";
import { DataTable, type DataTableColumn } from "@/components/datatable/DataTable";
import { Pagination } from "@/components/datatable/Pagination";
import { usePagedList } from "@/hooks/usePagedList";

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

  const [search, setSearch] = useState("");
  const [status, setStatus] = useState<LeadStatus | "">("");

  const {
    items: leads,
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
    reload,
  } = usePagedList<LeadListItem, LeadSearchParams>(
    leadService.search,
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

  const columns: DataTableColumn<LeadListItem>[] = [
    { key: "leadNumber", header: "Number", render: (lead) => lead.leadNumber },
    { key: "companyName", header: "Company", render: (lead) => lead.companyName },
    { key: "contactName", header: "Contact", render: (lead) => lead.contactName },
    { key: "source", header: "Source", render: (lead) => lead.source },
    {
      key: "expectedValue",
      header: "Expected Value",
      align: "end",
      render: (lead) => (lead.expectedValue != null ? lead.expectedValue.toLocaleString() : <span className="text-muted">&mdash;</span>),
    },
    {
      key: "assignedToUserName",
      header: "Owner",
      render: (lead) => lead.assignedToUserName ?? <span className="text-muted">Unassigned</span>,
    },
    {
      key: "status",
      header: "Status",
      render: (lead) => <span className={`badge ${STATUS_BADGE[lead.status]}`}>{lead.status}</span>,
    },
  ];

  return (
    <div>
      <PageHeader
        title="Leads"
        subtitle="Track and qualify inbound and outbound leads."
        actions={
          canCreate && (
            <button type="button" className="btn btn-primary" onClick={() => navigate("/leads/new")}>
              <i className="bi bi-plus-lg me-1" aria-hidden="true" />
              New Lead
            </button>
          )
        }
      />

      <div className="card shadow-sm border-0 p-3 mb-3">
        <div className="d-flex gap-2 align-items-start">
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

          <ImportExportBar
            className="ms-auto"
            entityLabel="Leads"
            onExport={leadService.exportCsv}
            onImport={leadService.importCsv}
            onImportComplete={reload}
            sampleFileUrl="/samples/leads-import-sample.csv"
          />
        </div>
      </div>

      {error && <div className="alert alert-danger">{error}</div>}

      <DataTable
        columns={columns}
        items={leads}
        rowKey={(lead) => lead.id}
        isLoading={isLoading}
        emptyMessage="No leads found."
        emptyIcon="bi-person-lines-fill"
        onRowClick={(lead) => navigate(`/leads/${lead.id}`)}
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
