import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { tenantService, type Tenant, type TenantUsage, type PlanTier } from "@/services/tenantService";
import { StatusBadge } from "@/components/StatusBadge";
import { PaymentStatusBadge } from "@/components/PaymentStatusBadge";
import { Avatar } from "@/components/Avatar";
import { ReasonModal } from "@/components/ReasonModal";
import { PlanEditor } from "@/pages/tenants/PlanEditor";
import { MetadataEditor } from "@/pages/tenants/MetadataEditor";
import { BillingPanel } from "@/pages/tenants/BillingPanel";
import { NotesPanel } from "@/pages/tenants/NotesPanel";
import { ActivityTab } from "@/pages/tenants/ActivityTab";
import { useAuth } from "@/context/AuthContext";

type ActiveDialog = "suspend" | "stop" | "impersonate" | null;
type ActiveTab = "overview" | "billing" | "notes" | "activity";

function UsageBar({ label, value, max, format }: { label: string; value: number; max: number; format?: (n: number) => string }) {
  const pct = max > 0 ? Math.min(100, Math.round((value / max) * 100)) : 0;
  const display = format ?? ((n: number) => n.toLocaleString());
  const barColor = pct >= 100 ? "var(--itm-danger)" : pct >= 80 ? "var(--itm-warning)" : "var(--itm-accent)";

  return (
    <div className="mb-3">
      <div className="d-flex justify-content-between small mb-1">
        <span className="text-muted">{label}</span>
        <span className="fw-semibold">
          {display(value)} / {display(max)}
        </span>
      </div>
      <div className="progress" style={{ height: 7, borderRadius: 999 }}>
        <div className="progress-bar" style={{ width: `${pct}%`, background: barColor, borderRadius: 999 }} />
      </div>
    </div>
  );
}

