import { useState } from "react";
import { PLAN_TIERS, type PlanTier, type Tenant } from "@/services/tenantService";

interface PlanEditorProps {
  tenant: Tenant;
  onCancel: () => void;
  onSave: (planTier: PlanTier, maxUsers?: number, maxStorageMB?: number, maxRecords?: number) => Promise<void>;
}

/** Inline edit form for a tenant's plan tier and limit overrides. Leaving a limit field blank
 * means "use that tier's default" (see UpdateTenantPlanRequest on the backend). */
export function PlanEditor({ tenant, onCancel, onSave }: PlanEditorProps) {
  const [planTier, setPlanTier] = useState<PlanTier>(tenant.planTier);
  const [maxUsers, setMaxUsers] = useState(String(tenant.maxUsers));
  const [maxStorageMB, setMaxStorageMB] = useState(String(tenant.maxStorageMB));
  const [maxRecords, setMaxRecords] = useState(String(tenant.maxRecords));
  const [saving, setSaving] = useState(false);

  const handleSave = async () => {
    setSaving(true);
    try {
      await onSave(
        planTier,
        maxUsers ? Number(maxUsers) : undefined,
        maxStorageMB ? Number(maxStorageMB) : undefined,
        maxRecords ? Number(maxRecords) : undefined
      );
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="border rounded-3 p-3" style={{ borderColor: "var(--itm-border)" }}>
      <div className="mb-3">
        <label className="form-label">Plan Tier</label>
        <select className="form-select" value={planTier} onChange={(e) => setPlanTier(e.target.value as PlanTier)}>
          {PLAN_TIERS.map((tier) => (
            <option key={tier} value={tier}>
              {tier}
            </option>
          ))}
        </select>
      </div>

      <div className="row g-2 mb-3">
        <div className="col-4">
          <label className="form-label small">Max Users</label>
          <input className="form-control" type="number" min={1} value={maxUsers} onChange={(e) => setMaxUsers(e.target.value)} />
        </div>
        <div className="col-4">
          <label className="form-label small">Max Storage (MB)</label>
          <input className="form-control" type="number" min={1} value={maxStorageMB} onChange={(e) => setMaxStorageMB(e.target.value)} />
        </div>
        <div className="col-4">
          <label className="form-label small">Max Records</label>
          <input className="form-control" type="number" min={1} value={maxRecords} onChange={(e) => setMaxRecords(e.target.value)} />
        </div>
      </div>

      <div className="d-flex gap-2 justify-content-end">
        <button type="button" className="btn btn-outline-secondary btn-sm" onClick={onCancel} disabled={saving}>
          Cancel
        </button>
        <button type="button" className="btn btn-primary btn-sm" onClick={() => void handleSave()} disabled={saving}>
          {saving ? "Saving..." : "Save Plan"}
        </button>
      </div>
    </div>
  );
}
