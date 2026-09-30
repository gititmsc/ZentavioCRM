import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { PageHeader } from "@/components/PageHeader";
import { Avatar } from "@/components/Avatar";
import { platformAdminService, type CreatePlatformAdminRequest } from "@/services/platformAdminService";
import type { PlatformAdmin, PlatformAdminRole } from "@/services/authService";
import { useAuth } from "@/context/AuthContext";

interface FormValues {
  email: string;
  firstName: string;
  lastName: string;
  password: string;
  role: PlatformAdminRole;
}

export function PlatformAdminsPage() {
  const { isSuperAdmin } = useAuth();
  const [admins, setAdmins] = useState<PlatformAdmin[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showForm, setShowForm] = useState(false);

  const {
    register,
    handleSubmit,
    reset,
    setError: setFormError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ defaultValues: { email: "", firstName: "", lastName: "", password: "", role: "SuperAdmin" } });

  const load = async () => {
    setLoading(true);
    const response = await platformAdminService.getAll();
    if (response.success && response.data) {
      setAdmins(response.data);
      setError(null);
    } else {
      setError(response.message || "Could not load platform admins.");
    }
    setLoading(false);
  };

  useEffect(() => {
    void load();
  }, []);

  const onSubmit = async (values: FormValues) => {
    const request: CreatePlatformAdminRequest = {
      email: values.email.trim(),
      firstName: values.firstName.trim(),
      lastName: values.lastName.trim() || undefined,
      password: values.password,
      role: values.role,
    };

    const response = await platformAdminService.create(request);
    if (!response.success) {
      setFormError("root", { message: response.message || "Could not create platform admin." });
      return;
    }

    reset();
    setShowForm(false);
    void load();
  };

  return (
    <div>
      <PageHeader
        eyebrow="Access"
        icon="bi-shield-lock-fill"
        title="Platform Admins"
        subtitle="Who can sign in to this console and act on tenants."
        actions={
          isSuperAdmin ? (
            <button type="button" className="btn btn-primary" onClick={() => setShowForm((v) => !v)}>
              <i className="bi bi-plus-lg me-1" />
              New Admin
            </button>
          ) : undefined
        }
      />

      {error && <div className="alert alert-danger">{error}</div>}

      {showForm && isSuperAdmin && (
        <div className="app-card mb-4">
          <div className="app-card__header">
            <h3 className="app-card__title">
              <i className="bi bi-person-plus-fill" />
              New Platform Admin
            </h3>
          </div>
          <div className="app-card__body">
            <form onSubmit={handleSubmit(onSubmit)} noValidate>
              {errors.root && <div className="alert alert-danger py-2">{errors.root.message}</div>}
              <div className="row g-3">
                <div className="col-md-4">
                  <label className="form-label">First Name</label>
                  <input className={`form-control ${errors.firstName ? "is-invalid" : ""}`} {...register("firstName", { required: "Required." })} />
                </div>
                <div className="col-md-4">
                  <label className="form-label">Last Name</label>
                  <input className="form-control" {...register("lastName")} />
                </div>
                <div className="col-md-4">
                  <label className="form-label">Email</label>
                  <input type="email" className={`form-control ${errors.email ? "is-invalid" : ""}`} {...register("email", { required: "Required." })} />
                </div>
                <div className="col-md-6">
                  <label className="form-label">Password</label>
                  <input
                    type="password"
                    className={`form-control ${errors.password ? "is-invalid" : ""}`}
                    {...register("password", { required: "Required.", minLength: { value: 8, message: "At least 8 characters." } })}
                  />
                  {errors.password && <div className="invalid-feedback">{errors.password.message}</div>}
                </div>
                <div className="col-md-6">
                  <label className="form-label">Role</label>
                  <select className="form-select" {...register("role", { required: true })}>
                    <option value="SuperAdmin">Super Admin — full access</option>
                    <option value="Support">Support — read-only</option>
                  </select>
                </div>
              </div>
              <div className="d-flex justify-content-end gap-2 mt-3">
                <button type="button" className="btn btn-outline-secondary" onClick={() => setShowForm(false)}>
                  Cancel
                </button>
                <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
                  {isSubmitting ? "Creating..." : "Create Admin"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      <div className="app-card">
        <div className="table-responsive">
          <table className="table table-modern align-middle mb-0">
            <thead>
              <tr>
                <th>Name</th>
                <th>Role</th>
                <th>Status</th>
                <th>Last Login</th>
                <th>Created</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={5} className="text-center text-muted py-5">
                    Loading...
                  </td>
                </tr>
              ) : admins.length === 0 ? (
                <tr>
                  <td colSpan={5} className="p-0">
                    <div className="empty-state">
                      <div className="empty-state__icon">
                        <i className="bi bi-shield-lock" aria-hidden="true" />
                      </div>
                      <div className="empty-state__title">No platform admins found</div>
                    </div>
                  </td>
                </tr>
              ) : (
                admins.map((a) => (
                  <tr key={a.id}>
                    <td>
                      <div className="d-flex align-items-center gap-3">
                        <Avatar name={a.fullName} size={34} />
                        <div>
                          <div className="fw-semibold">{a.fullName}</div>
                          <div className="text-muted" style={{ fontSize: "0.78rem" }}>
                            {a.email}
                          </div>
                        </div>
                      </div>
                    </td>
                    <td>
                      <span className={`badge ${a.role === "SuperAdmin" ? "text-bg-primary" : "text-bg-light text-dark border"}`}>
                        {a.role === "SuperAdmin" ? "Super Admin" : "Support"}
                      </span>
                    </td>
                    <td>
                      <span className={`badge ${a.isActive ? "text-bg-success" : "text-bg-secondary"}`}>
                        {a.isActive ? "Active" : "Inactive"}
                      </span>
                      {a.isLockedOut && (
                        <span className="badge text-bg-danger ms-1">
                          <i className="bi bi-lock-fill me-1" />
                          Locked
                        </span>
                      )}
                    </td>
                    <td className="text-muted small">{a.lastLoginAtUtc ? new Date(a.lastLoginAtUtc).toLocaleString() : "Never"}</td>
                    <td className="text-muted small">{new Date(a.createdAtUtc).toLocaleDateString()}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
