import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router-dom";
import { territoryService, type Territory, type SaveTerritoryRequest } from "@/services/territoryService";
import { PageHeader } from "@/components/layout/PageHeader";
import { FormSection } from "@/components/form/FormSection";
import { FormActionBar } from "@/components/form/FormActionBar";

export default function TerritoryForm() {
  const { id } = useParams<{ id: string }>();
  const isEditMode = Boolean(id);
  const navigate = useNavigate();

  const [territories, setTerritories] = useState<Territory[]>([]);
  const [serverError, setServerError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(isEditMode);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<SaveTerritoryRequest>({
    defaultValues: { name: "", parentTerritoryId: null, isActive: true },
  });

  useEffect(() => {
    (async () => {
      const all = await territoryService.getAll();
      if (all.success && all.data) {
        setTerritories(all.data.filter((t) => t.id !== id));
      }

      if (isEditMode && id) {
        const existing = await territoryService.getById(id);
        if (existing.success && existing.data) {
          reset({
            name: existing.data.name,
            parentTerritoryId: existing.data.parentTerritoryId,
            isActive: existing.data.isActive,
          });
        }
      }
      setIsLoading(false);
    })();
  }, [id, isEditMode, reset]);

  const onSubmit = async (values: SaveTerritoryRequest) => {
    setServerError(null);
    const request: SaveTerritoryRequest = {
      ...values,
      parentTerritoryId: values.parentTerritoryId || null,
    };

    const result = isEditMode && id
      ? await territoryService.update(id, request)
      : await territoryService.create(request);

    if (!result.success) {
      setServerError(result.message || "Unable to save territory.");
      return;
    }

    navigate("/territories");
  };

  if (isLoading) {
    return <div className="text-muted">Loading...</div>;
  }

  return (
    <div>
      <PageHeader
        title={isEditMode ? "Edit Territory" : "New Territory"}
        subtitle={isEditMode ? "Update this territory's name and hierarchy." : "Add a new sales territory."}
        backTo="/territories"
        backLabel="Back to Territories"
      />

      {serverError && <div className="alert alert-danger">{serverError}</div>}

      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        <FormSection icon="bi-geo-alt" title="Territory Details" description="Name, hierarchy, and status.">
          <div className="row g-3">
            <div className="col-md-6">
              <label className="form-label" htmlFor="name">
                Territory Name
              </label>
              <input
                id="name"
                className={`form-control ${errors.name ? "is-invalid" : ""}`}
                {...register("name", { required: "Territory name is required." })}
              />
              {errors.name && <div className="invalid-feedback">{errors.name.message}</div>}
            </div>

            <div className="col-md-6">
              <label className="form-label" htmlFor="parentTerritoryId">
                Parent Territory
              </label>
              <select id="parentTerritoryId" className="form-select" {...register("parentTerritoryId")}>
                <option value="">None</option>
                {territories.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.name}
                  </option>
                ))}
              </select>
            </div>

            <div className="col-md-6 d-flex align-items-end">
              <div className="form-check">
                <input id="isActive" type="checkbox" className="form-check-input" {...register("isActive")} />
                <label className="form-check-label" htmlFor="isActive">
                  Active
                </label>
              </div>
            </div>
          </div>
        </FormSection>

        <FormActionBar>
          <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
            {isSubmitting ? "Saving..." : "Save"}
          </button>
          <button type="button" className="btn btn-outline-secondary" onClick={() => navigate("/territories")}>
            Cancel
          </button>
        </FormActionBar>
      </form>
    </div>
  );
}
