import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { tenantService, type PlanTier, type ProvisionTenantRequest } from "@/services/tenantService";

const PLAN_TIERS: PlanTier[] = ["Trial", "Starter", "Professional", "Enterprise"];

const DEFAULT_VALUES: ProvisionTenantRequest = {
  planTier: "Trial",
  companyName: "",
  subdomain: "",
  adminFirstName: "",
  adminLastName: "",
  adminEmail: "",
  adminPassword: "",
};

export default function NewTenant() {
  const navigate = useNavigate();
  const [values, setValues] = useState<ProvisionTenantRequest>(DEFAULT_VALUES);
  const [error, setError] = useState<string | null>(null);
  const [fieldErrors, setFieldErrors] = useState<string[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);

  const update = <K extends keyof ProvisionTenantRequest>(key: K, value: ProvisionTenantRequest[K]) =>
    setValues((prev) => ({ ...prev, [key]: value }));

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setFieldErrors([]);
    setIsSubmitting(true);

    const result = await tenantService.provision({
      ...values,
      subdomain: values.subdomain.trim().toLowerCase(),
      companyName: values.companyName.trim(),
      adminEmail: values.adminEmail.trim(),
    });

    setIsSubmitting(false);

    if (!result.success || !result.data) {
      setError(result.message || "Unable to create tenant.");
      setFieldErrors(result.errors ?? []);
      return;
    }

    navigate(`/tenants/${result.data.id}`);
  };

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <div>
          <h1 className="h4 mb-0">New Tenant</h1>
          <p className="text-muted small mb-0">Provision a new company and its administrator account.</p>
        </div>
        <button type="button" className="btn btn-outline-secondary" onClick={() => navigate("/")}>
          Cancel
        </button>
      </div>

      {error && (
        <div className="alert alert-danger">
          {error}
          {fieldErrors.length > 0 && (
            <ul className="mb-0 mt-1 small">
              {fieldErrors.map((err) => (
                <li key={err}>{err}</li>
              ))}
            </ul>
          )}
        </div>
      )}

      <form onSubmit={handleSubmit} className="card shadow-sm border-0" style={{ maxWidth: 640 }}>
        <div className="card-body p-4">
          <h2 className="h6 text-uppercase text-muted mb-3">Company</h2>
          <div className="row g-3 mb-4">
            <div className="col-md-8">
              <label className="form-label">Company Name</label>
              <input
                className="form-control"
                value={values.companyName}
                onChange={(e) => update("companyName", e.target.value)}
                required
              />
            </div>
            <div className="col-md-4">
              <label className="form-label">Plan Tier</label>
              <select
                className="form-select"
                value={values.planTier}
                onChange={(e) => update("planTier", e.target.value as PlanTier)}
              >
                {PLAN_TIERS.map((tier) => (
                  <option key={tier} value={tier}>
                    {tier}
                  </option>
                ))}
              </select>
            </div>
            <div className="col-12">
              <label className="form-label">Subdomain</label>
              <div className="input-group">
                <input
                  className="form-control"
                  placeholder="acme"
                  pattern="^[a-z0-9][a-z0-9-]{1,61}[a-z0-9]$"
                  value={values.subdomain}
                  onChange={(e) => update("subdomain", e.target.value)}
                  required
                />
                <span className="input-group-text">.zentaviocrm.com</span>
              </div>
              <div className="form-text">Lowercase letters, digits and hyphens only.</div>
            </div>
          </div>

          <h2 className="h6 text-uppercase text-muted mb-3">Administrator Account</h2>
          <div className="row g-3">
            <div className="col-md-6">
              <label className="form-label">First Name</label>
              <input
                className="form-control"
                value={values.adminFirstName}
                onChange={(e) => update("adminFirstName", e.target.value)}
                required
              />
            </div>
            <div className="col-md-6">
              <label className="form-label">Last Name</label>
              <input
                className="form-control"
                value={values.adminLastName}
                onChange={(e) => update("adminLastName", e.target.value)}
              />
            </div>
            <div className="col-md-6">
              <label className="form-label">Email</label>
              <input
                type="email"
                className="form-control"
                value={values.adminEmail}
                onChange={(e) => update("adminEmail", e.target.value)}
                required
              />
            </div>
            <div className="col-md-6">
              <label className="form-label">Password</label>
              <input
                type="password"
                className="form-control"
                minLength={8}
                value={values.adminPassword}
                onChange={(e) => update("adminPassword", e.target.value)}
                required
              />
            </div>
          </div>
        </div>

        <div className="card-footer bg-white border-top d-flex gap-2 justify-content-end p-3">
          <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
            {isSubmitting ? "Creating..." : "Create Tenant"}
          </button>
        </div>
      </form>
    </div>
  );
}
