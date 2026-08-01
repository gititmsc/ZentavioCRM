import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { userService, type ManagedUser, type UserSearchParams } from "@/services/userService";
import { PermissionCodes } from "@/services/permissionCodes";
import { UserAvatar } from "@/components/users/UserAvatar";
import { PageHeader } from "@/components/layout/PageHeader";
import { DataTable, type DataTableColumn } from "@/components/datatable/DataTable";
import { Pagination } from "@/components/datatable/Pagination";
import { usePagedList } from "@/hooks/usePagedList";

export default function UsersList() {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const canManage = hasPermission(PermissionCodes.UsersManage);

  const [search, setSearch] = useState("");

  const {
    items: users,
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
  } = usePagedList<ManagedUser, UserSearchParams>(
    userService.search,
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

  const columns: DataTableColumn<ManagedUser>[] = [
    {
      header: "",
      render: (user) => <UserAvatar userId={user.id} fullName={user.fullName} hasProfilePhoto={user.hasProfilePhoto} size={32} />,
    },
    { key: "employeeCode", header: "Employee Code", render: (user) => user.employeeCode },
    { key: "fullName", header: "Name", render: (user) => user.fullName },
    { key: "email", header: "Email", render: (user) => user.email },
    { key: "roleName", header: "Role", render: (user) => user.roleName },
    {
      key: "departmentName",
      header: "Department",
      render: (user) => user.departmentName ?? <span className="text-muted">&mdash;</span>,
    },
    {
      key: "isActive",
      header: "Status",
      render: (user) => (
        <span className={`badge ${user.isActive ? "text-bg-success" : "text-bg-secondary"}`}>
          {user.isActive ? "Active" : "Inactive"}
        </span>
      ),
    },
    ...(canManage
      ? [
          {
            header: "",
            align: "end" as const,
            render: (user: ManagedUser) => (
              <Link to={`/users/${user.id}/edit`} className="btn btn-sm btn-outline-secondary" onClick={(e) => e.stopPropagation()}>
                Edit
              </Link>
            ),
          },
        ]
      : []),
  ];

  return (
    <div>
      <PageHeader
        title="Users"
        subtitle="People with access to this workspace."
        actions={
          canManage && (
            <button type="button" className="btn btn-primary" onClick={() => navigate("/users/new")}>
              <i className="bi bi-plus-lg me-1" aria-hidden="true" />
              New User
            </button>
          )
        }
      />

      <div className="card shadow-sm border-0 p-3 mb-3">
        <form className="d-flex" style={{ maxWidth: 360 }} onSubmit={handleSearchSubmit}>
          <input
            className="form-control me-2"
            placeholder="Search by name, code, email..."
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
        items={users}
        rowKey={(user) => user.id}
        isLoading={isLoading}
        emptyMessage="No users yet."
        emptyIcon="bi-people"
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
