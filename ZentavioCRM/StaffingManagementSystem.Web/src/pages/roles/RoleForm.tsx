import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router-dom";
import { roleService, type PermissionCatalog, type SaveRoleRequest } from "@/services/roleService";

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
  } = useForm<{ name: string; description: string }>({ defaultValues: { name: "", description: "" } });

  useEffect(() => {
    (async () => {
      const catalogResult = await roleService.getPermissionCatalog();
      if (catalogResult.success && catalogResult.data) {
        setCatalog(catalogResult.data);
      }

      if (isEditMode && id) {
        const existing = await roleService.getById(id);
        if (existing.success && existing.data) {
          reset({ name: existing.data.name, description: existing.data.description ?? "" });
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

  const onSubmit = async (values: { name: string; description: string }) => {
    setServerError(null);

    const request: SaveRoleRequest = {
      name: values.name,
      description: values.description || null,
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
      <h1 className="h4 mb-4">{isEditMode ? "Edit Role" : "New Role"}</h1>

      <div className="card shadow-sm border-0" style={{ maxWidth: 720 }}>
        <div className="card-body">
          {serverError && <div className="alert alert-danger">{serverError}</div>}

          <form onSubmit={handleSubmit(onSubmit)} noValidate>
            <div className="mb-3">
              <label className="form-label">Role Name</label>
              <input
                className={`form-control ${errors.name ? "is-invalid" : ""}`}
                {...register("name", { required: "Role name is required." })}
              />
              {errors.name && <div className="invalid-feedback">{errors.name.message}</div>}
            </div>

            <div className="mb-4">
              <label className="form-label">Description</label>
              <textarea className="form-control" rows={2} {...register("description")} />
            </div>

            <label className="form-label">Permissions</label>
            <div className="row g-3 mb-4">
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

            <div className="d-flex gap-2">
              <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
                {isSubmitting ? "Saving..." : "Save"}
              </button>
              <button type="button" className="btn btn-outline-secondary" onClick={() => navigate("/roles")}>
                Cancel
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
