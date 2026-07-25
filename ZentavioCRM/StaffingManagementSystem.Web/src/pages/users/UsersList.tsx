import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { userService, type ManagedUser } from "@/services/userService";
import { PermissionCodes } from "@/services/permissionCodes";

export default function UsersList() {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const canManage = hasPermission(PermissionCodes.UsersManage);

  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      setIsLoading(true);
      const result = await userService.getAll();
      setIsLoading(false);
      if (!result.success || !result.data) {
        setError(result.message || "Unable to load users.");
        return;
      }
      setUsers(result.data);
    })();
  }, []);

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h1 className="h4 mb-0">Users</h1>
        {canManage && (
          <button type="button" className="btn btn-primary" onClick={() => navigate("/users/new")}>
            <i className="bi bi-plus-lg me-1" aria-hidden="true" />
            New User
          </button>
        )}
      </div>

      {error && <div className="alert alert-danger">{error}</div>}

      <div className="card shadow-sm border-0">
        <div className="table-responsive">
          <table className="table table-hover align-middle mb-0">
            <thead className="table-light">
              <tr>
                <th>Employee Code</th>
                <th>Name</th>
                <th>Email</th>
                <th>Role</th>
                <th>Department</th>
                <th>Status</th>
                {canManage && <th className="text-end">Actions</th>}
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
              {!isLoading && users.length === 0 && (
                <tr>
                  <td colSpan={7} className="text-center text-muted py-4">
                    No users yet.
                  </td>
                </tr>
              )}
              {users.map((user) => (
                <tr key={user.id}>
                  <td>{user.employeeCode}</td>
                  <td>{user.fullName}</td>
                  <td>{user.email}</td>
                  <td>{user.roleName}</td>
                  <td>{user.departmentName ?? <span className="text-muted">&mdash;</span>}</td>
                  <td>
                    <span className={`badge ${user.isActive ? "text-bg-success" : "text-bg-secondary"}`}>
                      {user.isActive ? "Active" : "Inactive"}
                    </span>
                  </td>
                  {canManage && (
                    <td className="text-end">
                      <Link to={`/users/${user.id}/edit`} className="btn btn-sm btn-outline-secondary">
                        Edit
                      </Link>
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
