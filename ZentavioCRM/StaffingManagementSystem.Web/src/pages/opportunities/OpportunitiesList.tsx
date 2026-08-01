import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import {
  opportunityService,
  type OpportunityListItem,
  type OpportunitySearchParams,
  type OpportunityStage,
} from "@/services/opportunityService";
import { PermissionCodes } from "@/services/permissionCodes";
import { PageHeader } from "@/components/layout/PageHeader";
import { DataTable, type DataTableColumn } from "@/components/datatable/DataTable";
import { Pagination } from "@/components/datatable/Pagination";
import { usePagedList } from "@/hooks/usePagedList";

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

  const [search, setSearch] = useState("");
  const [stage, setStage] = useState<OpportunityStage | "">("");

  const {
    items: opportunities,
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
  } = usePagedList<OpportunityListItem, OpportunitySearchParams>(
    opportunityService.search,
    ({ page, pageSize, sortBy, sortDescending }) => ({
      search: search || undefined,
      stage: stage || undefined,
      page,
      pageSize,
      sortBy,
      sortDescending,
    }),
    [search, stage]
  );

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    resetToFirstPage();
  };

  const columns: DataTableColumn<OpportunityListItem>[] = [
    { key: "opportunityNumber", header: "Number", render: (o) => o.opportunityNumber },
    { key: "name", header: "Opportunity", render: (o) => o.name },
    { key: "customerName", header: "Customer", render: (o) => o.customerName },
    {
      key: "value",
      header: "Value",
      align: "end",
      render: (o) => (o.value != null ? `${o.currencyCode} ${o.value.toLocaleString()}` : <span className="text-muted">&mdash;</span>),
    },
    {
      key: "expectedCloseDate",
      header: "Close Date",
      render: (o) => (o.expectedCloseDate ? new Date(o.expectedCloseDate).toLocaleDateString() : <span className="text-muted">&mdash;</span>),
    },
    {
      key: "assignedToUserName",
      header: "Owner",
      render: (o) => o.assignedToUserName ?? <span className="text-muted">Unassigned</span>,
    },
    {
      key: "stage",
      header: "Stage",
      render: (o) => <span className={`badge ${STAGE_BADGE[o.stage]}`}>{o.stage}</span>,
    },
  ];

  return (
    <div>
      <PageHeader
        title="Opportunities"
        subtitle="Deals in progress across your pipeline."
        actions={
          canCreate && (
            <button type="button" className="btn btn-primary" onClick={() => navigate("/opportunities/new")}>
              <i className="bi bi-plus-lg me-1" aria-hidden="true" />
              New Opportunity
            </button>
          )
        }
      />

      <div className="card shadow-sm border-0 p-3 mb-3">
        <div className="d-flex gap-2">
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
              resetToFirstPage();
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
      </div>

      {error && <div className="alert alert-danger">{error}</div>}

      <DataTable
        columns={columns}
        items={opportunities}
        rowKey={(o) => o.id}
        isLoading={isLoading}
        emptyMessage="No opportunities found."
        emptyIcon="bi-graph-up-arrow"
        onRowClick={(o) => navigate(`/opportunities/${o.id}`)}
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
