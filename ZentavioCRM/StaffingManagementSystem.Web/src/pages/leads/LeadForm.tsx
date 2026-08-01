import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { Link, useNavigate, useParams } from "react-router-dom";
import { leadService, type DuplicateMatch, type LeadSource, type SaveLeadRequest } from "@/services/leadService";
import { userService, type ManagedUser } from "@/services/userService";
import { territoryService, type Territory } from "@/services/territoryService";
import { emailPatternRule } from "@/utils/validation";
import { PageHeader } from "@/components/layout/PageHeader";
import { FormSection } from "@/components/form/FormSection";
import { FormActionBar } from "@/components/form/FormActionBar";

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

  const {
    register,
    handleSubmit,
    reset,
    getValues,
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
    },
  });

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
          });
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

  const onSubmit = async (values: SaveLeadRequest) => {
    setServerError(null);

    const result = isEditMode && id ? await leadService.update(id, values) : await leadService.create(values);

    if (!result.success) {
      setServerError(result.message || "Unable to save lead.");
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
          <FormSection icon="bi-building" title="Company & Contact" description="Who this lead is and how to reach them.">
            <div className="row g-3">
              <div className="col-md-6">
                <label className="form-label">Company Name</label>
                <input
                  className={`form-control ${errors.companyName ? "is-invalid" : ""}`}
                  {...register("companyName", { required: "Company name is required." })}
                />
                {errors.companyName && <div className="invalid-feedback">{errors.companyName.message}</div>}
              </div>

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
                <input className="form-control" {...register("mobile", { onBlur: checkForDuplicates })} />
              </div>

              <div className="col-md-6">
                <label className="form-label">Industry</label>
                <input className="form-control" {...register("industry")} />
              </div>
            </div>
          </FormSection>

          <FormSection icon="bi-graph-up-arrow" title="Source & Tracking" description="Where this lead came from and any campaign attribution.">
            <div className="row g-3">
              <div className="col-md-6">
                <label className="form-label">Source</label>
                <select className="form-select" {...register("source")}>
                  {SOURCES.map((source) => (
                    <option key={source} value={source}>
                      {source}
                    </option>
                  ))}
                </select>
              </div>

              <div className="col-md-6">
                <label className="form-label">Campaign</label>
                <input className="form-control" placeholder="Human-readable label, e.g. Spring Promo" {...register("campaign")} />
              </div>

              <div className="col-12">
                <label className="form-label small text-muted mb-1">UTM Tracking</label>
                <div className="row g-2">
                  <div className="col-md-4">
                    <input className="form-control form-control-sm" placeholder="utm_source (e.g. google)" {...register("utmSource")} />
                  </div>
                  <div className="col-md-4">
                    <input className="form-control form-control-sm" placeholder="utm_medium (e.g. cpc)" {...register("utmMedium")} />
                  </div>
                  <div className="col-md-4">
                    <input className="form-control form-control-sm" placeholder="utm_campaign" {...register("utmCampaign")} />
                  </div>
                  <div className="col-md-4">
                    <input className="form-control form-control-sm" placeholder="utm_term" {...register("utmTerm")} />
                  </div>
                  <div className="col-md-4">
                    <input className="form-control form-control-sm" placeholder="utm_content" {...register("utmContent")} />
                  </div>
                </div>
              </div>
            </div>
          </FormSection>

          <FormSection icon="bi-cash-coin" title="Deal Details & Assignment" description="Value, timeline, territory, and who owns this lead.">
            <div className="row g-3">
              <div className="col-md-4">
                <label className="form-label">Budget</label>
                <input type="number" step="0.01" className="form-control" {...register("budget")} />
              </div>

              <div className="col-md-4">
                <label className="form-label">Expected Value</label>
                <input type="number" step="0.01" className="form-control" {...register("expectedValue")} />
              </div>

              <div className="col-md-4">
                <label className="form-label">Timeline</label>
                <input className="form-control" placeholder="e.g. This Quarter" {...register("timeline")} />
              </div>

              <div className="col-md-4">
                <label className="form-label">Territory (legacy free text)</label>
                <input className="form-control" {...register("territory")} />
              </div>

              <div className="col-md-4">
                <label className="form-label">Territory (structured)</label>
                <select className="form-select" {...register("territoryId")}>
                  <option value="">None</option>
                  {territories.map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.name}
                    </option>
                  ))}
                </select>
              </div>

              <div className="col-md-4">
                <label className="form-label">Next Follow-Up</label>
                <input type="date" className="form-control" {...register("nextFollowUpDate")} />
              </div>

              <div className="col-md-6">
                <label className="form-label">Assign To</label>
                <select className="form-select" {...register("assignedToUserId")}>
                  <option value="">Unassigned</option>
                  {users.map((user) => (
                    <option key={user.id} value={user.id}>
                      {user.fullName}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          </FormSection>

          <FormSection icon="bi-journal-text" title="Notes">
            <textarea className="form-control" rows={3} placeholder="Anything else worth noting about this lead..." {...register("notes")} />
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
