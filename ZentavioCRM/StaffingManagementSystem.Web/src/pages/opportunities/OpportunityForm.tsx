import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import { opportunityService, type SaveOpportunityRequest } from "@/services/opportunityService";
import { userService, type ManagedUser } from "@/services/userService";
import { customerService, type CustomerListItem } from "@/services/customerService";

/** yyyy-MM-dd for a native <input type="date">, or "" if null. */
function toDateInputValue(value: string | null): string {
  if (!value) return "";
  return value.slice(0, 10);
}

export default function OpportunityForm() {
  const { id } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const isEditMode = Boolean(id);
  const navigate = useNavigate();

  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [customers, setCustomers] = useState<CustomerListItem[]>([]);
  const [serverError, setServerError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<SaveOpportunityRequest>({
    defaultValues: {
      name: "",
      customerId: searchParams.get("customerId") ?? "",
      value: null,
      probability: null,
      products: null,
      competitors: null,
      expectedCloseDate: null,
      assignedToUserId: null,
      notes: null,
    },
  });

  useEffect(() => {
    (async () => {
      const [usersResult, customersResult] = await Promise.all([
        userService.getAll(),
        customerService.search({ pageSize: 200 }),
      ]);
      if (usersResult.success && usersResult.data) setUsers(usersResult.data);
      if (customersResult.success && customersResult.data) setCustomers(customersResult.data.items);

      if (isEditMode && id) {
        const existing = await opportunityService.getById(id);
        if (existing.success && existing.data) {
          const o = existing.data;
          reset({
            name: o.name,
            customerId: o.customerId,
            value: o.value,
            probability: o.probability,
            products: o.products,
            competitors: o.competitors,
            expectedCloseDate: toDateInputValue(o.expectedCloseDate) || null,
            assignedToUserId: o.assignedToUserId,
            notes: o.notes,
          });
        }
      }

      setIsLoading(false);
    })();
  }, [id, isEditMode, reset]);

  const onSubmit = async (values: SaveOpportunityRequest) => {
    setServerError(null);

    const result = isEditMode && id
      ? await opportunityService.update(id, values)
      : await opportunityService.create(values);

    if (!result.success) {
      setServerError(result.message || "Unable to save opportunity.");
      return;
    }

    navigate(isEditMode && id ? `/opportunities/${id}` : "/opportunities");
  };

  if (isLoading) {
    return <div className="text-muted">Loading...</div>;
  }

  return (
    <div>
      <h1 className="h4 mb-4">{isEditMode ? "Edit Opportunity" : "New Opportunity"}</h1>

      <div className="card shadow-sm border-0" style={{ maxWidth: 780 }}>
        <div className="card-body">
          {serverError && <div className="alert alert-danger">{serverError}</div>}

          <form onSubmit={handleSubmit(onSubmit)} noValidate>
            <div className="row g-3">
              <div className="col-md-6">
                <label className="form-label">Opportunity Name</label>
                <input
                  className={`form-control ${errors.name ? "is-invalid" : ""}`}
                  {...register("name", { required: "Opportunity name is required." })}
                />
                {errors.name && <div className="invalid-feedback">{errors.name.message}</div>}
              </div>

              <div className="col-md-6">
                <label className="form-label">Customer</label>
                <select
                  className={`form-select ${errors.customerId ? "is-invalid" : ""}`}
                  {...register("customerId", { required: "A customer must be selected." })}
                >
                  <option value="">Select a customer</option>
                  {customers.map((customer) => (
                    <option key={customer.id} value={customer.id}>
                      {customer.displayName}
                    </option>
                  ))}
                </select>
                {errors.customerId && <div className="invalid-feedback">{errors.customerId.message}</div>}
              </div>

              <div className="col-md-4">
                <label className="form-label">Value</label>
                <input type="number" step="0.01" className="form-control" {...register("value")} />
              </div>

              <div className="col-md-4">
                <label className="form-label">Probability (%)</label>
                <input type="number" min={0} max={100} className="form-control" {...register("probability")} />
              </div>

              <div className="col-md-4">
                <label className="form-label">Expected Close Date</label>
                <input type="date" className="form-control" {...register("expectedCloseDate")} />
              </div>

              <div className="col-md-6">
                <label className="form-label">Products</label>
                <input className="form-control" placeholder="Products/services in scope" {...register("products")} />
              </div>

              <div className="col-md-6">
                <label className="form-label">Competitors</label>
                <input className="form-control" {...register("competitors")} />
              </div>

              <div className="col-md-6">
                <label className="form-label">Owner</label>
                <select className="form-select" {...register("assignedToUserId")}>
                  <option value="">Unassigned</option>
                  {users.map((user) => (
                    <option key={user.id} value={user.id}>
                      {user.fullName}
                    </option>
                  ))}
                </select>
              </div>

              <div className="col-12">
                <label className="form-label">Notes</label>
                <textarea className="form-control" rows={3} {...register("notes")} />
              </div>
            </div>

            <div className="d-flex gap-2 mt-4">
              <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
                {isSubmitting ? "Saving..." : "Save"}
              </button>
              <button
                type="button"
                className="btn btn-outline-secondary"
                onClick={() => navigate(isEditMode && id ? `/opportunities/${id}` : "/opportunities")}
              >
                Cancel
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
}
