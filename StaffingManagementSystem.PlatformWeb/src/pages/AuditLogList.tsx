import { useEffect, useState } from "react";
import { auditLogService, type PlatformAuditLogEntry } from "@/services/auditLogService";

export default function AuditLogList() {
  const [entries, setEntries] = useState<PlatformAuditLogEntry[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      setIsLoading(true);
      const result = await auditLogService.getAll();
      setIsLoading(false);
      if (!result.success || !result.data) {
        setError(result.message || "Unable to load the audit log.");
        return;
      }
      setEntries(result.data);
    })();
  }, []);

  return (
    <div>
      <h1 className="h4 mb-4">Audit Log</h1>

      {error && <div className="alert alert-danger">{error}</div>}

      <div className="card shadow-sm border-0">
        <div className="table-responsive">
          <table className="table table-hover align-middle mb-0">
            <thead className="table-light">
              <tr>
                <th>When</th>
                <th>Admin</th>
                <th>Action</th>
                <th>Tenant</th>
                <th>Summary</th>
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
              {!isLoading && entries.length === 0 && !error && (
                <tr>
                  <td colSpan={5} className="text-center text-muted py-4">
                    No audit log entries found.
                  </td>
                </tr>
              )}
              {entries.map((e) => (
                <tr key={e.id}>
                  <td className="text-nowrap">{new Date(e.createdAtUtc).toLocaleString()}</td>
                  <td>{e.platformAdminEmail ?? "—"}</td>
                  <td>{e.action}</td>
                  <td>{e.tenantName ?? "—"}</td>
                  <td>{e.summary}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
