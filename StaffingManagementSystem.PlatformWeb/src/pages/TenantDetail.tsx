import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  tenantService,
  type PlanTier,
  type Tenant,
  type TenantUsage,
} from "@/services/tenantService";

const PLAN_TIERS: PlanTier[] = ["Trial", "Starter", "Professional", "Enterprise"];

const STATUS_BADGE: Record<Tenant["status"], string> = {
  Provisioning: "text-bg-secondary",
  Active: "text-bg-success",
  Suspended: "text-bg-warning",
  Failed: "text-bg-danger",
  Terminated: "text-bg-danger",
};

export default function TenantDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  const [tenant, setTenant] = useState<Tenant | null>(null);
  const [usage, setUsage] = useState<TenantUsage | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);

  const [planTier, setPlanTier] = useState<PlanTier>("Trial");
  const [maxUsers, setMaxUsers] = useState<number>(0);
  const [maxStorageMB, setMaxStorageMB] = useState<number>(0);
  const [maxRecords, setMaxRecords] = useState<number>(0);

  const [actionError, setActionError] = useState<string | null>(null);
  const [isSavingPlan, setIsSavingPlan] = useState(false);
  const [isChangingStatus, setIsChangingStatus] = useState(false);
  const [reason, setReason] = useState("");

  const load = async () => {
    if (!id) return;
    setIsLoading(true);
    const [tenantResult, usageResult] = await Promise.all([tenantService.getById(id), tenantService.getUsage(id)]);
    setIsLoading(false);

    if (!tenantResult.success || !tenantResult.data) {
      setLoadError(tenantResult.message || "Unable to load tenant.");
      return;
    }
    setTenant(tenantResult.data);
    setPlanTier(tenantResult.data.planTier);
    setMaxUsers(tenantResult.data.maxUsers);
    setMaxStorageMB(tenantResult.data.maxStorageMB);
    setMaxRecords(tenantResult.data.maxRecords);

    if (usageResult.success && usageResult.data) {
      setUsage(usageResult.data);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const handleSavePlan = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!id) return;
    setActionError(null);
    setIsSavingPlan(true);

    const result = await tenantService.updatePlan(id, { planTier, maxUsers, maxStorageMB, maxRecords });
    setIsSavingPlan(false);

    if (!result.success || !result.data) {
      setActionError(result.message || "Unable to update plan.");
      return;
    }
    setTenant(result.data);
  };

  const runStatusAction = async (action: () => Promise<{ success: boolean; message: string; data?: Tenant }>) => {
    setActionError(null);
    setIsChangingStatus(true);
    const result = await action();
    setIsChangingStatus(false);

    if (!result.success || !result.data) {
      setActionError(result.message || "Unable to update tenant status.");
      return;
    }
    setTenant(result.data);
    setReason("");
  };

  if (isLoading) {
    return <div className="text-muted">Loading...</div>;
  }

  if (loadError || !tenant) {
    return <div className="alert alert-danger">{loadError || "Tenant not found."}</div>;
  }

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h1 className="h4 mb-0">{tenant.name}</h1>
          <p className="text-muted small mb-0">
            {tenant.subdomain}.zentaviocrm.com &middot; <span className={`badge ${STATUS_BADGE[tenant.status]}`}>{tenant.status}</span>
          </p>
        </div>
        <button type="button" className="btn btn-outline-secondary" onClick={() => navigate("/")}>
          Back to Tenants
        </button>
      </div>

      {actionError && <div className="alert alert-danger">{actionError}</div>}

      <div className="row g-4">
        <div className="col-lg-7">
          <div className="card shadow-sm border-0 mb-4">
            <div className="card-body p-4">
              <h2 className="h6 text-uppercase text-muted mb-3">Overview</h2>
              <dl className="row mb-0 small">
                <dt className="col-5 text-muted">Admin Email</dt>
                <dd className="col-7">{tenant.adminEmail}</dd>
                <dt className="col-5 text-muted">Database</dt>
                <dd className="col-7">{tenant.databaseName}</dd>
                <dt className="col-5 text-muted">Created</dt>
                <dd className="col-7">{new Date(tenant.createdAtUtc).toLocaleString()}</dd>
                <dt className="col-5 text-muted">Activated</dt>
                <dd className="col-7">{tenant.activatedAtUtc ? new Date(tenant.activatedAtUtc).toLocaleString() : "—"}</dd>
              </dl>
            </div>
          </div>

          <form onSubmit={handleSavePlan} className="card shadow-sm border-0 mb-4">
            <div className="card-body p-4">
              <h2 className="h6 text-uppercase text-muted mb-3">Plan &amp; Limits</h2>
              <div className="row g-3">
                <div className="col-md-6">
                  <label className="form-label">Plan Tier</label>
                  <select className="form-select" value={planTier} onChange={(e) => setPlanTier(e.target.value as PlanTier)}>
                    {PLAN_TIERS.map((tier) => (
                      <option key={tier} value={tier}>
                        {tier}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="col-md-6" />
                <div className="col-md-4">
                  <label className="form-label">Max Users</label>
                  <input
                    type="number"
                    min={1}
                    className="form-control"
                    value={maxUsers}
                    onChange={(e) => setMaxUsers(Number(e.target.value))}
                  />
                </div>
                <div className="col-md-4">
                  <label className="form-label">Max Storage (MB)</label>
                  <input
                    type="number"
                    min={1}
                    className="form-control"
                    value={maxStorageMB}
                    onChange={(e) => setMaxStorageMB(Number(e.target.value))}
                  />
                </div>
                <div className="col-md-4">
                  <label className="form-label">Max Records</label>
                  <input
                    type="number"
                    min={1}
                    className="form-control"
                    value={maxRecords}
                    onChange={(e) => setMaxRecords(Number(e.target.value))}
                  />
                </div>
              </div>
            </div>
            <div className="card-footer bg-white border-top d-flex justify-content-end p-3">
              <button type="submit" className="btn btn-primary" disabled={isSavingPlan}>
                {isSavingPlan ? "Saving..." : "Save Plan & Limits"}
              </button>
            </div>
          </form>

          <div className="card shadow-sm border-0">
            <div className="card-body p-4">
              <h2 className="h6 text-uppercase text-muted mb-3">Status</h2>
              <p className="small text-muted">
                Suspend or stop this tenant to block sign-in without touching its data, or reactivate it to restore access.
              </p>
              <input
                className="form-control form-control-sm mb-3"
                placeholder="Reason (optional)"
                value={reason}
                onChange={(e) => setReason(e.target.value)}
              />
              <div className="d-flex gap-2 flex-wrap">
                {tenant.status !== "Suspended" && (
                  <button
                    type="button"
                    className="btn btn-outline-warning btn-sm"
                    disabled={isChangingStatus}
                    onClick={() => runStatusAction(() => tenantService.suspend(tenant.id, reason || undefined))}
                  >
                    Suspend
                  </button>
                )}
                {tenant.status !== "Terminated" && (
                  <button
                    type="button"
                    className="btn btn-outline-danger btn-sm"
                    disabled={isChangingStatus}
                    onClick={() => runStatusAction(() => tenantService.stop(tenant.id, reason || undefined))}
                  >
                    Stop
                  </button>
                )}
                {(tenant.status === "Suspended" || tenant.status === "Terminated") && (
                  <button
                    type="button"
                    className="btn btn-outline-success btn-sm"
                    disabled={isChangingStatus}
                    onClick={() => runStatusAction(() => tenantService.reactivate(tenant.id))}
                  >
                    Reactivate
                  </button>
                )}
              </div>
            </div>
          </div>
        </div>

        <div className="col-lg-5">
          <div className="card shadow-sm border-0">
            <div className="card-body p-4">
              <h2 className="h6 text-uppercase text-muted mb-3">Usage</h2>
              {!usage && <div className="text-muted small">Usage data unavailable.</div>}
              {usage && (
                <dl className="row mb-0 small">
                  <dt className="col-7 text-muted">Users</dt>
                  <dd className="col-5 text-end">
                    {usage.userCount} / {usage.maxUsers}
                  </dd>
                  <dt className="col-7 text-muted">Records</dt>
                  <dd className="col-5 text-end">
                    {usage.recordCount} / {usage.maxRecords}
                  </dd>
                  <dt className="col-7 text-muted">Storage</dt>
                  <dd className="col-5 text-end">
                    {usage.databaseSizeMB.toFixed(1)} / {usage.maxStorageMB} MB
                  </dd>
                  <dt className="col-7 text-muted">Last Activity</dt>
                  <dd className="col-5 text-end">
                    {usage.lastActivityAtUtc ? new Date(usage.lastActivityAtUtc).toLocaleDateString() : "—"}
                  </dd>
                </dl>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
