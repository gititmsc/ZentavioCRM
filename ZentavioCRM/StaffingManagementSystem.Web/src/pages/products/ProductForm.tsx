import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router-dom";
import { productService, type SaveProductRequest } from "@/services/productService";
import { PageHeader } from "@/components/layout/PageHeader";
import { FormSection } from "@/components/form/FormSection";
import { FormActionBar } from "@/components/form/FormActionBar";

export default function ProductForm() {
  const { id } = useParams<{ id: string }>();
  const isEditMode = Boolean(id);
  const navigate = useNavigate();

  const [serverError, setServerError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(isEditMode);

  const {
    register,
    handleSubmit,
    reset,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<SaveProductRequest>({
    defaultValues: {
      sku: "",
      name: "",
      type: "Product",
      category: null,
      brand: null,
      unitOfMeasure: null,
      unitPrice: 0,
      cost: null,
      taxPercent: null,
      description: null,
      durationMinutes: null,
      billingType: null,
      isActive: true,
    },
  });

  const type = watch("type");

  useEffect(() => {
    if (!isEditMode || !id) {
      setIsLoading(false);
      return;
    }

    (async () => {
      const existing = await productService.getById(id);
      if (existing.success && existing.data) {
        reset({
          sku: existing.data.sku,
          name: existing.data.name,
          type: existing.data.type,
          category: existing.data.category,
          brand: existing.data.brand,
          unitOfMeasure: existing.data.unitOfMeasure,
          unitPrice: existing.data.unitPrice,
          cost: existing.data.cost,
          taxPercent: existing.data.taxPercent,
          description: existing.data.description,
          durationMinutes: existing.data.durationMinutes,
          billingType: existing.data.billingType,
          isActive: existing.data.isActive,
        });
      }
      setIsLoading(false);
    })();
  }, [id, isEditMode, reset]);

  const onSubmit = async (values: SaveProductRequest) => {
    setServerError(null);

    const request: SaveProductRequest = {
      ...values,
      category: values.category?.trim() || null,
      brand: values.brand?.trim() || null,
      unitOfMeasure: values.unitOfMeasure?.trim() || null,
      description: values.description?.trim() || null,
      billingType: values.type === "Service" ? values.billingType?.trim() || null : null,
      durationMinutes: values.type === "Service" ? values.durationMinutes || null : null,
      cost: values.cost === undefined || (values.cost as unknown as string) === "" ? null : Number(values.cost),
      taxPercent:
        values.taxPercent === undefined || (values.taxPercent as unknown as string) === ""
          ? null
          : Number(values.taxPercent),
      unitPrice: Number(values.unitPrice) || 0,
    };

    const result = isEditMode && id
      ? await productService.update(id, request)
      : await productService.create(request);

    if (!result.success) {
      setServerError(result.message || "Unable to save product.");
      return;
    }

    navigate("/products");
  };

  if (isLoading) {
    return <div className="text-muted">Loading...</div>;
  }

  return (
    <div>
      <PageHeader
        title={isEditMode ? "Edit Catalog Item" : "New Catalog Item"}
        subtitle={isEditMode ? "Update this product or service's details." : "Add a new product or service to the catalog."}
        backTo="/products"
        backLabel="Back to Product Catalog"
      />

      {serverError && <div className="alert alert-danger">{serverError}</div>}

      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        <FormSection icon="bi-box-seam" title="Catalog Details" description="Identity, classification, and status.">
          <div className="row g-3">
            <div className="col-md-4">
              <label className="form-label" htmlFor="sku">
                SKU
              </label>
              <input
                id="sku"
                className={`form-control ${errors.sku ? "is-invalid" : ""}`}
                {...register("sku", { required: "SKU is required." })}
              />
              {errors.sku && <div className="invalid-feedback">{errors.sku.message}</div>}
            </div>

            <div className="col-md-8">
              <label className="form-label" htmlFor="name">
                Name
              </label>
              <input
                id="name"
                className={`form-control ${errors.name ? "is-invalid" : ""}`}
                {...register("name", { required: "Name is required." })}
              />
              {errors.name && <div className="invalid-feedback">{errors.name.message}</div>}
            </div>

            <div className="col-md-3">
              <label className="form-label" htmlFor="type">
                Type
              </label>
              <select id="type" className="form-select" {...register("type")}>
                <option value="Product">Product</option>
                <option value="Service">Service</option>
              </select>
            </div>

            <div className="col-md-3">
              <label className="form-label" htmlFor="category">
                Category
              </label>
              <input id="category" className="form-control" {...register("category")} />
            </div>

            <div className="col-md-3">
              <label className="form-label" htmlFor="brand">
                Brand
              </label>
              <input id="brand" className="form-control" {...register("brand")} />
            </div>

            <div className="col-md-3">
              <label className="form-label" htmlFor="unitOfMeasure">
                Unit of Measure
              </label>
              <input
                id="unitOfMeasure"
                className="form-control"
                placeholder="e.g. Each, Hour, Box"
                {...register("unitOfMeasure")}
              />
            </div>

            <div className="col-md-3 d-flex align-items-end">
              <div className="form-check">
                <input id="isActive" type="checkbox" className="form-check-input" {...register("isActive")} />
                <label className="form-check-label" htmlFor="isActive">
                  Active
                </label>
              </div>
            </div>
          </div>
        </FormSection>

        <FormSection icon="bi-currency-dollar" title="Pricing" description="Sell price, cost, and tax.">
          <div className="row g-3">
            <div className="col-md-4">
              <label className="form-label" htmlFor="unitPrice">
                Unit Price
              </label>
              <input
                id="unitPrice"
                type="number"
                step="0.01"
                min="0"
                className={`form-control ${errors.unitPrice ? "is-invalid" : ""}`}
                {...register("unitPrice", { required: "Unit price is required.", min: 0 })}
              />
              {errors.unitPrice && <div className="invalid-feedback">{errors.unitPrice.message}</div>}
            </div>

            <div className="col-md-4">
              <label className="form-label" htmlFor="cost">
                Cost
              </label>
              <input id="cost" type="number" step="0.01" min="0" className="form-control" {...register("cost")} />
            </div>

            <div className="col-md-4">
              <label className="form-label" htmlFor="taxPercent">
                Tax %
              </label>
              <input
                id="taxPercent"
                type="number"
                step="0.01"
                min="0"
                max="100"
                className="form-control"
                {...register("taxPercent")}
              />
            </div>
          </div>
        </FormSection>

        {type === "Service" && (
          <FormSection icon="bi-clock-history" title="Service Details" description="Duration and billing cadence.">
            <div className="row g-3">
              <div className="col-md-6">
                <label className="form-label" htmlFor="durationMinutes">
                  Typical Duration (minutes)
                </label>
                <input
                  id="durationMinutes"
                  type="number"
                  min="0"
                  className="form-control"
                  {...register("durationMinutes")}
                />
              </div>

              <div className="col-md-6">
                <label className="form-label" htmlFor="billingType">
                  Billing Type
                </label>
                <input
                  id="billingType"
                  className="form-control"
                  placeholder="e.g. One-Time, Hourly, Monthly, Annual"
                  {...register("billingType")}
                />
              </div>
            </div>
          </FormSection>
        )}

        <FormSection icon="bi-card-text" title="Description" description="Optional details shown to your sales team.">
          <textarea className="form-control" rows={4} {...register("description")} />
        </FormSection>

        <FormActionBar>
          <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
            {isSubmitting ? "Saving..." : "Save"}
          </button>
          <button type="button" className="btn btn-outline-secondary" onClick={() => navigate("/products")}>
            Cancel
          </button>
        </FormActionBar>
      </form>
    </div>
  );
}
