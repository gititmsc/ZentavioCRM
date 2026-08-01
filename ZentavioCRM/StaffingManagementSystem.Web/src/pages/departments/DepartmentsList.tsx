import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { departmentService, type Department, type DepartmentSearchParams } from "@/services/departmentService";
import { PermissionCodes } from "@/services/permissionCodes";
import { DataTable, type DataTableColumn } from "@/components/datatable/DataTable";
import { Pagination } from "@/components/datatable/Pagination";
import { usePagedList } from "@/hooks/usePagedList";

export default function DepartmentsList() {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const canManage = hasPermission(PermissionCodes.DepartmentsManage);

  const [search, setSearch] = useState("");

  const {
    items: departments,
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
  } = usePagedList<Department, DepartmentSearchParams>(
    departmentService.search,
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

  const handleDelete = async (department: Department) => {
    if (!window.confirm(`Delete department "${department.name}"?`)) {
      return;
    }
    const result = await departmentService.remove(department.id);
    if (!result.success) {
      window.alert(result.message || "Unable to delete department.");
      return;
    }
    reload();
  };

  const columns: DataTableColumn<Department>[] = [
    { key: "name", header: "Name", render: (d) => d.name },
    {
      key: "parentDepartmentName",
      header: "Parent Department",
      render: (d) => d.parentDepartmentName ?? <span className="text-muted">&mdash;</span>,
    },
    { key: "userCount", header: "Users", render: (d) => d.userCount },
    {
      key: "isActive",
      header: "Status",
      render: (d) => (
        <span className={`badge ${d.isActive ? "text-bg-success" : "text-bg-secondary"}`}>
          {d.isActive ? "Active" : "Inactive"}
        </span>
      ),
    },
    ...(canManage
      ? [
          {
            header: "",
            align: "end" as const,
            render: (department: Department) => (
              <>
                <Link
                  to={`/departments/${department.id}/edit`}
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
                    handleDelete(department);
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
        <h1 className="h4 mb-0">Departments</h1>
        {canManage && (
          <button type="button" className="btn btn-primary" onClick={() => navigate("/departments/new")}>
            <i className="bi bi-plus-lg me-1" aria-hidden="true" />
            New Department
          </button>
        )}
      </div>

      <form className="d-flex mb-3" style={{ maxWidth: 360 }} onSubmit={handleSearchSubmit}>
        <input
          className="form-control me-2"
          placeholder="Search departments..."
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
        items={departments}
        rowKey={(d) => d.id}
        isLoading={isLoading}
        emptyMessage="No departments yet."
        emptyIcon="bi-diagram-3"
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
