import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { roleService, type Role, type RoleSearchParams, type VisibilityScope } from "@/services/roleService";
import { PermissionCodes } from "@/services/permissionCodes";
import { PageHeader } from "@/components/layout/PageHeader";
import { DataTable, type DataTableColumn } from "@/components/datatable/DataTable";
import { Pagination } from "@/components/datatable/Pagination";
import { usePagedList } from "@/hooks/usePagedList";

const VISIBILITY_SCOPE_LABEL: Record<VisibilityScope, string> = {
  Own: "Own only",
  Team: "Team",
  All: "All",
};

const VISIBILITY_SCOPE_BADGE_CLASS: Record<VisibilityScope, string> = {
  Own: "text-bg-warning",
  Team: "text-bg-primary",
  All: "text-bg-secondary",
};

export default function RolesList() {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const canManage = hasPermission(PermissionCodes.RolesManage);

  const [search, setSearch] = useState("");

  const {
    items: roles,
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
  } = usePagedList<Role, RoleSearchParams>(
    roleService.search,
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

  const handleDelete = async (role: Role) => {
    if (!window.confirm(`Delete role "${role.name}"?`)) return;
    const result = await roleService.remove(role.id);
    if (!result.success) {
      window.alert(result.message || "Unable to delete role.");
      return;
    }
    reload();
  };

  const columns: DataTableColumn<Role>[] = [
    { key: "name", header: "Name", render: (role) => role.name },
    {
      key: "description",
      header: "Description",
      render: (role) => role.description ?? <span className="text-muted">&mdash;</span>,
    },
    {
      key: "visibilityScope",
      header: "Visibility",
      render: (role) => (
        <span className={`badge ${VISIBILITY_SCOPE_BADGE_CLASS[role.visibilityScope]}`}>
          {VISIBILITY_SCOPE_LABEL[role.visibilityScope]}
        </span>
      ),
    },
    { key: "permissionCount", header: "Permissions", render: (role) => role.permissionCodes.length },
    {
      key: "isSystemRole",
      header: "Type",
      render: (role) => (
        <span className={`badge ${role.isSystemRole ? "text-bg-secondary" : "text-bg-info"}`}>
          {role.isSystemRole ? "System" : "Custom"}
        </span>
      ),
    },
    ...(canManage
      ? [
          {
            header: "",
            align: "end" as const,
            render: (role: Role) =>
              !role.isSystemRole && (
                <>
                  <Link
                    to={`/roles/${role.id}/edit`}
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
                      handleDelete(role);
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
      <PageHeader
        title="Roles"
        subtitle="Permission sets assigned to users."
        actions={
          canManage && (
            <button type="button" className="btn btn-primary" onClick={() => navigate("/roles/new")}>
              <i className="bi bi-plus-lg me-1" aria-hidden="true" />
              New Role
            </button>
          )
        }
      />

      <div className="card shadow-sm border-0 p-3 mb-3">
        <form className="d-flex" style={{ maxWidth: 360 }} onSubmit={handleSearchSubmit}>
          <input
            className="form-control me-2"
            placeholder="Search roles..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <button type="submit" className="btn btn-outline-secondary">
            <i className="bi bi-search" aria-hidden="true" />
          </button>
        </form>
      </div>

      {error && <div className="alert alert-danger">{error}</div>}

      <DataTable
        columns={columns}
        items={roles}
        rowKey={(role) => role.id}
        isLoading={isLoading}
        emptyMessage="No roles yet."
        emptyIcon="bi-shield-lock"
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
