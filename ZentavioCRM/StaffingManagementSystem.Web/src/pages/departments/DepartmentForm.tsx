import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router-dom";
import { departmentService, type Department, type SaveDepartmentRequest } from "@/services/departmentService";

export default function DepartmentForm() {
  const { id } = useParams<{ id: string }>();
  const isEditMode = Boolean(id);
  const navigate = useNavigate();

  const [departments, setDepartments] = useState<Department[]>([]);
  const [serverError, setServerError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(isEditMode);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<SaveDepartmentRequest>({
    defaultValues: { name: "", parentDepartmentId: null, isActive: true },
  });

  useEffect(() => {
    (async () => {
      const all = await departmentService.getAll();
      if (all.success && all.data) {
        setDepartments(all.data.filter((d) => d.id !== id));
      }

      if (isEditMode && id) {
        const existing = await departmentService.getById(id);
        if (existing.success && existing.data) {
          reset({
            name: existing.data.name,
            parentDepartmentId: existing.data.parentDepartmentId,
            isActive: existing.data.isActive,
          });
        }
      }
      setIsLoading(false);
    })();
  }, [id, isEditMode, reset]);

  const onSubmit = async (values: SaveDepartmentRequest) => {
    setServerError(null);
    const request: SaveDepartmentRequest = {
      ...values,
      parentDepartmentId: values.parentDepartmentId || null,
    };

    const result = isEditMode && id
      ? await departmentService.update(id, request)
      : await departmentService.create(request);

    if (!result.success) {
      setServerError(result.message || "Unable to save department.");
      return;
    }

    navigate("/departments");
  };

  if (isLoading) {
    return <div className="text-muted">Loading...</div>;
  }

  return (
    <div>
      <h1 className="h4 mb-4">{isEditMode ? "Edit Department" : "New Department"}</h1>

      <div className="card shadow-sm border-0" style={{ maxWidth: 560 }}>
        <div className="card-body">
          {serverError && <div className="alert alert-danger">{serverError}</div>}

          <form onSubmit={handleSubmit(onSubmit)} noValidate>
            <div className="mb-3">
              <label className="form-label" htmlFor="name">
                Department Name
              </label>
              <input
                id="name"
                className={`form-control ${errors.name ? "is-invalid" : ""}`}
                {...register("name", { required: "Department name is required." })}
              />
              {errors.name && <div className="invalid-feedback">{errors.name.message}</div>}
            </div>

            <div className="mb-3">
              <label className="form-label" htmlFor="parentDepartmentId">
                Parent Department
              </label>
              <select id="parentDepartmentId" className="form-select" {...register("parentDepartmentId")}>
                <option value="">None</option>
                {departments.map((d) => (
                  <option key={d.id} value={d.id}>
                    {d.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="form-check mb-4">
              <input id="isActive" type="checkbox" className="form-check-input" {...register("isActive")} />
              <label className="form-check-label" htmlFor="isActive">
                Active
              </label>
            </div>

            <div className="d-flex gap-2">
              <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
                {isSubmitting ? "Saving..." : "Save"}
              </button>
              <button type="button" className="btn btn-outline-secondary" onClick={() => navigate("/departments")}>
                Cancel
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
