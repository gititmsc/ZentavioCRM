import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useNavigate, useParams } from "react-router-dom";
import {
  leadService,
  type CustomerLookupContact,
  type CustomerLookupMatch,
  type DuplicateMatch,
  type LeadSource,
  type SaveLeadRequest,
} from "@/services/leadService";
import { userService, type ManagedUser } from "@/services/userService";
import { territoryService, type Territory } from "@/services/territoryService";
import { emailPatternRule } from "@/utils/validation";
import { allServerFieldErrorsMatched, applyServerFieldErrors } from "@/utils/serverErrors";
import { PageHeader } from "@/components/layout/PageHeader";
import { FormSection } from "@/components/form/FormSection";
import { FormActionBar } from "@/components/form/FormActionBar";

/** Every SaveLeadRequest key, used to match server-side field errors back onto this form's inputs. */
const LEAD_FORM_FIELDS = [
  "companyName",
  "contactName",
  "email",
  "mobile",
  "industry",
  "source",
  "campaign",
  "utmSource",
  "utmMedium",
  "utmCampaign",
  "utmTerm",
  "utmContent",
  "budget",
  "timeline",
  "expectedValue",
  "assignedToUserId",
  "territory",
  "territoryId",
  "notes",
  "nextFollowUpDate",
  "linkedCustomerId",
  "linkedContactId",
] as const satisfies readonly (keyof SaveLeadRequest)[];

/** yyyy-MM-dd for a native <input type="date">, or "" if null. */
function toDateInputValue(value: string | null): string {
  if (!value) return "";
  return value.slice(0, 10);
}

