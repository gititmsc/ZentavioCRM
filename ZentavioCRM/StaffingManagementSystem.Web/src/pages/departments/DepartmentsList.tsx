import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { departmentService, type Department } from "@/services/departmentService";
import { PermissionCodes } from "@/services/permissionCodes";

export default function DepartmentsList() {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const canManage = hasPermission(PermissionCodes.DepartmentsManage);

  const [departments, setDepartments] = useState<Department[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setIsLoading(true);
    const result = await departmentService.getAll();
    setIsLoading(false);
    if (!result.success || !result.data) {
      setError(result.message || "Unable to load departments.");
      return;
    }
    setDepartments(result.data);
  };

  useEffect(() => {
    load();
  }, []);

  const handleDelete = async (department: Department) => {
    if (!window.confirm(`Delete department "${department.name}"?`)) {
      return;
    }
    const result = await departmentService.remove(department.id);
    if (!result.success) {
      window.alert(result.message || "Unable to delete department.");
      return;
    }
    load();
  };

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

      {error && <div className="alert alert-danger">{error}</div>}

      <div className="card shadow-sm border-0">
        <div className="table-responsive">
          <table className="table table-hover align-middle mb-0">
            <thead className="table-light">
              <tr>
                <th>Name</th>
                <th>Parent Department</th>
                <th>Users</th>
                <th>Status</th>
                {canManage && <th className="text-end">Actions</th>}
              </tr>
            </thead>
            <tbody>
              {isLoading && (
                <tr>
                  <td colSpan={5} className="text-center text-muted py-4">
                    Loading...
                  </td>
                </tr>
              )}
              {!isLoading && departments.length === 0 && (
                <tr>
                  <td colSpan={5} className="text-center text-muted py-4">
                    No departments yet.
                  </td>
                </tr>
              )}
              {departments.map((department) => (
                <tr key={department.id}>
                  <td>{department.name}</td>
                  <td>{department.parentDepartmentName ?? <span className="text-muted">&mdash;</span>}</td>
                  <td>{department.userCount}</td>
                  <td>
                    <span className={`badge ${department.isActive ? "text-bg-success" : "text-bg-secondary"}`}>
                      {department.isActive ? "Active" : "Inactive"}
                    </span>
                  </td>
                  {canManage && (
                    <td className="text-end">
                      <Link to={`/departments/${department.id}/edit`} className="btn btn-sm btn-outline-secondary me-2">
                        Edit
                      </Link>
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-danger"
                        onClick={() => handleDelete(department)}
                      >
                        Delete
                      </button>
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
