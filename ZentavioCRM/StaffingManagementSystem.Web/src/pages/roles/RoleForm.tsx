import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router-dom";
import { roleService, type PermissionCatalog, type SaveRoleRequest, type VisibilityScope } from "@/services/roleService";
import { PageHeader } from "@/components/layout/PageHeader";
import { FormSection } from "@/components/form/FormSection";
import { FormActionBar } from "@/components/form/FormActionBar";

const VISIBILITY_SCOPE_OPTIONS: { value: VisibilityScope; label: string; description: string }[] = [
  { value: "Own", label: "Own records only", description: "Users with this role only see Leads/Customers/Opportunities assigned to (or created by) them." },
  { value: "Team", label: "Team (same department)", description: "Users with this role see records belonging to anyone in their department, in addition to their own." },
  { value: "All", label: "All records", description: "Users with this role see every Lead/Customer/Opportunity, regardless of owner — today's default behavior." },
];

export default function RoleForm() {
  const { id } = useParams<{ id: string }>();
  const isEditMode = Boolean(id);
  const navigate = useNavigate();

  const [catalog, setCatalog] = useState<PermissionCatalog>({});
  const [selectedCodes, setSelectedCodes] = useState<Set<string>>(new Set());
  const [serverError, setServerError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<{ name: string; description: string; visibilityScope: VisibilityScope }>({
    defaultValues: { name: "", description: "", visibilityScope: "All" },
  });

  useEffect(() => {
    (async () => {
      const catalogResult = await roleService.getPermissionCatalog();
      if (catalogResult.success && catalogResult.data) {
        setCatalog(catalogResult.data);
      }

      if (isEditMode && id) {
        const existing = await roleService.getById(id);
        if (existing.success && existing.data) {
          reset({
            name: existing.data.name,
            description: existing.data.description ?? "",
            visibilityScope: existing.data.visibilityScope,
          });
          setSelectedCodes(new Set(existing.data.permissionCodes));
        }
      }

      setIsLoading(false);
    })();
  }, [id, isEditMode, reset]);

  const toggleCode = (code: string) => {
    setSelectedCodes((prev) => {
      const next = new Set(prev);
      if (next.has(code)) next.delete(code);
      else next.add(code);
      return next;
    });
  };

  const onSubmit = async (values: { name: string; description: string; visibilityScope: VisibilityScope }) => {
    setServerError(null);

    const request: SaveRoleRequest = {
      name: values.name,
      description: values.description || null,
      visibilityScope: values.visibilityScope,
      permissionCodes: Array.from(selectedCodes),
    };

    const result = isEditMode && id ? await roleService.update(id, request) : await roleService.create(request);

    if (!result.success) {
      setServerError(result.message || "Unable to save role.");
      return;
    }

    navigate("/roles");
  };

  if (isLoading) {
    return <div className="text-muted">Loading...</div>;
  }

  return (
    <div>
      <PageHeader
        title={isEditMode ? "Edit Role" : "New Role"}
        subtitle={isEditMode ? "Update this role's details and permissions." : "Define a new role and its permissions."}
        backTo="/roles"
        backLabel="Back to Roles"
      />

      {serverError && <div className="alert alert-danger">{serverError}</div>}

      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        <FormSection icon="bi-briefcase" title="Role Details" description="Name, description, and record visibility.">
          <div className="row g-3">
            <div className="col-md-6">
              <label className="form-label">Role Name</label>
              <input
                className={`form-control ${errors.name ? "is-invalid" : ""}`}
                {...register("name", { required: "Role name is required." })}
              />
              {errors.name && <div className="invalid-feedback">{errors.name.message}</div>}
            </div>

            <div className="col-md-6">
              <label className="form-label">Record Visibility</label>
              <select className="form-select" {...register("visibilityScope")}>
                {VISIBILITY_SCOPE_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
              <div className="form-text">
                How much of the Leads/Customers/Opportunities record-set a user with this role can see, independent of module permissions.
              </div>
            </div>

            <div className="col-12">
              <label className="form-label">Description</label>
              <textarea className="form-control" rows={2} {...register("description")} />
            </div>
          </div>
        </FormSection>

        <FormSection icon="bi-shield-lock" title="Permissions" description="What users with this role are allowed to do.">
          <div className="row g-3">
            {Object.entries(catalog).map(([module, codes]) => (
              <div className="col-md-6" key={module}>
                <div className="border rounded p-3 h-100">
                  <div className="fw-semibold mb-2">{module}</div>
                  {codes.map((code) => (
                    <div className="form-check" key={code}>
                      <input
                        id={`perm-${code}`}
                        type="checkbox"
                        className="form-check-input"
                        checked={selectedCodes.has(code)}
                        onChange={() => toggleCode(code)}
                      />
                      <label className="form-check-label" htmlFor={`perm-${code}`}>
                        {code.split(".")[1]}
                      </label>
                    </div>
                  ))}
                </div>
              </div>
            ))}
          </div>
        </FormSection>

        <FormActionBar>
          <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
            {isSubmitting ? "Saving..." : "Save"}
          </button>
          <button type="button" className="btn btn-outline-secondary" onClick={() => navigate("/roles")}>
            Cancel
          </button>
        </FormActionBar>
      </form>
    </div>
  );
}
