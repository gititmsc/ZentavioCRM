import { useEffect, useState } from "react";
import { useFieldArray, useForm } from "react-hook-form";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import { opportunityService, type OpportunityContactRole, type SaveOpportunityRequest } from "@/services/opportunityService";
import { userService, type ManagedUser } from "@/services/userService";
import { customerService, type CustomerListItem, type ContactPerson } from "@/services/customerService";

/** yyyy-MM-dd for a native <input type="date">, or "" if null. */
function toDateInputValue(value: string | null): string {
  if (!value) return "";
  return value.slice(0, 10);
}

function lineTotal(quantity: number, unitPrice: number, discountPercent: number | null): number {
  const qty = Number(quantity) || 0;
  const price = Number(unitPrice) || 0;
  const discount = Number(discountPercent) || 0;
  return Math.round(qty * price * (1 - discount / 100) * 100) / 100;
}

const OPPORTUNITY_CONTACT_ROLES: { value: OpportunityContactRole; label: string }[] = [
  { value: "Champion", label: "Champion" },
  { value: "EconomicBuyer", label: "Economic Buyer" },
  { value: "Blocker", label: "Blocker" },
  { value: "Influencer", label: "Influencer" },
  { value: "DecisionMaker", label: "Decision Maker" },
  { value: "TechnicalEvaluator", label: "Technical Evaluator" },
  { value: "Other", label: "Other" },
];

