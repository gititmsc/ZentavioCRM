import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { roleService, type Role, type VisibilityScope } from "@/services/roleService";
import { PermissionCodes } from "@/services/permissionCodes";

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

  const [roles, setRoles] = useState<Role[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setIsLoading(true);
    const result = await roleService.getAll();
    setIsLoading(false);
    if (!result.success || !result.data) {
      setError(result.message || "Unable to load roles.");
      return;
    }
    setRoles(result.data);
  };

  useEffect(() => {
    load();
  }, []);

  const handleDelete = async (role: Role) => {
    if (!window.confirm(`Delete role "${role.name}"?`)) return;
    const result = await roleService.remove(role.id);
    if (!result.success) {
      window.alert(result.message || "Unable to delete role.");
      return;
    }
    load();
  };

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h1 className="h4 mb-0">Roles</h1>
        {canManage && (
          <button type="button" className="btn btn-primary" onClick={() => navigate("/roles/new")}>
            <i className="bi bi-plus-lg me-1" aria-hidden="true" />
            New Role
          </button>
        )}
      </div>

      {error && <div className="alert alert-danger">{error}</div>}

      <div className="card shadow-sm border-0">
        <div className="table-responsive">
          <table className="table table-hover align-middle mb-0">
            <thead className="table-light">
              <tr>
                <th>Name</th>
                <th>Description</th>
                <th>Visibility</th>
                <th>Permissions</th>
                <th>Type</th>
                {canManage && <th className="text-end">Actions</th>}
              </tr>
            </thead>
            <tbody>
              {isLoading && (
                <tr>
                  <td colSpan={6} className="text-center text-muted py-4">
                    Loading...
                  </td>
                </tr>
              )}
              {roles.map((role) => (
                <tr key={role.id}>
                  <td>{role.name}</td>
                  <td>{role.description ?? <span className="text-muted">&mdash;</span>}</td>
                  <td>
                    <span className={`badge ${VISIBILITY_SCOPE_BADGE_CLASS[role.visibilityScope]}`}>
                      {VISIBILITY_SCOPE_LABEL[role.visibilityScope]}
                    </span>
                  </td>
                  <td>{role.permissionCodes.length}</td>
                  <td>
                    <span className={`badge ${role.isSystemRole ? "text-bg-secondary" : "text-bg-info"}`}>
                      {role.isSystemRole ? "System" : "Custom"}
                    </span>
                  </td>
                  {canManage && (
                    <td className="text-end">
                      {!role.isSystemRole && (
                        <>
                          <Link to={`/roles/${role.id}/edit`} className="btn btn-sm btn-outline-secondary me-2">
                            Edit
                          </Link>
                          <button
                            type="button"
                            className="btn btn-sm btn-outline-danger"
                            onClick={() => handleDelete(role)}
                          >
                            Delete
                          </button>
                        </>
                      )}
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
