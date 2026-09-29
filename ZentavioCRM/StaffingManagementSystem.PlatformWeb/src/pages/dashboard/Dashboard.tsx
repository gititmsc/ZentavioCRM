import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { PageHeader } from "@/components/PageHeader";
import { StatCard } from "@/components/StatCard";
import { StatusBadge } from "@/components/StatusBadge";
import { tenantService, type Tenant, PLAN_TIERS } from "@/services/tenantService";
import { auditLogService, type PlatformAuditLogEntry } from "@/services/auditLogService";
import { platformAdminService } from "@/services/platformAdminService";

const ACTION_ICONS: Record<string, string> = {
  Login: "bi-box-arrow-in-right",
  TenantProvisioned: "bi-plus-circle-fill",
  TenantSuspended: "bi-pause-circle-fill",
  TenantReactivated: "bi-check-circle-fill",
  TenantStopped: "bi-stop-circle-fill",
  TenantPlanChanged: "bi-arrow-repeat",
  TenantImpersonated: "bi-person-badge-fill",
};

export function Dashboard() {
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [recentActivity, setRecentActivity] = useState<PlatformAuditLogEntry[]>([]);
  const [adminCount, setAdminCount] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    void (async () => {
      setLoading(true);
      const [tenantsResponse, auditResponse, adminsResponse] = await Promise.all([
        tenantService.getAll(),
        auditLogService.getAll(),
        platformAdminService.getAll(),
      ]);
      if (tenantsResponse.success && tenantsResponse.data) setTenants(tenantsResponse.data);
      if (auditResponse.success && auditResponse.data) setRecentActivity(auditResponse.data.slice(0, 6));
      if (adminsResponse.success && adminsResponse.data) setAdminCount(adminsResponse.data.length);
      setLoading(false);
    })();
  }, []);

  const active = tenants.filter((t) => t.status === "Active").length;
  const suspended = tenants.filter((t) => t.status === "Suspended").length;
  const terminated = tenants.filter((t) => t.status === "Terminated").length;
  const needsAttention = tenants.filter((t) => t.status === "Failed" || t.status === "Provisioning");

  const planCounts = PLAN_TIERS.map((tier) => ({
    tier,
    count: tenants.filter((t) => t.planTier === tier).length,
  }));
  const maxPlanCount = Math.max(1, ...planCounts.map((p) => p.count));

  return (
    <div>
      <PageHeader
        eyebrow="Overview"
        icon="bi-grid-1x2-fill"
        title="Dashboard"
        subtitle="A snapshot of every tenant on the platform, right now."
      />

      <div className="row g-3 mb-4">
        <div className="col-sm-6 col-xl-3">
          <StatCard icon="bi-buildings-fill" label="Total Tenants" value={loading ? "–" : tenants.length} tint="accent" />
        </div>
        <div className="col-sm-6 col-xl-3">
          <StatCard icon="bi-check-circle-fill" label="Active" value={loading ? "–" : active} tint="success" />
        </div>
        <div className="col-sm-6 col-xl-3">
          <StatCard icon="bi-pause-circle-fill" label="Suspended / Stopped" value={loading ? "–" : suspended + terminated} tint="warning" />
        </div>
        <div className="col-sm-6 col-xl-3">
          <StatCard icon="bi-shield-lock-fill" label="Platform Admins" value={adminCount ?? "–"} tint="muted" />
        </div>
      </div>

      <div className="row g-4">
        <div className="col-lg-7">
          <div className="app-card mb-4">
            <div className="app-card__header">
              <h3 className="app-card__title">
                <i className="bi bi-clock-history" aria-hidden="true" />
                Recent Activity
              </h3>
              <Link to="/audit-log" className="small fw-semibold">
                View all
              </Link>
            </div>
            <div className="list-group list-group-flush">
              {loading ? (
                <div className="text-center text-muted py-4">Loading...</div>
              ) : recentActivity.length === 0 ? (
                <div className="empty-state">
                  <div className="empty-state__icon">
                    <i className="bi bi-clock-history" aria-hidden="true" />
                  </div>
                  <div className="empty-state__title">No activity yet</div>
                </div>
              ) : (
                recentActivity.map((entry) => (
                  <div key={entry.id} className="list-group-item d-flex align-items-start gap-3 py-3">
                    <div
                      className="d-flex align-items-center justify-content-center flex-shrink-0"
                      style={{ width: 34, height: 34, borderRadius: "50%", background: "var(--itm-bg)" }}
                    >
                      <i className={`bi ${ACTION_ICONS[entry.action] ?? "bi-info-circle-fill"} text-primary`} aria-hidden="true" />
                    </div>
                    <div className="flex-grow-1">
                      <div className="small">{entry.summary}</div>
                      <div className="text-muted" style={{ fontSize: "0.76rem" }}>
                        {new Date(entry.createdAtUtc).toLocaleString()}
                      </div>
                    </div>
                  </div>
                ))
              )}
            </div>
          </div>

          {needsAttention.length > 0 && (
            <div className="app-card">
              <div className="app-card__header">
                <h3 className="app-card__title">
                  <i className="bi bi-exclamation-triangle-fill text-warning" aria-hidden="true" />
                  Needs Attention
                </h3>
              </div>
              <div className="list-group list-group-flush">
                {needsAttention.map((t) => (
                  <Link
                    key={t.id}
                    to={`/tenants/${t.id}`}
                    className="list-group-item d-flex justify-content-between align-items-center py-3 text-decoration-none"
                  >
                    <span className="text-body">{t.name}</span>
                    <StatusBadge status={t.status} />
                  </Link>
                ))}
              </div>
            </div>
          )}
        </div>

        <div className="col-lg-5">
          <div className="app-card">
            <div className="app-card__header">
              <h3 className="app-card__title">
                <i className="bi bi-pie-chart-fill" aria-hidden="true" />
                Tenants by Plan
              </h3>
            </div>
            <div className="app-card__body">
              {planCounts.map((p) => (
                <div key={p.tier} className="mb-3">
                  <div className="d-flex justify-content-between small mb-1">
                    <span className="text-muted">{p.tier}</span>
                    <span className="fw-semibold">{p.count}</span>
                  </div>
                  <div className="progress" style={{ height: 6 }}>
                    <div
                      className="progress-bar"
                      style={{ width: `${(p.count / maxPlanCount) * 100}%`, background: "var(--itm-accent)" }}
                    />
                  </div>
                </div>
              ))}

              <Link to="/tenants" className="btn btn-outline-primary btn-sm w-100 mt-2">
                <i className="bi bi-plus-lg me-1" />
                Provision New Tenant
              </Link>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
