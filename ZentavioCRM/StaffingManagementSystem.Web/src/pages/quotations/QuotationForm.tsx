import { useEffect, useState } from "react";
import { useFieldArray, useForm } from "react-hook-form";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import {
  quotationService,
  type CreateQuotationRequest,
  type SaveQuotationLineItemRequest,
} from "@/services/quotationService";
import { userService, type ManagedUser } from "@/services/userService";
import { opportunityService, type OpportunityListItem } from "@/services/opportunityService";

type QuotationFormValues = CreateQuotationRequest;

/** yyyy-MM-dd for a native <input type="date">, or "" if null. */
function toDateInputValue(value: string | null): string {
  if (!value) return "";
  return value.slice(0, 10);
}

function lineTotal(quantity: number, unitPrice: number, discountPercent: number | null, taxPercent: number | null): number {
  const qty = Number(quantity) || 0;
  const price = Number(unitPrice) || 0;
  const discount = Number(discountPercent) || 0;
  const tax = Number(taxPercent) || 0;
  const subtotal = qty * price * (1 - discount / 100);
  return Math.round(subtotal * (1 + tax / 100) * 100) / 100;
}

const emptyLine: SaveQuotationLineItemRequest = { productName: "", quantity: 1, unitPrice: 0, discountPercent: null, taxPercent: null };