export function TenantDetail() {
  const { isSuperAdmin } = useAuth();
  const { id } = useParams<{ id: string }>();
  const [tenant, setTenant] = useState<Tenant | null>(null);
  const [usage, setUsage] = useState<TenantUsage | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [banner, setBanner] = useState<string | null>(null);
  const [editingPlan, setEditingPlan] = useState(false);
  const [editingMetadata, setEditingMetadata] = useState(false);
  const [activeTab, setActiveTab] = useState<ActiveTab>("overview");
  const [activeDialog, setActiveDialog] = useState<ActiveDialog>(null);
  const [impersonateResult, setImpersonateResult] = useState<{
    token: string;
    expiresAtUtc: string;
    tenantSubdomain: string;
    impersonatedUserEmail: string;
  } | null>(null);

  const load = async () => {
    if (!id) return;
    const [tenantResponse, usageResponse] = await Promise.all([tenantService.getById(id), tenantService.getUsage(id)]);
    if (tenantResponse.success && tenantResponse.data) {
      setTenant(tenantResponse.data);
      setError(null);
    } else {
      setError(tenantResponse.message || "Tenant not found.");
    }
    if (usageResponse.success && usageResponse.data) {
      setUsage(usageResponse.data);
    }
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const backLink = (
    <Link to="/tenants" className="text-muted text-decoration-none small fw-semibold d-inline-flex align-items-center gap-1">
      <i className="bi bi-arrow-left" /> Back to Tenants
    </Link>
  );

  if (error) {
    return (
      <div>
        {backLink}
        <div className="alert alert-danger mt-3">{error}</div>
      </div>
    );
  }

  if (!tenant) {
    return (
      <div>
        {backLink}
        <div className="text-muted mt-3">Loading tenant...</div>
      </div>
    );
  }

  const handleReactivate = async () => {
    if (!id) return;
    const response = await tenantService.reactivate(id);
    if (response.success) {
      setBanner("Tenant reactivated.");
      void load();
    } else {
      setError(response.message || "Could not reactivate tenant.");
    }
  };

  const handleSuspend = async (reason: string) => {
    if (!id) return;
    const response = await tenantService.suspend(id, reason || undefined);
    setActiveDialog(null);
    if (response.success) {
      setBanner("Tenant suspended.");
      void load();
    } else {
      setError(response.message || "Could not suspend tenant.");
    }
  };

  const handleStop = async (reason: string) => {
    if (!id) return;
    const response = await tenantService.stop(id, reason || undefined);
    setActiveDialog(null);
    if (response.success) {
      setBanner("Tenant stopped. This is reversible — reactivate it to restore access.");
      void load();
    } else {
      setError(response.message || "Could not stop tenant.");
    }
  };

  const [sendingAdminAction, setSendingAdminAction] = useState<"welcome" | "reset" | null>(null);

  const handleResendWelcomeEmail = async () => {
    if (!id) return;
    setSendingAdminAction("welcome");
    const response = await tenantService.resendWelcomeEmail(id);
    setSendingAdminAction(null);
    if (response.success) {
      setBanner("Welcome email sent.");
    } else {
      setError(response.message || "Could not send the welcome email.");
    }
  };

  const handleForcePasswordReset = async () => {
    if (!id) return;
    setSendingAdminAction("reset");
    const response = await tenantService.forcePasswordReset(id);
    setSendingAdminAction(null);
    if (response.success) {
      setBanner("Password reset email sent.");
    } else {
      setError(response.message || "Could not send the password reset email.");
    }
  };

  const handleImpersonate = async (reason: string) => {
    if (!id) return;
    const response = await tenantService.impersonate(id, reason);
    setActiveDialog(null);
    if (response.success && response.data) {
      setImpersonateResult(response.data);
    } else {
      setError(response.message || "Could not impersonate this tenant's admin.");
    }
  };

  const handleSavePlan = async (planTier: PlanTier, maxUsers?: number, maxStorageMB?: number, maxRecords?: number) => {
    if (!id) return;
    const response = await tenantService.updatePlan(id, { planTier, maxUsers, maxStorageMB, maxRecords });
    if (response.success) {
      setEditingPlan(false);
      setBanner("Plan updated.");
      void load();
    } else {
      setError(response.message || "Could not update plan.");
    }
  };

  const handleSaveMetadata = async (name: string, adminEmail: string) => {
    if (!id) return;
    const response = await tenantService.updateMetadata(id, { name, adminEmail });
    if (response.success) {
      setEditingMetadata(false);
      setBanner("Tenant metadata updated.");
      void load();
    } else {
      setError(response.message || "Could not update tenant metadata.");
    }
  };

  return (
    <div>
      {backLink}

      {banner && (
        <div className="alert alert-success mt-3 d-flex justify-content-between align-items-center">
          <span>
            <i className="bi bi-check-circle-fill me-2" />
            {banner}
          </span>
          <button type="button" className="btn-close" onClick={() => setBanner(null)} />
        </div>
      )}
      {error && (
        <div className="alert alert-danger mt-3 d-flex justify-content-between align-items-center">
          <span>{error}</span>
          <button type="button" className="btn-close" onClick={() => setError(null)} />
        </div>
      )}

      <div className="d-flex justify-content-between align-items-start flex-wrap gap-3 mt-3 mb-4">
        <div className="d-flex align-items-center gap-3">
          <Avatar name={tenant.name} size={54} />
          <div>
            <div className="d-flex align-items-center gap-2">
              <h4 className="fw-bold mb-0">{tenant.name}</h4>
              <StatusBadge status={tenant.status} />
              <PaymentStatusBadge status={tenant.paymentStatus} />
              {isSuperAdmin && (
                <button
                  type="button"
                  className="btn btn-sm btn-link text-muted p-0 ms-1"
                  onClick={() => setEditingMetadata(true)}
                  title="Edit company name / directory email"
                >
                  <i className="bi bi-pencil-fill" />
                </button>
              )}
            </div>
            <div className="text-muted small mt-1">
              <i className="bi bi-globe2 me-1" />
              {tenant.subdomain}.zentaviocrm.com &middot; <i className="bi bi-person-fill" /> {tenant.adminEmail}
            </div>
          </div>
        </div>

        <div className="d-flex gap-2">
          {!isSuperAdmin && (tenant.status === "Active" || tenant.status === "Suspended" || tenant.status === "Terminated") && (
            <span className="text-muted small fst-italic align-self-center">
              <i className="bi bi-eye me-1" />
              Read-only (Support role)
            </span>
          )}
          {isSuperAdmin && tenant.status === "Active" && (
            <>
              <button
                type="button"
                className="btn btn-outline-secondary btn-sm"
                onClick={() => void handleResendWelcomeEmail()}
                disabled={sendingAdminAction !== null}
                title="Email this tenant's admin a fresh welcome / set-password link"
              >
                <i className="bi bi-envelope-fill me-1" />
                {sendingAdminAction === "welcome" ? "Sending..." : "Resend Welcome Email"}
              </button>
              <button
                type="button"
                className="btn btn-outline-secondary btn-sm"
                onClick={() => void handleForcePasswordReset()}
                disabled={sendingAdminAction !== null}
                title="Email this tenant's admin a password-reset link"
              >
                <i className="bi bi-key-fill me-1" />
                {sendingAdminAction === "reset" ? "Sending..." : "Force Password Reset"}
              </button>
              <button type="button" className="btn btn-outline-warning btn-sm" onClick={() => setActiveDialog("suspend")}>
                <i className="bi bi-pause-fill me-1" />
                Suspend
              </button>
              <button type="button" className="btn btn-outline-danger btn-sm" onClick={() => setActiveDialog("stop")}>
                <i className="bi bi-stop-fill me-1" />
                Stop
              </button>
              <button type="button" className="btn btn-primary btn-sm" onClick={() => setActiveDialog("impersonate")}>
                <i className="bi bi-person-badge me-1" /> Impersonate
              </button>
            </>
          )}
          {isSuperAdmin && tenant.status === "Suspended" && (
            <>
              <button type="button" className="btn btn-success btn-sm" onClick={() => void handleReactivate()}>
                <i className="bi bi-play-fill me-1" />
                Reactivate
              </button>
              <button type="button" className="btn btn-outline-danger btn-sm" onClick={() => setActiveDialog("stop")}>
                <i className="bi bi-stop-fill me-1" />
                Stop
              </button>
            </>
          )}
          {isSuperAdmin && tenant.status === "Terminated" && (
            <button type="button" className="btn btn-success btn-sm" onClick={() => void handleReactivate()}>
              <i className="bi bi-play-fill me-1" />
              Reactivate
            </button>
          )}
          {(tenant.status === "Provisioning" || tenant.status === "Failed") && (
            <span className="text-muted small fst-italic align-self-center">No actions available while {tenant.status.toLowerCase()}.</span>
          )}
        </div>
      </div>

      <div className="itm-tabs">
        <button type="button" className={`itm-tabs__tab${activeTab === "overview" ? " active" : ""}`} onClick={() => setActiveTab("overview")}>
          <i className="bi bi-grid-1x2-fill" /> Overview
        </button>
        <button type="button" className={`itm-tabs__tab${activeTab === "billing" ? " active" : ""}`} onClick={() => setActiveTab("billing")}>
          <i className="bi bi-receipt" /> Billing
        </button>
        <button type="button" className={`itm-tabs__tab${activeTab === "notes" ? " active" : ""}`} onClick={() => setActiveTab("notes")}>
          <i className="bi bi-sticky-fill" /> Notes
        </button>
        <button type="button" className={`itm-tabs__tab${activeTab === "activity" ? " active" : ""}`} onClick={() => setActiveTab("activity")}>
          <i className="bi bi-clock-history" /> Activity
        </button>
      </div>

      {activeTab === "overview" && (
      <div className="row g-4">
        <div className="col-lg-6">
          <div className="app-card h-100">
            <div className="app-card__header">
              <h3 className="app-card__title">
                <i className="bi bi-credit-card-2-front-fill" aria-hidden="true" />
                Plan
              </h3>
              {!editingPlan && isSuperAdmin && (
                <button type="button" className="btn btn-sm btn-outline-secondary" onClick={() => setEditingPlan(true)}>
                  <i className="bi bi-pencil-fill me-1" />
                  Edit
                </button>
              )}
            </div>
            <div className="app-card__body">
              {editingPlan ? (
                <PlanEditor tenant={tenant} onCancel={() => setEditingPlan(false)} onSave={handleSavePlan} />
              ) : (
                <div>
                  <span className="plan-badge mb-3 d-inline-flex" style={{ fontSize: "0.85rem", padding: "5px 14px" }}>
                    {tenant.planTier}
                  </span>
                  {tenant.planTier === "Trial" && tenant.trialEndsAtUtc && (
                    <div className={`small mt-2 mb-1 ${new Date(tenant.trialEndsAtUtc) <= new Date() ? "text-danger" : "text-warning"}`}>
                      <i className="bi bi-hourglass-split me-1" />
                      {new Date(tenant.trialEndsAtUtc) <= new Date()
                        ? "Trial expired — will be suspended on next access."
                        : `Trial ends ${new Date(tenant.trialEndsAtUtc).toLocaleDateString()}`}
                    </div>
                  )}
                  <div className="d-flex flex-column gap-2 mt-2">
                    <div className="d-flex justify-content-between text-muted small">
                      <span>Max Users</span>
                      <span className="fw-semibold text-body">{tenant.maxUsers.toLocaleString()}</span>
                    </div>
                    <div className="d-flex justify-content-between text-muted small">
                      <span>Max Storage</span>
                      <span className="fw-semibold text-body">{tenant.maxStorageMB.toLocaleString()} MB</span>
                    </div>
                    <div className="d-flex justify-content-between text-muted small">
                      <span>Max Records</span>
                      <span className="fw-semibold text-body">{tenant.maxRecords.toLocaleString()}</span>
                    </div>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>

        <div className="col-lg-6">
          <div className="app-card h-100">
            <div className="app-card__header">
              <h3 className="app-card__title">
                <i className="bi bi-speedometer2" aria-hidden="true" />
                Usage
              </h3>
            </div>
            <div className="app-card__body">
              {usage ? (
                <>
                  <UsageBar label="Users" value={usage.userCount} max={usage.maxUsers} />
                  <UsageBar label="Records" value={usage.recordCount} max={usage.maxRecords} />
                  <UsageBar
                    label="Storage"
                    value={usage.databaseSizeMB}
                    max={usage.maxStorageMB}
                    format={(n) => `${n.toLocaleString(undefined, { maximumFractionDigits: 1 })} MB`}
                  />
                  <div className="text-muted small mt-3 d-flex align-items-center gap-1">
                    <i className="bi bi-activity" />
                    Last activity:{" "}
                    {usage.lastActivityAtUtc ? new Date(usage.lastActivityAtUtc).toLocaleString() : "No user has logged in yet"}
                  </div>
                </>
              ) : (
                <div className="text-muted small">
                  {tenant.status === "Provisioning" || tenant.status === "Failed"
                    ? "No usage yet — this tenant hasn't finished provisioning."
                    : "Loading usage..."}
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
      )}

      {activeTab === "billing" && (
        <BillingPanel
          tenant={tenant}
          onTenantChange={(updated) => setTenant(updated)}
          onError={(message) => setError(message)}
          onBanner={(message) => setBanner(message)}
        />
      )}

      {activeTab === "notes" && id && <NotesPanel tenantId={id} onError={(message) => setError(message)} />}

      {activeTab === "activity" && id && <ActivityTab tenantId={id} />}

      {editingMetadata && (
        <div className="modal d-block" tabIndex={-1} style={{ background: "rgba(15, 23, 42, 0.45)" }}>
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content" style={{ borderRadius: "var(--itm-radius-card)" }}>
              <div className="modal-header">
                <h5 className="modal-title d-flex align-items-center gap-2">
                  <i className="bi bi-pencil-fill text-primary" />
                  Edit Tenant Metadata
                </h5>
                <button type="button" className="btn-close" onClick={() => setEditingMetadata(false)} aria-label="Close" />
              </div>
              <div className="modal-body">
                <MetadataEditor tenant={tenant} onCancel={() => setEditingMetadata(false)} onSave={handleSaveMetadata} />
              </div>
            </div>
          </div>
        </div>
      )}

      {activeDialog === "suspend" && (
        <ReasonModal
          title="Suspend Tenant"
          description="This tenant's users will lose access immediately. Reversible via Reactivate."
          confirmLabel="Suspend"
          confirmVariant="warning"
          onCancel={() => setActiveDialog(null)}
          onConfirm={handleSuspend}
        />
      )}

      {activeDialog === "stop" && (
        <ReasonModal
          title="Stop Tenant"
          description="This tenant's users will lose access immediately. No data is deleted — this is reversible via Reactivate."
          confirmLabel="Stop"
          confirmVariant="danger"
          onCancel={() => setActiveDialog(null)}
          onConfirm={handleStop}
        />
      )}

      {activeDialog === "impersonate" && (
        <ReasonModal
          title="Impersonate Tenant Admin"
          description={`Mints a 15-minute session token for ${tenant.adminEmail}. This is the single most sensitive platform action — it is always audit-logged, both here and in the tenant's own activity history.`}
          confirmLabel="Impersonate"
          confirmVariant="primary"
          reasonRequired
          onCancel={() => setActiveDialog(null)}
          onConfirm={handleImpersonate}
        />
      )}

      {impersonateResult && (
        <div className="modal d-block" tabIndex={-1} style={{ background: "rgba(15, 23, 42, 0.45)" }}>
          <div className="modal-dialog modal-dialog-centered">
            <div className="modal-content" style={{ borderRadius: "var(--itm-radius-card)" }}>
              <div className="modal-header">
                <h5 className="modal-title d-flex align-items-center gap-2">
                  <i className="bi bi-person-badge-fill text-primary" />
                  Impersonation Token
                </h5>
                <button type="button" className="btn-close" onClick={() => setImpersonateResult(null)} />
              </div>
              <div className="modal-body">
                <p className="text-muted small">
                  Signed in as <strong>{impersonateResult.impersonatedUserEmail}</strong> on{" "}
                  <strong>{impersonateResult.tenantSubdomain}.zentaviocrm.com</strong>. Expires at{" "}
                  {new Date(impersonateResult.expiresAtUtc).toLocaleTimeString()} — no refresh, it hard-expires.
                </p>
                <label className="form-label small">Access Token</label>
                <textarea className="form-control font-monospace" rows={4} readOnly value={impersonateResult.token} />
                <button
                  type="button"
                  className="btn btn-sm btn-outline-primary mt-2"
                  onClick={() => void navigator.clipboard.writeText(impersonateResult.token)}
                >
                  <i className="bi bi-clipboard me-1" /> Copy Token
                </button>
              </div>
              <div className="modal-footer">
                <button type="button" className="btn btn-primary" onClick={() => setImpersonateResult(null)}>
                  Done
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
