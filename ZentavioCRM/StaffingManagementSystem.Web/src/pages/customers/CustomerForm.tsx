import { useEffect, useState } from "react";
import { useFieldArray, useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router-dom";
import {
  customerService,
  type CustomerHealthStatus,
  type CustomerType,
  type PreferredContactMethod,
  type SaveCustomerRequest,
} from "@/services/customerService";
import type { LeadSource } from "@/services/leadService";
import { userService, type ManagedUser } from "@/services/userService";
import { DocumentsPanel } from "@/components/documents/DocumentsPanel";
import { HistoryPanel } from "@/components/history/HistoryPanel";

/** yyyy-MM-dd for a native <input type="date">, or "" if null. */
function toDateInputValue(value: string | null): string {
  if (!value) return "";
  return value.slice(0, 10);
}

const CUSTOMER_TYPES: CustomerType[] = [
  "Prospect",
  "Individual",
  "Business",
  "Vendor",
  "Partner",
  "Supplier",
  "Distributor",
  "Dealer",
  "Franchise",
  "Consultant",
];

const HEALTH_STATUSES: { value: CustomerHealthStatus; label: string }[] = [
  { value: "Hot", label: "Hot Account" },
  { value: "Warm", label: "Warm" },
  { value: "Cold", label: "Cold" },
  { value: "AtRisk", label: "At Risk" },
];

const PREFERRED_CONTACT_METHODS: PreferredContactMethod[] = ["Email", "Mobile", "WhatsApp", "LinkedIn"];

const ACQUISITION_SOURCES: LeadSource[] = [
  "Website",
  "LandingPage",
  "Referral",
  "Exhibition",
  "WhatsApp",
  "Facebook",
  "LinkedIn",
  "EmailCampaign",
  "GoogleAds",
  "ManualEntry",
  "ApiIntegration",
];

export default function CustomerForm() {
  const { id } = useParams<{ id: string }>();
  const isEditMode = Boolean(id);
  const navigate = useNavigate();

  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [serverError, setServerError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(isEditMode);

  const {
    register,
    control,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<SaveCustomerRequest>({
    defaultValues: {
      type: "Prospect",
      legalName: "",
      displayName: "",
      industry: null,
      website: null,
      email: null,
      phone: null,
      taxNumber: null,
      employeesCount: null,
      annualRevenue: null,
      currencyCode: "USD",
      paymentTermsDays: null,
      creditLimit: null,
      rating: null,
      tags: null,
      acquisitionSource: null,
      healthStatus: null,
      assignedToUserId: null,
      isActive: true,
      contacts: [],
      addresses: [],
    },
  });

  const contactsArray = useFieldArray({ control, name: "contacts" });
  const addressesArray = useFieldArray({ control, name: "addresses" });

  useEffect(() => {
    (async () => {
      const usersResult = await userService.getAll();
      if (usersResult.success && usersResult.data) setUsers(usersResult.data);

      if (isEditMode && id) {
        const existing = await customerService.getById(id);
        if (existing.success && existing.data) {
          const c = existing.data;
          reset({
            type: c.type,
            legalName: c.legalName,
            displayName: c.displayName,
            industry: c.industry,
            website: c.website,
            email: c.email,
            phone: c.phone,
            taxNumber: c.taxNumber,
            employeesCount: c.employeesCount,
            annualRevenue: c.annualRevenue,
            currencyCode: c.currencyCode,
            paymentTermsDays: c.paymentTermsDays,
            creditLimit: c.creditLimit,
            rating: c.rating,
            tags: c.tags,
            acquisitionSource: c.acquisitionSource,
            healthStatus: c.healthStatus,
            assignedToUserId: c.assignedToUserId,
            isActive: c.isActive,
            contacts: c.contacts.map(({ id: _cid, dateOfBirth, anniversaryDate, ...rest }) => ({
              ...rest,
              dateOfBirth: toDateInputValue(dateOfBirth),
              anniversaryDate: toDateInputValue(anniversaryDate),
            })),
            addresses: c.addresses.map(({ id: _aid, ...rest }) => rest),
          });
        }
      }

      setIsLoading(false);
    })();
  }, [id, isEditMode, reset]);

  const onSubmit = async (values: SaveCustomerRequest) => {
    setServerError(null);

    const request: SaveCustomerRequest = {
      ...values,
      assignedToUserId: values.assignedToUserId || null,
      acquisitionSource: values.acquisitionSource || null,
      healthStatus: values.healthStatus || null,
      contacts: values.contacts.map((c) => ({
        ...c,
        preferredContactMethod: c.preferredContactMethod || null,
        dateOfBirth: c.dateOfBirth || null,
        anniversaryDate: c.anniversaryDate || null,
      })),
    };

    const result = isEditMode && id ? await customerService.update(id, request) : await customerService.create(request);

    if (!result.success) {
      setServerError(result.message || "Unable to save customer.");
      return;
    }

    navigate("/customers");
  };

  if (isLoading) {
    return <div className="text-muted">Loading...</div>;
  }

  return (
    <div>
      <h1 className="h4 mb-4">{isEditMode ? "Edit Customer" : "New Customer"}</h1>

      {serverError && <div className="alert alert-danger">{serverError}</div>}

      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        <div className="card shadow-sm border-0 mb-4">
          <div className="card-header bg-white fw-semibold">Company Details</div>
          <div className="card-body row g-3">
            <div className="col-md-4">
              <label className="form-label">Legal Name</label>
              <input
                className={`form-control ${errors.legalName ? "is-invalid" : ""}`}
                {...register("legalName", { required: "Legal name is required." })}
              />
              {errors.legalName && <div className="invalid-feedback">{errors.legalName.message}</div>}
            </div>

            <div className="col-md-4">
              <label className="form-label">Display Name</label>
              <input className="form-control" placeholder="Defaults to legal name" {...register("displayName")} />
            </div>

            <div className="col-md-4">
              <label className="form-label">Type</label>
              <select className="form-select" {...register("type")}>
                {CUSTOMER_TYPES.map((type) => (
                  <option key={type} value={type}>
                    {type}
                  </option>
                ))}
              </select>
            </div>

            <div className="col-md-4">
              <label className="form-label">Industry</label>
              <input className="form-control" {...register("industry")} />
            </div>

            <div className="col-md-4">
              <label className="form-label">Website</label>
              <input className="form-control" {...register("website")} />
            </div>

            <div className="col-md-4">
              <label className="form-label">Email</label>
              <input type="email" className="form-control" {...register("email")} />
            </div>

            <div className="col-md-4">
              <label className="form-label">Phone</label>
              <input className="form-control" {...register("phone")} />
            </div>

            <div className="col-md-4">
              <label className="form-label">Tax Number</label>
              <input className="form-control" {...register("taxNumber")} />
            </div>

            <div className="col-md-4">
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

            <div className="col-md-3">
              <label className="form-label">Employees</label>
              <input type="number" className="form-control" {...register("employeesCount")} />
            </div>

            <div className="col-md-3">
              <label className="form-label">Annual Revenue</label>
              <input type="number" step="0.01" className="form-control" {...register("annualRevenue")} />
            </div>

            <div className="col-md-3">
              <label className="form-label">Payment Terms (days)</label>
              <input type="number" className="form-control" {...register("paymentTermsDays")} />
            </div>

            <div className="col-md-3">
              <label className="form-label">Credit Limit</label>
              <input type="number" step="0.01" className="form-control" {...register("creditLimit")} />
            </div>

            <div className="col-md-3">
              <label className="form-label">Rating</label>
              <select className="form-select" {...register("rating")}>
                <option value="">Not rated</option>
                <option value="Hot">Hot</option>
                <option value="Warm">Warm</option>
                <option value="Cold">Cold</option>
              </select>
            </div>

            <div className="col-md-3">
              <label className="form-label">Health Status</label>
              <select className="form-select" {...register("healthStatus")}>
                <option value="">Not set</option>
                {HEALTH_STATUSES.map((status) => (
                  <option key={status.value} value={status.value}>
                    {status.label}
                  </option>
                ))}
              </select>
            </div>

            <div className="col-md-3 d-flex align-items-end">
              <div className="form-check">
                <input id="isActive" type="checkbox" className="form-check-input" {...register("isActive")} />
                <label className="form-check-label" htmlFor="isActive">
                  Active
                </label>
              </div>
            </div>

            <div className="col-md-6">
              <label className="form-label">
                Tags <span className="text-muted small">(comma-separated, e.g. VIP, At Risk)</span>
              </label>
              <input className="form-control" placeholder="VIP, Hot Account, At Risk" {...register("tags")} />
            </div>

            <div className="col-md-6">
              <label className="form-label">Acquisition Source</label>
              <select className="form-select" {...register("acquisitionSource")}>
                <option value="">Unknown / not set</option>
                {ACQUISITION_SOURCES.map((source) => (
                  <option key={source} value={source}>
                    {source}
                  </option>
                ))}
              </select>
            </div>
          </div>
        </div>

        <div className="card shadow-sm border-0 mb-4">
          <div className="card-header bg-white d-flex justify-content-between align-items-center">
            <span className="fw-semibold">Contacts</span>
            <button
              type="button"
              className="btn btn-sm btn-outline-primary"
              onClick={() =>
                contactsArray.append({
                  firstName: "",
                  lastName: "",
                  designation: null,
                  department: null,
                  email: null,
                  mobile: null,
                  whatsApp: null,
                  linkedIn: null,
                  isPrimary: contactsArray.fields.length === 0,
                  isDecisionMaker: false,
                  preferredContactMethod: null,
                  dateOfBirth: null,
                  anniversaryDate: null,
                  notes: null,
                })
              }
            >
              <i className="bi bi-plus-lg me-1" aria-hidden="true" />
              Add Contact
            </button>
          </div>
          <div className="card-body">
            {contactsArray.fields.length === 0 && <div className="text-muted">No contacts added yet.</div>}
            {contactsArray.fields.map((field, index) => (
              <div className="row g-2 align-items-end border-bottom pb-3 mb-3" key={field.id}>
                <div className="col-md-3">
                  <label className="form-label small">First Name</label>
                  <input className="form-control form-control-sm" {...register(`contacts.${index}.firstName`)} />
                </div>
                <div className="col-md-3">
                  <label className="form-label small">Last Name</label>
                  <input className="form-control form-control-sm" {...register(`contacts.${index}.lastName`)} />
                </div>
                <div className="col-md-3">
                  <label className="form-label small">Designation</label>
                  <input className="form-control form-control-sm" {...register(`contacts.${index}.designation`)} />
                </div>
                <div className="col-md-3">
                  <label className="form-label small">Email</label>
                  <input className="form-control form-control-sm" {...register(`contacts.${index}.email`)} />
                </div>
                <div className="col-md-3">
                  <label className="form-label small">Mobile</label>
                  <input className="form-control form-control-sm" {...register(`contacts.${index}.mobile`)} />
                </div>
                <div className="col-md-3">
                  <label className="form-label small">Preferred Contact</label>
                  <select className="form-select form-select-sm" {...register(`contacts.${index}.preferredContactMethod`)}>
                    <option value="">Not set</option>
                    {PREFERRED_CONTACT_METHODS.map((method) => (
                      <option key={method} value={method}>
                        {method}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="col-md-3">
                  <label className="form-label small">Birthday</label>
                  <input
                    type="date"
                    className="form-control form-control-sm"
                    {...register(`contacts.${index}.dateOfBirth`)}
                  />
                </div>
                <div className="col-md-3">
                  <label className="form-label small">Anniversary</label>
                  <input
                    type="date"
                    className="form-control form-control-sm"
                    {...register(`contacts.${index}.anniversaryDate`)}
                  />
                </div>
                <div className="col-md-2 form-check">
                  <input
                    type="checkbox"
                    className="form-check-input"
                    id={`contact-primary-${field.id}`}
                    {...register(`contacts.${index}.isPrimary`)}
                  />
                  <label className="form-check-label small" htmlFor={`contact-primary-${field.id}`}>
                    Primary
                  </label>
                </div>
                <div className="col-md-2 form-check">
                  <input
                    type="checkbox"
                    className="form-check-input"
                    id={`contact-dm-${field.id}`}
                    {...register(`contacts.${index}.isDecisionMaker`)}
                  />
                  <label className="form-check-label small" htmlFor={`contact-dm-${field.id}`}>
                    Decision Maker
                  </label>
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
          </div>
        </div>

        <div className="card shadow-sm border-0 mb-4">
          <div className="card-header bg-white d-flex justify-content-between align-items-center">
            <span className="fw-semibold">Addresses</span>
            <button
              type="button"
              className="btn btn-sm btn-outline-primary"
              onClick={() =>
                addressesArray.append({
                  type: "Billing",
                  line1: "",
                  line2: null,
                  city: null,
                  state: null,
                  country: null,
                  postalCode: null,
                  isPrimary: addressesArray.fields.length === 0,
                })
              }
            >
              <i className="bi bi-plus-lg me-1" aria-hidden="true" />
              Add Address
            </button>
          </div>
          <div className="card-body">
            {addressesArray.fields.length === 0 && <div className="text-muted">No addresses added yet.</div>}
            {addressesArray.fields.map((field, index) => (
              <div className="row g-2 align-items-end border-bottom pb-3 mb-3" key={field.id}>
                <div className="col-md-2">
                  <label className="form-label small">Type</label>
                  <select className="form-select form-select-sm" {...register(`addresses.${index}.type`)}>
                    <option value="Billing">Billing</option>
                    <option value="Shipping">Shipping</option>
                    <option value="RegisteredOffice">Registered Office</option>
                    <option value="BranchOffice">Branch Office</option>
                    <option value="Warehouse">Warehouse</option>
                    <option value="Site">Site</option>
                  </select>
                </div>
                <div className="col-md-3">
                  <label className="form-label small">Address Line 1</label>
                  <input className="form-control form-control-sm" {...register(`addresses.${index}.line1`)} />
                </div>
                <div className="col-md-2">
                  <label className="form-label small">City</label>
                  <input className="form-control form-control-sm" {...register(`addresses.${index}.city`)} />
                </div>
                <div className="col-md-2">
                  <label className="form-label small">State</label>
                  <input className="form-control form-control-sm" {...register(`addresses.${index}.state`)} />
                </div>
                <div className="col-md-2">
                  <label className="form-label small">Country</label>
                  <input className="form-control form-control-sm" {...register(`addresses.${index}.country`)} />
                </div>
                <div className="col-md-1 text-end">
                  <button
                    type="button"
                    className="btn btn-sm btn-outline-danger"
                    onClick={() => addressesArray.remove(index)}
                  >
                    <i className="bi bi-trash" aria-hidden="true" />
                  </button>
                </div>
              </div>
            ))}
          </div>
        </div>

        <div className="d-flex gap-2 mb-4">
          <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
            {isSubmitting ? "Saving..." : "Save"}
          </button>
          <button type="button" className="btn btn-outline-secondary" onClick={() => navigate("/customers")}>
            Cancel
          </button>
        </div>
      </form>

      {isEditMode && id && (
        <>
          <DocumentsPanel entityType="Customer" entityId={id} />
          <HistoryPanel entityType="Customer" entityId={id} />
        </>
      )}
    </div>
  );
}