export default function QuotationForm() {
  const { id } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const isEditMode = Boolean(id);
  const navigate = useNavigate();

  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [opportunities, setOpportunities] = useState<OpportunityListItem[]>([]);
  const [opportunityName, setOpportunityName] = useState<string | null>(null);
  const [serverError, setServerError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const {
    register,
    handleSubmit,
    reset,
    control,
    watch,
    formState: { errors, isSubmitting },
  } = useForm<QuotationFormValues>({
    defaultValues: {
      opportunityId: searchParams.get("opportunityId") ?? "",
      validUntil: null,
      termsAndConditions: null,
      notes: null,
      assignedToUserId: null,
      lineItems: [emptyLine],
    },
  });

  const { fields, append, remove } = useFieldArray({ control, name: "lineItems" });
  const watchedLineItems = watch("lineItems");
  const computedTotal = watchedLineItems.reduce(
    (sum, item) => sum + lineTotal(item.quantity, item.unitPrice, item.discountPercent, item.taxPercent),
    0
  );

  useEffect(() => {
    (async () => {
      const [usersResult, opportunitiesResult] = await Promise.all([
        userService.getAll(),
        opportunityService.search({ pageSize: 200 }),
      ]);
      if (usersResult.success && usersResult.data) setUsers(usersResult.data);
      if (opportunitiesResult.success && opportunitiesResult.data) setOpportunities(opportunitiesResult.data.items);

      if (isEditMode && id) {
        const existing = await quotationService.getById(id);
        if (existing.success && existing.data) {
          const q = existing.data;
          setOpportunityName(q.opportunityName);
          reset({
            opportunityId: q.opportunityId,
            validUntil: toDateInputValue(q.validUntil) || null,
            termsAndConditions: q.termsAndConditions,
            notes: q.notes,
            assignedToUserId: q.assignedToUserId,
            lineItems: q.lineItems.map((li) => ({
              productName: li.productName,
              quantity: li.quantity,
              unitPrice: li.unitPrice,
              discountPercent: li.discountPercent,
              taxPercent: li.taxPercent,
            })),
          });
        }
      }

      setIsLoading(false);
    })();
  }, [id, isEditMode, reset]);

  const onSubmit = async (values: QuotationFormValues) => {
    setServerError(null);

    const result =
      isEditMode && id
        ? await quotationService.update(id, {
            validUntil: values.validUntil,
            termsAndConditions: values.termsAndConditions,
            notes: values.notes,
            lineItems: values.lineItems,
          })
        : await quotationService.create(values);

    if (!result.success) {
      setServerError(result.message || "Unable to save quotation.");
      return;
    }

    navigate(isEditMode && id ? `/quotations/${id}` : `/quotations/${result.data!.id}`);
  };

  if (isLoading) {
    return <div className="text-muted">Loading...</div>;
  }

  return (
    <div>
      <h1 className="h4 mb-4">{isEditMode ? "Edit Quotation" : "New Quotation"}</h1>

      <div className="card shadow-sm border-0" style={{ maxWidth: 900 }}>
        <div className="card-body">
          {serverError && <div className="alert alert-danger">{serverError}</div>}

          <form onSubmit={handleSubmit(onSubmit)} noValidate>
            <div className="row g-3">
              <div className="col-md-6">
                <label className="form-label">Opportunity</label>
                {isEditMode ? (
                  <input className="form-control" value={opportunityName ?? ""} disabled readOnly />
                ) : (
                  <select
                    className={`form-select ${errors.opportunityId ? "is-invalid" : ""}`}
                    {...register("opportunityId", { required: "An opportunity must be selected." })}
                  >
                    <option value="">Select an opportunity</option>
                    {opportunities.map((opportunity) => (
                      <option key={opportunity.id} value={opportunity.id}>
                        {opportunity.name} — {opportunity.customerName}
                      </option>
                    ))}
                  </select>
                )}
                {errors.opportunityId && <div className="invalid-feedback">{errors.opportunityId.message}</div>}
              </div>

              <div className="col-md-3">
                <label className="form-label">Valid Until</label>
                <input type="date" className="form-control" {...register("validUntil")} />
              </div>

              <div className="col-md-3">
                <label className="form-label">Owner</label>
                <select className="form-select" {...register("assignedToUserId")} disabled={isEditMode}>
                  <option value="">Unassigned</option>
                  {users.map((user) => (
                    <option key={user.id} value={user.id}>
                      {user.fullName}
                    </option>
                  ))}
                </select>
              </div>

              <div className="col-12">
                <label className="form-label">Terms &amp; Conditions</label>
                <textarea className="form-control" rows={3} {...register("termsAndConditions")} />
              </div>

              <div className="col-12">
                <label className="form-label">Notes</label>
                <textarea className="form-control" rows={2} {...register("notes")} />
              </div>
            </div>

            <hr className="my-4" />

            <div className="d-flex justify-content-between align-items-center mb-2">
              <h2 className="h6 mb-0">Line Items</h2>
              <button
                type="button"
                className="btn btn-sm btn-outline-primary"
                onClick={() => append({ ...emptyLine })}
              >
                <i className="bi bi-plus-lg me-1" aria-hidden="true" />
                Add Line
              </button>
            </div>

            {fields.map((field, index) => (
              <div className="row g-2 align-items-center mb-2" key={field.id}>
                <div className="col-md-3">
                  <input
                    className="form-control form-control-sm"
                    placeholder="Product/service"
                    {...register(`lineItems.${index}.productName`, { required: true })}
                  />
                </div>
                <div className="col-md-2">
                  <input
                    type="number"
                    step="0.01"
                    className="form-control form-control-sm"
                    placeholder="Qty"
                    {...register(`lineItems.${index}.quantity`, { valueAsNumber: true })}
                  />
                </div>
                <div className="col-md-2">
                  <input
                    type="number"
                    step="0.01"
                    className="form-control form-control-sm"
                    placeholder="Unit price"
                    {...register(`lineItems.${index}.unitPrice`, { valueAsNumber: true })}
                  />
                </div>
                <div className="col-md-2">
                  <input
                    type="number"
                    step="0.01"
                    className="form-control form-control-sm"
                    placeholder="Discount %"
                    {...register(`lineItems.${index}.discountPercent`, { valueAsNumber: true })}
                  />
                </div>
                <div className="col-md-1">
                  <input
                    type="number"
                    step="0.01"
                    className="form-control form-control-sm"
                    placeholder="Tax %"
                    {...register(`lineItems.${index}.taxPercent`, { valueAsNumber: true })}
                  />
                </div>
                <div className="col-md-1 text-end small text-muted">
                  {lineTotal(
                    watchedLineItems[index]?.quantity,
                    watchedLineItems[index]?.unitPrice,
                    watchedLineItems[index]?.discountPercent,
                    watchedLineItems[index]?.taxPercent
                  ).toLocaleString()}
                </div>
                <div className="col-md-1 text-end">
                  {fields.length > 1 && (
                    <button type="button" className="btn btn-sm btn-outline-danger" onClick={() => remove(index)}>
                      <i className="bi bi-trash" aria-hidden="true" />
                    </button>
                  )}
                </div>
              </div>
            ))}

            <div className="text-end fw-semibold mt-2">Grand Total (incl. tax): {computedTotal.toLocaleString()}</div>

            <div className="d-flex gap-2 mt-4">
              <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
                {isSubmitting ? "Saving..." : "Save"}
              </button>
              <button
                type="button"
                className="btn btn-outline-secondary"
                onClick={() => navigate(isEditMode && id ? `/quotations/${id}` : "/quotations")}
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