const SOURCES: LeadSource[] = [
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

export default function LeadForm() {
  const { id } = useParams<{ id: string }>();
  const isEditMode = Boolean(id);
  const navigate = useNavigate();

  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [territories, setTerritories] = useState<Territory[]>([]);
  const [serverError, setServerError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(isEditMode);
  const [duplicateMatches, setDuplicateMatches] = useState<DuplicateMatch[]>([]);
  const [duplicatesDismissed, setDuplicatesDismissed] = useState(false);
  const [customerMatches, setCustomerMatches] = useState<CustomerLookupMatch[]>([]);
  const [linkedCustomer, setLinkedCustomer] = useState<{ id: string; displayName: string; contactName?: string } | null>(
    null
  );

  const {
    register,
    handleSubmit,
    reset,
    getValues,
    setValue,
    watch,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<SaveLeadRequest>({
    defaultValues: {
      companyName: "",
      contactName: "",
      email: null,
      mobile: null,
      industry: null,
      source: "ManualEntry",
      campaign: null,
      utmSource: null,
      utmMedium: null,
      utmCampaign: null,
      utmTerm: null,
      utmContent: null,
      budget: null,
      timeline: null,
      expectedValue: null,
      assignedToUserId: null,
      territory: null,
      territoryId: null,
      notes: null,
      nextFollowUpDate: null,
      linkedCustomerId: null,
      linkedContactId: null,
    },
  });

  /** Drives the "will update vs. will add a new contact" wording on the linked-customer banner — reflects the actual save-time behavior in both the just-matched and loaded-from-an-existing-lead cases. */
  const linkedContactIdValue = watch("linkedContactId");

  useEffect(() => {
    (async () => {
      const usersResult = await userService.getAll();
      if (usersResult.success && usersResult.data) setUsers(usersResult.data);

      const territoriesResult = await territoryService.getAll();
      if (territoriesResult.success && territoriesResult.data) setTerritories(territoriesResult.data);

      if (isEditMode && id) {
        const existing = await leadService.getById(id);
        if (existing.success && existing.data) {
          const l = existing.data;
          reset({
            companyName: l.companyName,
            contactName: l.contactName,
            email: l.email,
            mobile: l.mobile,
            industry: l.industry,
            source: l.source,
            campaign: l.campaign,
            utmSource: l.utmSource,
            utmMedium: l.utmMedium,
            utmCampaign: l.utmCampaign,
            utmTerm: l.utmTerm,
            utmContent: l.utmContent,
            budget: l.budget,
            timeline: l.timeline,
            expectedValue: l.expectedValue,
            assignedToUserId: l.assignedToUserId,
            territory: l.territory,
            territoryId: l.territoryId,
            notes: l.notes,
            nextFollowUpDate: toDateInputValue(l.nextFollowUpDate) || null,
            linkedCustomerId: l.linkedCustomerId,
            linkedContactId: l.linkedContactId,
          });
          if (l.linkedCustomerId && l.linkedCustomerName) {
            setLinkedCustomer({ id: l.linkedCustomerId, displayName: l.linkedCustomerName });
          }
        }
      }

      setIsLoading(false);
    })();
  }, [id, isEditMode, reset]);

  const checkForDuplicates = async () => {
    const { email, mobile } = getValues();
    if (!email && !mobile) return;

    const result = await leadService.checkDuplicates(email, mobile, isEditMode ? id : undefined);
    if (result.success && result.data) {
      setDuplicateMatches(result.data.matches);
      setDuplicatesDismissed(false);
    }
  };

  const checkForCustomerMatches = async () => {
    const companyName = getValues("companyName")?.trim();
    if (!companyName || companyName.length < 2 || linkedCustomer) return;

    const result = await leadService.lookupCustomers(companyName);
    if (result.success && result.data) {
      // Exactly one company matches and it has exactly one contact on file — nothing to ask,
      // link and prefill immediately instead of showing a picker of one. The contact fields
      // stay editable below; saving will update this same contact, not create a duplicate.
      if (result.data.length === 1 && result.data[0].contacts.length === 1) {
        applyCustomerMatch(result.data[0], result.data[0].contacts[0]);
        return;
      }
      setCustomerMatches(result.data);
    }
  };

  /** Selecting a match links the lead to that customer and prefills from it — the contact's own details if a specific contact was picked, otherwise the company-level Email/Phone/Industry. */
  const applyCustomerMatch = (customer: CustomerLookupMatch, contact?: CustomerLookupContact) => {
    if (contact) {
      if (contact.fullName) setValue("contactName", contact.fullName);
      if (contact.email) setValue("email", contact.email);
      if (contact.mobile) setValue("mobile", contact.mobile);
    } else {
      if (customer.email) setValue("email", customer.email);
      if (customer.phone) setValue("mobile", customer.phone);
    }
    if (customer.industry) setValue("industry", customer.industry);
    setValue("linkedCustomerId", customer.id);
    setValue("linkedContactId", contact?.id ?? null);
    setLinkedCustomer({ id: customer.id, displayName: customer.displayName, contactName: contact?.fullName });
    setCustomerMatches([]);
  };

  const clearLinkedCustomer = () => {
    setValue("linkedCustomerId", null);
    setValue("linkedContactId", null);
    setLinkedCustomer(null);
  };

  const onSubmit = async (values: SaveLeadRequest) => {
    setServerError(null);

    const result = isEditMode && id ? await leadService.update(id, values) : await leadService.create(values);

    if (!result.success) {
      const matched = applyServerFieldErrors(setError, result.fieldErrors, LEAD_FORM_FIELDS);
      if (!allServerFieldErrorsMatched(result.fieldErrors, matched)) {
        setServerError(result.message || "Unable to save lead.");
      }
      return;
    }

    navigate(isEditMode && id ? `/leads/${id}` : "/leads");
  };

  if (isLoading) {
    return <div className="text-muted">Loading...</div>;
  }

  const backTo = isEditMode && id ? `/leads/${id}` : "/leads";

  return (
    <div>
      <PageHeader
        title={isEditMode ? "Edit Lead" : "New Lead"}
        subtitle={isEditMode ? "Update this lead's details, tracking info, and assignment." : "Capture a new inbound or outbound lead."}
        backTo={backTo}
        backLabel="Back to Leads"
      />

      <div style={{ maxWidth: 860 }}>
        {serverError && <div className="alert alert-danger">{serverError}</div>}

        {!duplicatesDismissed && duplicateMatches.length > 0 && (
          <div className="alert alert-warning d-flex justify-content-between align-items-start">
            <div>
              <strong>Possible duplicate{duplicateMatches.length > 1 ? "s" : ""} found:</strong>{" "}
              {duplicateMatches.map((match, i) => (
                <span key={match.id}>
                  {i > 0 && ", "}
                  <Link to={match.type === "Lead" ? `/leads/${match.id}` : `/customers/${match.id}/edit`}>
                    {match.name} ({match.type})
                  </Link>
                </span>
              ))}
              . You can still save — this is just a heads-up.
            </div>
            <button
              type="button"
              className="btn-close"
              aria-label="Dismiss"
              onClick={() => setDuplicatesDismissed(true)}
            />
          </div>
        )}

        <form onSubmit={handleSubmit(onSubmit)} noValidate>
          <input type="hidden" {...register("linkedCustomerId")} />
          <input type="hidden" {...register("linkedContactId")} />

          <FormSection icon="bi-building" title="Company & Contact" description="Who this lead is and how to reach them.">
            <div className="row g-3">
              <div className="col-md-6">
                <label className="form-label">Company Name</label>
                <input
                  className={`form-control ${errors.companyName ? "is-invalid" : ""}`}
                  {...register("companyName", { required: "Company name is required.", onBlur: checkForCustomerMatches })}
                />
                {errors.companyName && <div className="invalid-feedback">{errors.companyName.message}</div>}
              </div>

              {linkedCustomer && (
                <div className="col-12">
                  <div className="alert alert-success d-flex justify-content-between align-items-center py-2 px-3 mb-0">
                    <span>
                      <i className="bi bi-link-45deg me-1" aria-hidden="true" />
                      Linked to existing customer <strong>{linkedCustomer.displayName}</strong>
                      {linkedCustomer.contactName && (
                        <>
                          {" "}
                          — contact <strong>{linkedCustomer.contactName}</strong> matched automatically
                        </>
                      )}
                      . Saving will {linkedContactIdValue ? "update that contact's details" : "add the details below as a new contact"}{" "}
                      on this customer — feel free to edit the Contact Name/Email/Mobile fields first.
                    </span>
                    <button type="button" className="btn btn-sm btn-outline-secondary" onClick={clearLinkedCustomer}>
                      Unlink
                    </button>
                  </div>
                </div>
              )}

              {!linkedCustomer && customerMatches.length > 0 && (
                <div className="col-12">
                  <div className="border rounded p-3" style={{ background: "#fbfdff" }}>
                    <div className="fw-semibold small mb-2">
                      <i className="bi bi-building-check me-1 text-muted" aria-hidden="true" />
                      Found {customerMatches.length} existing customer{customerMatches.length > 1 ? "s" : ""} matching this
                      company name — select one to link and prefill this lead, or ignore to create a new company.
                    </div>
                    {customerMatches.map((customer) => (
                      <div key={customer.id} className="d-flex justify-content-between align-items-start border-top pt-2 mt-2">
                        <div>
                          <div>
                            <strong>{customer.displayName}</strong>{" "}
                            <span className="text-muted small">({customer.customerNumber})</span>
                          </div>
                          {customer.contacts.length > 0 ? (
                            <div className="mt-1">
                              {customer.contacts.map((contact) => (
                                <div key={contact.id} className="d-flex justify-content-between align-items-center small py-1">
                                  <span>
                                    {contact.fullName || "(unnamed contact)"}
                                    {contact.isPrimary && (
                                      <span className="badge bg-secondary-subtle text-secondary ms-1">Primary</span>
                                    )}
                                    {contact.email && <span className="text-muted ms-2">{contact.email}</span>}
                                    {contact.mobile && <span className="text-muted ms-2">{contact.mobile}</span>}
                                  </span>
                                  <button
                                    type="button"
                                    className="btn btn-sm btn-outline-primary"
                                    onClick={() => applyCustomerMatch(customer, contact)}
                                  >
                                    Use this contact
                                  </button>
                                </div>
                              ))}
                            </div>
                          ) : (
                            <div className="text-muted small mt-1">No contacts on file yet.</div>
                          )}
                        </div>
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-primary ms-2 flex-shrink-0"
                          onClick={() => applyCustomerMatch(customer)}
                        >
                          Link company only
                        </button>
                      </div>
                    ))}
                    <div className="text-end mt-2">
                      <button type="button" className="btn btn-sm btn-link p-0" onClick={() => setCustomerMatches([])}>
                        Dismiss
                      </button>
                    </div>
                  </div>
                </div>
              )}

              <div className="col-md-6">
                <label className="form-label">Contact Name</label>
                <input
                  className={`form-control ${errors.contactName ? "is-invalid" : ""}`}
                  {...register("contactName", { required: "Contact name is required." })}
                />
                {errors.contactName && <div className="invalid-feedback">{errors.contactName.message}</div>}
              </div>

              <div className="col-md-6">
                <label className="form-label">Email</label>
                <input
                  type="email"
                  className={`form-control ${errors.email ? "is-invalid" : ""}`}
                  {...register("email", { onBlur: checkForDuplicates, ...emailPatternRule })}
                />
                {errors.email && <div className="invalid-feedback">{errors.email.message}</div>}
              </div>

              <div className="col-md-6">
                <label className="form-label">Mobile</label>
                <input
                  className={`form-control ${errors.mobile ? "is-invalid" : ""}`}
                  {...register("mobile", { onBlur: checkForDuplicates })}
                />
                {errors.mobile && <div className="invalid-feedback">{errors.mobile.message}</div>}
              </div>

              <div className="col-md-6">
                <label className="form-label">Industry</label>
                <input className={`form-control ${errors.industry ? "is-invalid" : ""}`} {...register("industry")} />
                {errors.industry && <div className="invalid-feedback">{errors.industry.message}</div>}
              </div>
            </div>
          </FormSection>

          <FormSection icon="bi-graph-up-arrow" title="Source & Tracking" description="Where this lead came from and any campaign attribution.">
            <div className="row g-3">
              <div className="col-md-6">
                <label className="form-label">Source</label>
                <select className={`form-select ${errors.source ? "is-invalid" : ""}`} {...register("source")}>
                  {SOURCES.map((source) => (
                    <option key={source} value={source}>
                      {source}
                    </option>
                  ))}
                </select>
                {errors.source && <div className="invalid-feedback">{errors.source.message}</div>}
              </div>

              <div className="col-md-6">
                <label className="form-label">Campaign</label>
                <input
                  className={`form-control ${errors.campaign ? "is-invalid" : ""}`}
                  placeholder="Human-readable label, e.g. Spring Promo"
                  {...register("campaign")}
                />
                {errors.campaign && <div className="invalid-feedback">{errors.campaign.message}</div>}
              </div>

              <div className="col-12">
                <label className="form-label small text-muted mb-1">UTM Tracking</label>
                <div className="row g-2">
                  <div className="col-md-4">
                    <input
                      className={`form-control form-control-sm ${errors.utmSource ? "is-invalid" : ""}`}
                      placeholder="utm_source (e.g. google)"
                      {...register("utmSource")}
                    />
                    {errors.utmSource && <div className="invalid-feedback">{errors.utmSource.message}</div>}
                  </div>
                  <div className="col-md-4">
                    <input
                      className={`form-control form-control-sm ${errors.utmMedium ? "is-invalid" : ""}`}
                      placeholder="utm_medium (e.g. cpc)"
                      {...register("utmMedium")}
                    />
                    {errors.utmMedium && <div className="invalid-feedback">{errors.utmMedium.message}</div>}
                  </div>
                  <div className="col-md-4">
                    <input
                      className={`form-control form-control-sm ${errors.utmCampaign ? "is-invalid" : ""}`}
                      placeholder="utm_campaign"
                      {...register("utmCampaign")}
                    />
                    {errors.utmCampaign && <div className="invalid-feedback">{errors.utmCampaign.message}</div>}
                  </div>
                  <div className="col-md-4">
                    <input
                      className={`form-control form-control-sm ${errors.utmTerm ? "is-invalid" : ""}`}
                      placeholder="utm_term"
                      {...register("utmTerm")}
                    />
                    {errors.utmTerm && <div className="invalid-feedback">{errors.utmTerm.message}</div>}
                  </div>
                  <div className="col-md-4">
                    <input
                      className={`form-control form-control-sm ${errors.utmContent ? "is-invalid" : ""}`}
                      placeholder="utm_content"
                      {...register("utmContent")}
                    />
                    {errors.utmContent && <div className="invalid-feedback">{errors.utmContent.message}</div>}
                  </div>
                </div>
              </div>
            </div>
          </FormSection>

          <FormSection icon="bi-cash-coin" title="Deal Details & Assignment" description="Value, timeline, territory, and who owns this lead.">
            <div className="row g-3">
              <div className="col-md-4">
                <label className="form-label">Budget</label>
                <input
                  type="number"
                  step="0.01"
                  className={`form-control ${errors.budget ? "is-invalid" : ""}`}
                  {...register("budget")}
                />
                {errors.budget && <div className="invalid-feedback">{errors.budget.message}</div>}
              </div>

              <div className="col-md-4">
                <label className="form-label">Expected Value</label>
                <input
                  type="number"
                  step="0.01"
                  className={`form-control ${errors.expectedValue ? "is-invalid" : ""}`}
                  {...register("expectedValue")}
                />
                {errors.expectedValue && <div className="invalid-feedback">{errors.expectedValue.message}</div>}
              </div>

              <div className="col-md-4">
                <label className="form-label">Timeline</label>
                <input
                  className={`form-control ${errors.timeline ? "is-invalid" : ""}`}
                  placeholder="e.g. This Quarter"
                  {...register("timeline")}
                />
                {errors.timeline && <div className="invalid-feedback">{errors.timeline.message}</div>}
              </div>

              <div className="col-md-4">
                <label className="form-label">Territory (legacy free text)</label>
                <input className={`form-control ${errors.territory ? "is-invalid" : ""}`} {...register("territory")} />
                {errors.territory && <div className="invalid-feedback">{errors.territory.message}</div>}
              </div>

              <div className="col-md-4">
                <label className="form-label">Territory (structured)</label>
                <select className={`form-select ${errors.territoryId ? "is-invalid" : ""}`} {...register("territoryId")}>
                  <option value="">None</option>
                  {territories.map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.name}
                    </option>
                  ))}
                </select>
                {errors.territoryId && <div className="invalid-feedback">{errors.territoryId.message}</div>}
              </div>

              <div className="col-md-4">
                <label className="form-label">Next Follow-Up</label>
                <input
                  type="date"
                  className={`form-control ${errors.nextFollowUpDate ? "is-invalid" : ""}`}
                  {...register("nextFollowUpDate")}
                />
                {errors.nextFollowUpDate && <div className="invalid-feedback">{errors.nextFollowUpDate.message}</div>}
              </div>

              <div className="col-md-6">
                <label className="form-label">Assign To</label>
                <select
                  className={`form-select ${errors.assignedToUserId ? "is-invalid" : ""}`}
                  {...register("assignedToUserId")}
                >
                  <option value="">Unassigned</option>
                  {users.map((user) => (
                    <option key={user.id} value={user.id}>
                      {user.fullName}
                    </option>
                  ))}
                </select>
                {errors.assignedToUserId && <div className="invalid-feedback">{errors.assignedToUserId.message}</div>}
              </div>
            </div>
          </FormSection>

          <FormSection icon="bi-journal-text" title="Notes">
            <textarea
              className={`form-control ${errors.notes ? "is-invalid" : ""}`}
              rows={3}
              placeholder="Anything else worth noting about this lead..."
              {...register("notes")}
            />
            {errors.notes && <div className="invalid-feedback">{errors.notes.message}</div>}
          </FormSection>

          <FormActionBar>
            <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
              {isSubmitting ? "Saving..." : "Save Lead"}
            </button>
            <button type="button" className="btn btn-outline-secondary" onClick={() => navigate(backTo)}>
              Cancel
            </button>
          </FormActionBar>
        </form>
      </div>
    </div>
  );
}
