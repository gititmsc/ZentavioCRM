import { useEffect, useState } from "react";
import { auditLogService, type AuditLogEntry } from "@/services/auditLogService";

/** Shared "History" card — reused by any detail screen that wants an audit trail (Lead, Opportunity, ...). */
export function HistoryPanel({ entityType, entityId }: { entityType: string; entityId: string }) {
  const [entries, setEntries] = useState<AuditLogEntry[]>([]);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    (async () => {
      setIsLoading(true);
      const result = await auditLogService.getForEntity(entityType, entityId);
      setIsLoading(false);
      if (result.success && result.data) setEntries(result.data);
    })();
  }, [entityType, entityId]);

  return (
    <div className="card shadow-sm border-0 mt-4">
      <div className="card-header bg-white fw-semibold">History</div>
      <div className="card-body">
        {isLoading && <div className="text-muted small">Loading...</div>}
        {!isLoading && entries.length === 0 && <div className="text-muted small">No history yet.</div>}
        <ul className="list-unstyled mb-0">
          {entries.map((entry) => (
            <li key={entry.id} className="border-bottom py-2">
              <div className="d-flex justify-content-between">
                <span>{entry.summary}</span>
                <span className="text-muted small">{new Date(entry.createdAtUtc).toLocaleString()}</span>
              </div>
              <div className="text-muted small">{entry.performedByUserName ?? "System"}</div>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
