import { useEffect, useState } from "react";
import { PageHeader } from "@/components/PageHeader";
import { auditLogService, type PlatformAuditLogEntry } from "@/services/auditLogService";

const ACTION_META: Record<string, { icon: string; tint: string }> = {
  Login: { icon: "bi-box-arrow-in-right", tint: "var(--itm-muted)" },
  TenantProvisioned: { icon: "bi-plus-circle-fill", tint: "var(--itm-success)" },
  TenantSuspended: { icon: "bi-pause-circle-fill", tint: "var(--itm-warning)" },
  TenantReactivated: { icon: "bi-check-circle-fill", tint: "var(--itm-success)" },
  TenantStopped: { icon: "bi-stop-circle-fill", tint: "var(--itm-danger)" },
  TenantPlanChanged: { icon: "bi-arrow-repeat", tint: "var(--itm-accent)" },
  TenantImpersonated: { icon: "bi-person-badge-fill", tint: "var(--itm-accent)" },
  TenantMetadataUpdated: { icon: "bi-pencil-fill", tint: "var(--itm-accent)" },
  TenantBillingUpdated: { icon: "bi-receipt", tint: "var(--itm-teal)" },
  PaymentRecorded: { icon: "bi-cash-coin", tint: "var(--itm-success)" },
  TenantNoteAdded: { icon: "bi-sticky-fill", tint: "var(--itm-muted)" },
};

const DEFAULT_META = { icon: "bi-info-circle-fill", tint: "var(--itm-muted)" };

export function AuditLogPage() {
  const [entries, setEntries] = useState<PlatformAuditLogEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void (async () => {
      setLoading(true);
      const response = await auditLogService.getAll();
      if (response.success && response.data) {
        setEntries(response.data);
        setError(null);
      } else {
        setError(response.message || "Could not load audit log.");
      }
      setLoading(false);
    })();
  }, []);

  return (
    <div>
      <PageHeader
        eyebrow="History"
        icon="bi-clock-history"
        title="Platform Audit Log"
        subtitle="Every login, provision, suspend, reactivate, stop, plan change and impersonation — in order."
      />

      {error && <div className="alert alert-danger">{error}</div>}

      <div className="app-card">
        <div className="list-group list-group-flush">
          {loading ? (
            <div className="text-center text-muted py-5">Loading...</div>
          ) : entries.length === 0 ? (
            <div className="empty-state">
              <div className="empty-state__icon">
                <i className="bi bi-clock-history" aria-hidden="true" />
              </div>
              <div className="empty-state__title">No activity yet</div>
            </div>
          ) : (
            entries.map((entry) => {
              const meta = ACTION_META[entry.action] ?? DEFAULT_META;
              return (
                <div key={entry.id} className="list-group-item d-flex align-items-start gap-3 py-3">
                  <div
                    className="d-flex align-items-center justify-content-center flex-shrink-0"
                    style={{
                      width: 38,
                      height: 38,
                      borderRadius: "50%",
                      background: `color-mix(in srgb, ${meta.tint} 14%, white)`,
                      color: meta.tint,
                    }}
                  >
                    <i className={`bi ${meta.icon}`} aria-hidden="true" />
                  </div>
                  <div className="flex-grow-1">
                    <div>{entry.summary}</div>
                    <div className="text-muted small mt-1 d-flex align-items-center gap-2">
                      <span>{new Date(entry.createdAtUtc).toLocaleString()}</span>
                      {entry.tenantName && (
                        <>
                          <span>&middot;</span>
                          <span>{entry.tenantName}</span>
                        </>
                      )}
                      {entry.platformAdminEmail && (
                        <>
                          <span>&middot;</span>
                          <span>{entry.platformAdminEmail}</span>
                        </>
                      )}
                    </div>
                  </div>
                </div>
              );
            })
          )}
        </div>
      </div>
    </div>
  );
}
