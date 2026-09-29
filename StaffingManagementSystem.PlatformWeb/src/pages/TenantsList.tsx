import { useEffect, useState } from "react";
import { tenantService, type Tenant } from "@/services/tenantService";
import { usePlatformAuth } from "@/context/PlatformAuthContext";

const STATUS_BADGE: Record<Tenant["status"], string> = {
  PendingActivation: "text-bg-secondary",
  Active: "text-bg-success",
  Suspended: "text-bg-warning",
  Stopped: "text-bg-danger",
};

export default function TenantsList() {
  const { admin, logout } = usePlatformAuth();
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      setIsLoading(true);
      const result = await tenantService.getAll();
      setIsLoading(false);
      if (!result.success || !result.data) {
        setError(result.message || "Unable to load tenants.");
        return;
      }
      setTenants(result.data);
    })();
  }, []);

  return (
    <div className="container py-4">
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h1 className="h4 mb-0">Tenants</h1>
          <span className="text-muted small">Signed in as {admin?.fullName}</span>
        </div>
        <button type="button" className="btn btn-outline-secondary btn-sm" onClick={logout}>
          Sign Out
        </button>
      </div>

      {error && <div className="alert alert-danger">{error}</div>}

      <div className="card shadow-sm border-0">
        <div className="table-responsive">
          <table className="table table-hover align-middle mb-0">
            <thead className="table-light">
              <tr>
                <th>Name</th>
                <th>Subdomain</th>
                <th>Plan</th>
                <th>Status</th>
                <th>Admin Email</th>
                <th>Created</th>
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
              {!isLoading && tenants.length === 0 && !error && (
                <tr>
                  <td colSpan={6} className="text-center text-muted py-4">
                    No tenants found.
                  </td>
                </tr>
              )}
              {tenants.map((t) => (
                <tr key={t.id}>
                  <td>{t.name}</td>
                  <td>{t.subdomain}</td>
                  <td>{t.planTier}</td>
                  <td>
                    <span className={`badge ${STATUS_BADGE[t.status]}`}>{t.status}</span>
                  </td>
                  <td>{t.adminEmail}</td>
                  <td>{new Date(t.createdAtUtc).toLocaleDateString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
