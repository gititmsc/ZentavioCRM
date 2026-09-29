import { useForm } from "react-hook-form";
import { tenantService, PLAN_TIERS, type ProvisionTenantRequest, type PlanTier } from "@/services/tenantService";

interface ProvisionTenantModalProps {
  onClose: () => void;
  onProvisioned: () => void;
}

interface FormValues {
  companyName: string;
  subdomain: string;
  adminFirstName: string;
  adminLastName: string;
  adminEmail: string;
  adminPassword: string;
  planTier: PlanTier;
}

export function ProvisionTenantModal({ onClose, onProvisioned }: ProvisionTenantModalProps) {
  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    defaultValues: {
      companyName: "",
      subdomain: "",
      adminFirstName: "",
      adminLastName: "",
      adminEmail: "",
      adminPassword: "",
      planTier: "Trial",
    },
  });

  const onSubmit = async (values: FormValues) => {
    const request: ProvisionTenantRequest = {
      companyName: values.companyName.trim(),
      subdomain: values.subdomain.trim().toLowerCase(),
      adminFirstName: values.adminFirstName.trim(),
      adminLastName: values.adminLastName.trim() || undefined,
      adminEmail: values.adminEmail.trim(),
      adminPassword: values.adminPassword,
      planTier: values.planTier,
    };

    const response = await tenantService.provision(request);
    if (!response.success) {
      setError("root", { message: response.message || "Could not provision tenant." });
      return;
    }

    onProvisioned();
  };

  return (
    <div className="modal d-block" tabIndex={-1} style={{ background: "rgba(15, 23, 42, 0.45)" }}>
      <div className="modal-dialog modal-dialog-centered">
        <div className="modal-content" style={{ borderRadius: "var(--itm-radius-card)" }}>
          <form onSubmit={handleSubmit(onSubmit)} noValidate>
            <div className="modal-header">
              <h5 className="modal-title d-flex align-items-center gap-2">
                <i className="bi bi-buildings-fill text-primary" aria-hidden="true" />
                Provision New Tenant
              </h5>
              <button type="button" className="btn-close" onClick={onClose} aria-label="Close" />
            </div>

            <div className="modal-body">
              {errors.root && (
                <div className="alert alert-danger py-2" role="alert">
                  {errors.root.message}
                </div>
              )}

              <div className="mb-3">
                <label className="form-label">Company Name</label>
                <input
                  className={`form-control ${errors.companyName ? "is-invalid" : ""}`}
                  {...register("companyName", { required: "Company name is required." })}
                />
                {errors.companyName && <div className="invalid-feedback">{errors.companyName.message}</div>}
              </div>

              <div className="mb-3">
                <label className="form-label">Subdomain</label>
                <div className="input-group">
                  <input
                    className={`form-control ${errors.subdomain ? "is-invalid" : ""}`}
                    placeholder="acme"
                    {...register("subdomain", {
                      required: "Subdomain is required.",
                      pattern: {
                        value: /^[a-z0-9][a-z0-9-]{1,61}[a-z0-9]$/,
                        message: "Lowercase letters, digits and hyphens only.",
                      },
                    })}
                  />
                  <span className="input-group-text">.zentaviocrm.com</span>
                </div>
                {errors.subdomain && <div className="text-danger small mt-1">{errors.subdomain.message}</div>}
              </div>

              <div className="mb-3">
                <label className="form-label">Plan Tier</label>
                <select className="form-select" {...register("planTier")}>
                  {PLAN_TIERS.map((tier) => (
                    <option key={tier} value={tier}>
                      {tier}
                    </option>
                  ))}
                </select>
              </div>

              <hr />

              <div className="row">
                <div className="col-6 mb-3">
                  <label className="form-label">Admin First Name</label>
                  <input
                    className={`form-control ${errors.adminFirstName ? "is-invalid" : ""}`}
                    {...register("adminFirstName", { required: "Required." })}
                  />
                </div>
                <div className="col-6 mb-3">
                  <label className="form-label">Admin Last Name</label>
                  <input className="form-control" {...register("adminLastName")} />
                </div>
              </div>

              <div className="mb-3">
                <label className="form-label">Admin Email</label>
                <input
                  type="email"
                  className={`form-control ${errors.adminEmail ? "is-invalid" : ""}`}
                  {...register("adminEmail", { required: "Admin email is required." })}
                />
                {errors.adminEmail && <div className="invalid-feedback">{errors.adminEmail.message}</div>}
              </div>

              <div className="mb-1">
                <label className="form-label">Admin Password</label>
                <input
                  type="password"
                  className={`form-control ${errors.adminPassword ? "is-invalid" : ""}`}
                  {...register("adminPassword", {
                    required: "Password is required.",
                    minLength: { value: 8, message: "At least 8 characters." },
                  })}
                />
                {errors.adminPassword && <div className="invalid-feedback">{errors.adminPassword.message}</div>}
              </div>
            </div>

            <div className="modal-footer">
              <button type="button" className="btn btn-outline-secondary" onClick={onClose}>
                Cancel
              </button>
              <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
                {isSubmitting ? "Provisioning..." : "Provision Tenant"}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
