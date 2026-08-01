import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import {
  customerService,
  type CustomerHealthStatus,
  type CustomerListItem,
  type CustomerSearchParams,
} from "@/services/customerService";
import { PermissionCodes } from "@/services/permissionCodes";
import { ImportExportBar } from "@/components/import-export/ImportExportBar";
import { PageHeader } from "@/components/layout/PageHeader";
import { DataTable, type DataTableColumn } from "@/components/datatable/DataTable";
import { Pagination } from "@/components/datatable/Pagination";
import { usePagedList } from "@/hooks/usePagedList";

const HEALTH_LABEL: Record<CustomerHealthStatus, string> = {
  Hot: "Hot Account",
  Warm: "Warm",
  Cold: "Cold",
  AtRisk: "At Risk",
};

const HEALTH_BADGE_CLASS: Record<CustomerHealthStatus, string> = {
  Hot: "text-bg-danger",
  Warm: "text-bg-warning",
  Cold: "text-bg-info",
  AtRisk: "text-bg-dark",
};

export default function CustomersList() {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const canCreate = hasPermission(PermissionCodes.CustomersCreate);

  const [search, setSearch] = useState("");

  const {
    items: customers,
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
  } = usePagedList<CustomerListItem, CustomerSearchParams>(
    customerService.search,
    ({ page, pageSize, sortBy, sortDescending }) => ({
      search: search || undefined,
      page,
      pageSize,
      sortBy,
      sortDescending,
    }),
    [search]
  );

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    resetToFirstPage();
  };

  const columns: DataTableColumn<CustomerListItem>[] = [
    { key: "customerNumber", header: "Number", render: (c) => c.customerNumber },
    { key: "displayName", header: "Name", render: (c) => c.displayName },
    { key: "type", header: "Type", render: (c) => c.type },
    { key: "industry", header: "Industry", render: (c) => c.industry ?? <span className="text-muted">&mdash;</span> },
    {
      header: "Tags",
      render: (c) =>
        c.tags ? (
          c.tags.split(",").map((tag) => (
            <span key={tag} className="badge text-bg-light border me-1">
              {tag.trim()}
            </span>
          ))
        ) : (
          <span className="text-muted">&mdash;</span>
        ),
    },
    {
      key: "healthStatus",
      header: "Health",
      render: (c) =>
        c.healthStatus ? (
          <span className={`badge ${HEALTH_BADGE_CLASS[c.healthStatus]}`}>{HEALTH_LABEL[c.healthStatus]}</span>
        ) : (
          <span className="text-muted">&mdash;</span>
        ),
    },
    {
      key: "assignedToUserName",
      header: "Owner",
      render: (c) => c.assignedToUserName ?? <span className="text-muted">Unassigned</span>,
    },
    {
      key: "isActive",
      header: "Status",
      render: (c) => (
        <span className={`badge ${c.isActive ? "text-bg-success" : "text-bg-secondary"}`}>
          {c.isActive ? "Active" : "Inactive"}
        </span>
      ),
    },
  ];

  return (
    <div>
      <PageHeader
        title="Customers"
        subtitle="Your accounts, contacts, and addresses in one place."
        actions={
          canCreate && (
            <button type="button" className="btn btn-primary" onClick={() => navigate("/customers/new")}>
              <i className="bi bi-plus-lg me-1" aria-hidden="true" />
              New Customer
            </button>
          )
        }
      />

      <div className="card shadow-sm border-0 p-3 mb-3">
        <div className="d-flex gap-2 align-items-start">
          <form className="d-flex" style={{ maxWidth: 360 }} onSubmit={handleSearchSubmit}>
            <input
              className="form-control me-2"
              placeholder="Search by name, number, email..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
            />
            <button type="submit" className="btn btn-outline-secondary">
              <i className="bi bi-search" aria-hidden="true" />
            </button>
          </form>

          <ImportExportBar
            className="ms-auto"
            entityLabel="Customers"
            onExport={customerService.exportCsv}
            onImport={customerService.importCsv}
            onImportComplete={reload}
          />
        </div>
      </div>

      {error && <div className="alert alert-danger">{error}</div>}

      <DataTable
        columns={columns}
        items={customers}
        rowKey={(c) => c.id}
        isLoading={isLoading}
        emptyMessage="No customers found."
        emptyIcon="bi-building"
        onRowClick={(c) => navigate(`/customers/${c.id}/edit`)}
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
