import { useState } from "react";
import type { Tenant } from "@/services/tenantService";

interface MetadataEditorProps {
  tenant: Tenant;
  onCancel: () => void;
  onSave: (name: string, adminEmail: string) => Promise<void>;
}

/** Inline edit form for a tenant's company name and directory admin-email display field. Note:
 * AdminEmail here is denormalized display data only — it does not change what the tenant's real
 * admin user signs in with in their own database. */
export function MetadataEditor({ tenant, onCancel, onSave }: MetadataEditorProps) {
  const [name, setName] = useState(tenant.name);
  const [adminEmail, setAdminEmail] = useState(tenant.adminEmail);
  const [saving, setSaving] = useState(false);

  const handleSave = async () => {
    setSaving(true);
    try {
      await onSave(name.trim(), adminEmail.trim());
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="border rounded-3 p-3" style={{ borderColor: "var(--itm-border)" }}>
      <div className="mb-3">
        <label className="form-label">Company Name</label>
        <input className="form-control" value={name} onChange={(e) => setName(e.target.value)} maxLength={200} />
      </div>

      <div className="mb-2">
        <label className="form-label">Directory Admin Email</label>
        <input
          className="form-control"
          type="email"
          value={adminEmail}
          onChange={(e) => setAdminEmail(e.target.value)}
          maxLength={256}
        />
        <div className="form-text">
          <i className="bi bi-info-circle me-1" />
          This updates the directory listing only — it does not change the tenant's actual sign-in email.
        </div>
      </div>

      <div className="d-flex gap-2 justify-content-end mt-3">
        <button type="button" className="btn btn-outline-secondary btn-sm" onClick={onCancel} disabled={saving}>
          Cancel
        </button>
        <button
          type="button"
          className="btn btn-primary btn-sm"
          onClick={() => void handleSave()}
          disabled={saving || !name.trim() || !adminEmail.trim()}
        >
          {saving ? "Saving..." : "Save Changes"}
        </button>
      </div>
    </div>
  );
}
