import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { territoryService, type Territory, type TerritorySearchParams } from "@/services/territoryService";
import { PermissionCodes } from "@/services/permissionCodes";
import { DataTable, type DataTableColumn } from "@/components/datatable/DataTable";
import { Pagination } from "@/components/datatable/Pagination";
import { usePagedList } from "@/hooks/usePagedList";

export default function TerritoriesList() {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const canManage = hasPermission(PermissionCodes.TerritoriesManage);

  const [search, setSearch] = useState("");

  const {
    items: territories,
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
  } = usePagedList<Territory, TerritorySearchParams>(
    territoryService.search,
    ({ page, pageSize, sortBy, sortDescending }) => ({
      search: search || undefined,
      page,
      pageSize,
      sortBy,
      sortDescending,
    }),
    [search],
    { defaultSortDescending: false }
  );

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    resetToFirstPage();
  };

  const handleDelete = async (territory: Territory) => {
    if (!window.confirm(`Delete territory "${territory.name}"?`)) {
      return;
    }
    const result = await territoryService.remove(territory.id);
    if (!result.success) {
      window.alert(result.message || "Unable to delete territory.");
      return;
    }
    reload();
  };

  const columns: DataTableColumn<Territory>[] = [
    { key: "name", header: "Name", render: (t) => t.name },
    {
      key: "parentTerritoryName",
      header: "Parent Territory",
      render: (t) => t.parentTerritoryName ?? <span className="text-muted">&mdash;</span>,
    },
    { key: "userCount", header: "Users", render: (t) => t.userCount },
    { key: "leadCount", header: "Leads", render: (t) => t.leadCount },
    {
      key: "isActive",
      header: "Status",
      render: (t) => (
        <span className={`badge ${t.isActive ? "text-bg-success" : "text-bg-secondary"}`}>
          {t.isActive ? "Active" : "Inactive"}
        </span>
      ),
    },
    ...(canManage
      ? [
          {
            header: "",
            align: "end" as const,
            render: (territory: Territory) => (
              <>
                <Link
                  to={`/territories/${territory.id}/edit`}
                  className="btn btn-sm btn-outline-secondary me-2"
                  onClick={(e) => e.stopPropagation()}
                >
                  Edit
                </Link>
                <button
                  type="button"
                  className="btn btn-sm btn-outline-danger"
                  onClick={(e) => {
                    e.stopPropagation();
                    handleDelete(territory);
                  }}
                >
                  Delete
                </button>
              </>
            ),
          },
        ]
      : []),
  ];

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h1 className="h4 mb-0">Territories</h1>
        {canManage && (
          <button type="button" className="btn btn-primary" onClick={() => navigate("/territories/new")}>
            <i className="bi bi-plus-lg me-1" aria-hidden="true" />
            New Territory
          </button>
        )}
      </div>

      <form className="d-flex mb-3" style={{ maxWidth: 360 }} onSubmit={handleSearchSubmit}>
        <input
          className="form-control me-2"
          placeholder="Search territories..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <button type="submit" className="btn btn-outline-secondary">
          <i className="bi bi-search" aria-hidden="true" />
        </button>
      </form>

      {error && <div className="alert alert-danger">{error}</div>}

      <DataTable
        columns={columns}
        items={territories}
        rowKey={(t) => t.id}
        isLoading={isLoading}
        emptyMessage="No territories yet."
        emptyIcon="bi-geo-alt"
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