export default function OpportunityForm() {
  const { id } = useParams<{ id: string }>();
  const [searchParams] = useSearchParams();
  const isEditMode = Boolean(id);
  const navigate = useNavigate();

  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [customers, setCustomers] = useState<CustomerListItem[]>([]);
  const [customerContacts, setCustomerContacts] = useState<ContactPerson[]>([]);
  const [serverError, setServerError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const {
    register,
    handleSubmit,
    reset,
    control,
    watch,
    setValue,
    getValues,
    formState: { errors, isSubmitting },
  } = useForm<SaveOpportunityRequest>({
    defaultValues: {
      name: "",
      customerId: searchParams.get("customerId") ?? "",
      value: null,
      currencyCode: null,
      probability: null,
      products: null,
      competitors: null,
      expectedCloseDate: null,
      assignedToUserId: null,
      notes: null,
      nextStep: null,
      nextStepDate: null,
      lineItems: [],
      contacts: [],
    },
  });

  const { fields, append, remove } = useFieldArray({ control, name: "lineItems" });
  const contactsArray = useFieldArray({ control, name: "contacts" });
  const watchedLineItems = watch("lineItems");
  const watchedCustomerId = watch("customerId");
  const hasLineItems = watchedLineItems.length > 0;
  const computedTotal = watchedLineItems.reduce(
    (sum, item) => sum + lineTotal(item.quantity, item.unitPrice, item.discountPercent),
    0
  );

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
            currencyCode: o.currencyCode,
            probability: o.probability,
            products: o.products,
            competitors: o.competitors,
            expectedCloseDate: toDateInputValue(o.expectedCloseDate) || null,
            assignedToUserId: o.assignedToUserId,
            notes: o.notes,
            nextStep: o.nextStep,
            nextStepDate: toDateInputValue(o.nextStepDate) || null,
            lineItems: o.lineItems.map((li) => ({
              productName: li.productName,
              quantity: li.quantity,
              unitPrice: li.unitPrice,
              discountPercent: li.discountPercent,
            })),
            contacts: o.contacts.map((c) => ({
              contactPersonId: c.contactPersonId,
              role: c.role,
              notes: c.notes,
            })),
          });
        }
      }

      setIsLoading(false);
    })();
  }, [id, isEditMode, reset]);

  // Keep the buying-committee contact picker (and the default currency, on new opportunities)
  // in sync with whichever customer is currently selected.
  useEffect(() => {
    if (!watchedCustomerId) {
      setCustomerContacts([]);
      return;
    }

    (async () => {
      const result = await customerService.getById(watchedCustomerId);
      if (result.success && result.data) {
        setCustomerContacts(result.data.contacts);
        if (!isEditMode && !getValues("currencyCode")) {
          setValue("currencyCode", result.data.currencyCode);
        }
      }
    })();
  }, [watchedCustomerId, isEditMode, getValues, setValue]);

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

      <div className="card shadow-sm border-0" style={{ maxWidth: 900 }}>
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
                <label className="form-label">
                  Value {hasLineItems && <span className="text-muted small">(computed from line items)</span>}
                </label>
                {hasLineItems ? (
                  <input type="number" className="form-control" value={computedTotal} readOnly disabled />
                ) : (
                  <input type="number" step="0.01" className="form-control" {...register("value")} />
                )}
              </div>

              <div className="col-md-2">
                <label className="form-label">Currency</label>
                <input
                  className="form-control"
                  placeholder="USD"
                  maxLength={10}
                  {...register("currencyCode")}
                />
              </div>

              <div className="col-md-2">
                <label className="form-label">Probability (%)</label>
                <input type="number" min={0} max={100} className="form-control" {...register("probability")} />
              </div>

              <div className="col-md-4">
                <label className="form-label">Expected Close Date</label>
                <input type="date" className="form-control" {...register("expectedCloseDate")} />
              </div>

              <div className="col-md-6">
                <label className="form-label">Products (summary)</label>
                <input className="form-control" placeholder="Short free-text summary" {...register("products")} />
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

              <div className="col-md-6" />

              <div className="col-md-8">
                <label className="form-label">Next Step</label>
                <input className="form-control" placeholder="What happens next to move this deal forward" {...register("nextStep")} />
              </div>

              <div className="col-md-4">
                <label className="form-label">Next Step Date</label>
                <input type="date" className="form-control" {...register("nextStepDate")} />
              </div>

              <div className="col-12">
                <label className="form-label">Notes</label>
                <textarea className="form-control" rows={3} {...register("notes")} />
              </div>
            </div>

            <hr className="my-4" />

            <div className="d-flex justify-content-between align-items-center mb-2">
              <h2 className="h6 mb-0">Line Items</h2>
              <button
                type="button"
                className="btn btn-sm btn-outline-primary"
                onClick={() => append({ productName: "", quantity: 1, unitPrice: 0, discountPercent: null })}
              >
                <i className="bi bi-plus-lg me-1" aria-hidden="true" />
                Add Line
              </button>
            </div>

            {fields.length === 0 && (
              <div className="text-muted small mb-2">
                No line items — Value above is entered manually. Add a line item to switch to computed pricing.
              </div>
            )}

            {fields.map((field, index) => (
              <div className="row g-2 align-items-center mb-2" key={field.id}>
                <div className="col-md-4">
                  <input
                    className={`form-control form-control-sm ${
                      errors.lineItems?.[index]?.productName ? "is-invalid" : ""
                    }`}
                    placeholder="Product/service"
                    {...register(`lineItems.${index}.productName`, { required: "Required" })}
                  />
                  {errors.lineItems?.[index]?.productName && (
                    <div className="invalid-feedback">Product/service name is required.</div>
                  )}
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
                <div className="col-md-1 text-end small text-muted">
                  {lineTotal(
                    watchedLineItems[index]?.quantity,
                    watchedLineItems[index]?.unitPrice,
                    watchedLineItems[index]?.discountPercent
                  ).toLocaleString()}
                </div>
                <div className="col-md-1 text-end">
                  <button type="button" className="btn btn-sm btn-outline-danger" onClick={() => remove(index)}>
                    <i className="bi bi-trash" aria-hidden="true" />
                  </button>
                </div>
              </div>
            ))}

            {hasLineItems && (
              <div className="text-end fw-semibold mt-2">Total: {computedTotal.toLocaleString()}</div>
            )}

            <hr className="my-4" />

            <div className="d-flex justify-content-between align-items-center mb-2">
              <h2 className="h6 mb-0">Buying Committee</h2>
              <button
                type="button"
                className="btn btn-sm btn-outline-primary"
                disabled={customerContacts.length === 0}
                onClick={() => contactsArray.append({ contactPersonId: "", role: "Other", notes: null })}
              >
                <i className="bi bi-plus-lg me-1" aria-hidden="true" />
                Add Contact
              </button>
            </div>

            {customerContacts.length === 0 && (
              <div className="text-muted small mb-2">
                {watchedCustomerId
                  ? "This customer has no contacts yet — add one from the customer's edit screen first."
                  : "Select a customer above to add buying-committee members."}
              </div>
            )}

            {contactsArray.fields.length === 0 && customerContacts.length > 0 && (
              <div className="text-muted small mb-2">
                No buying-committee members added yet — track who's the champion, economic buyer, or a blocker on this deal.
              </div>
            )}

            {contactsArray.fields.map((field, index) => (
              <div className="row g-2 align-items-center mb-2" key={field.id}>
                <div className="col-md-4">
                  <select
                    className={`form-select form-select-sm ${
                      errors.contacts?.[index]?.contactPersonId ? "is-invalid" : ""
                    }`}
                    {...register(`contacts.${index}.contactPersonId`, { required: "Required" })}
                  >
                    <option value="">Select a contact</option>
                    {customerContacts.map((contact) => (
                      <option key={contact.id} value={contact.id}>
                        {contact.firstName} {contact.lastName}
                        {contact.designation ? ` — ${contact.designation}` : ""}
                      </option>
                    ))}
                  </select>
                  {errors.contacts?.[index]?.contactPersonId && (
                    <div className="invalid-feedback">A buying-committee contact must be selected.</div>
                  )}
                </div>
                <div className="col-md-3">
                  <select className="form-select form-select-sm" {...register(`contacts.${index}.role`)}>
                    {OPPORTUNITY_CONTACT_ROLES.map((role) => (
                      <option key={role.value} value={role.value}>
                        {role.label}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="col-md-4">
                  <input
                    className="form-control form-control-sm"
                    placeholder="Notes (optional)"
                    {...register(`contacts.${index}.notes`)}
                  />
                </div>
                <div className="col-md-1 text-end">
                  <button
                    type="button"
                    className="btn btn-sm btn-outline-danger"
                    onClick={() => contactsArray.remove(index)}
                  >
                    <i className="bi bi-trash" aria-hidden="true" />
                  </button>
                </div>
              </div>
            ))}

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
