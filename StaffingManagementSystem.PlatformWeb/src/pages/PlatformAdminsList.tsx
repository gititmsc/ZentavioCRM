import { useEffect, useState } from "react";
import { platformAdminService } from "@/services/platformAdminService";
import type { PlatformAdmin } from "@/services/platformAuthService";

export default function PlatformAdminsList() {
  const [admins, setAdmins] = useState<PlatformAdmin[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      setIsLoading(true);
      const result = await platformAdminService.getAll();
      setIsLoading(false);
      if (!result.success || !result.data) {
        setError(result.message || "Unable to load platform admins.");
        return;
      }
      setAdmins(result.data);
    })();
  }, []);

  return (
    <div>
      <h1 className="h4 mb-4">Platform Admins</h1>

      {error && <div className="alert alert-danger">{error}</div>}

      <div className="card shadow-sm border-0">
        <div className="table-responsive">
          <table className="table table-hover align-middle mb-0">
            <thead className="table-light">
              <tr>
                <th>Name</th>
                <th>Email</th>
                <th>Status</th>
                <th>Last Login</th>
                <th>Created</th>
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
              {!isLoading && admins.length === 0 && !error && (
                <tr>
                  <td colSpan={5} className="text-center text-muted py-4">
                    No platform admins found.
                  </td>
                </tr>
              )}
              {admins.map((a) => (
                <tr key={a.id}>
                  <td>{a.fullName}</td>
                  <td>{a.email}</td>
                  <td>
                    <span className={`badge ${a.isActive ? "text-bg-success" : "text-bg-secondary"}`}>
                      {a.isActive ? "Active" : "Inactive"}
                    </span>
                  </td>
                  <td>{a.lastLoginAtUtc ? new Date(a.lastLoginAtUtc).toLocaleString() : "—"}</td>
                  <td>{new Date(a.createdAtUtc).toLocaleDateString()}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
