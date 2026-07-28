import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { territoryService, type Territory } from "@/services/territoryService";
import { PermissionCodes } from "@/services/permissionCodes";

export default function TerritoriesList() {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const canManage = hasPermission(PermissionCodes.TerritoriesManage);

  const [territories, setTerritories] = useState<Territory[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setIsLoading(true);
    const result = await territoryService.getAll();
    setIsLoading(false);
    if (!result.success || !result.data) {
      setError(result.message || "Unable to load territories.");
      return;
    }
    setTerritories(result.data);
  };

  useEffect(() => {
    load();
  }, []);

  const handleDelete = async (territory: Territory) => {
    if (!window.confirm(`Delete territory "${territory.name}"?`)) {
      return;
    }
    const result = await territoryService.remove(territory.id);
    if (!result.success) {
      window.alert(result.message || "Unable to delete territory.");
      return;
    }
    load();
  };

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

      {error && <div className="alert alert-danger">{error}</div>}

      <div className="card shadow-sm border-0">
        <div className="table-responsive">
          <table className="table table-hover align-middle mb-0">
            <thead className="table-light">
              <tr>
                <th>Name</th>
                <th>Parent Territory</th>
                <th>Users</th>
                <th>Leads</th>
                <th>Status</th>
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
              {!isLoading && territories.length === 0 && (
                <tr>
                  <td colSpan={6} className="text-center text-muted py-4">
                    No territories yet.
                  </td>
                </tr>
              )}
              {territories.map((territory) => (
                <tr key={territory.id}>
                  <td>{territory.name}</td>
                  <td>{territory.parentTerritoryName ?? <span className="text-muted">&mdash;</span>}</td>
                  <td>{territory.userCount}</td>
                  <td>{territory.leadCount}</td>
                  <td>
                    <span className={`badge ${territory.isActive ? "text-bg-success" : "text-bg-secondary"}`}>
                      {territory.isActive ? "Active" : "Inactive"}
                    </span>
                  </td>
                  {canManage && (
                    <td className="text-end">
                      <Link to={`/territories/${territory.id}/edit`} className="btn btn-sm btn-outline-secondary me-2">
                        Edit
                      </Link>
                      <button
                        type="button"
                        className="btn btn-sm btn-outline-danger"
                        onClick={() => handleDelete(territory)}
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
