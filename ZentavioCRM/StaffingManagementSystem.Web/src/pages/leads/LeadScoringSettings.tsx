import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useForm } from "react-hook-form";
import { leadScoringSettingsService, type LeadScoringSettings } from "@/services/leadScoringSettingsService";
import { PageHeader } from "@/components/layout/PageHeader";
import { FormSection } from "@/components/form/FormSection";
import { FormActionBar } from "@/components/form/FormActionBar";

export default function LeadScoringSettingsPage() {
  const navigate = useNavigate();
  const [isLoading, setIsLoading] = useState(true);
  const [serverError, setServerError] = useState<string | null>(null);
  const [serverErrors, setServerErrors] = useState<string[]>([]);
  const [savedMessage, setSavedMessage] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    formState: { isSubmitting },
  } = useForm<LeadScoringSettings>();

  useEffect(() => {
    (async () => {
      const result = await leadScoringSettingsService.get();
      if (result.success && result.data) {
        reset(result.data);
      } else {
        setServerError(result.message || "Unable to load lead scoring settings.");
      }
      setIsLoading(false);
    })();
  }, [reset]);

  const onSubmit = async (values: LeadScoringSettings) => {
    setServerError(null);
    setServerErrors([]);
    setSavedMessage(null);

    // react-hook-form registers numeric inputs as strings unless valueAsNumber is set on every
    // single field — coerce here once instead, same approach apiClient's number handling takes.
    const request: LeadScoringSettings = Object.fromEntries(
      Object.entries(values).map(([key, value]) => [key, key === "updatedAtUtc" ? value : Number(value)])
    ) as unknown as LeadScoringSettings;

    const result = await leadScoringSettingsService.update(request);
    if (!result.success || !result.data) {
      setServerError(result.message || "Unable to save lead scoring settings.");
      setServerErrors(result.errors || []);
      return;
    }

    reset(result.data);
    setSavedMessage("Lead scoring settings saved.");
  };

  if (isLoading) {
    return <div className="text-muted">Loading...</div>;
  }

  return (
    <div>
      <PageHeader
        title="Lead Scoring Settings"
        subtitle="Tune the weights behind every lead's automatic score. Changes apply the next time a lead is created or edited — existing scores aren't recalculated retroactively."
        backTo="/leads"
        backLabel="Back to Leads"
      />

      {serverError && (
        <div className="alert alert-danger">
          {serverError}
          {serverErrors.length > 0 && (
            <ul className="mb-0 mt-2">
              {serverErrors.map((e) => (
                <li key={e}>{e}</li>
              ))}
            </ul>
          )}
        </div>
      )}
      {savedMessage && <div className="alert alert-success">{savedMessage}</div>}

      <form onSubmit={handleSubmit(onSubmit)} noValidate>
        <FormSection icon="bi-person-check" title="Field Completeness" description="Points for having key contact details on file.">
          <div className="row g-3">
            <div className="col-md-3">
              <label className="form-label" htmlFor="emailPresentPoints">Email present</label>
              <input id="emailPresentPoints" type="number" className="form-control" {...register("emailPresentPoints", { required: true })} />
            </div>
            <div className="col-md-3">
              <label className="form-label" htmlFor="mobilePresentPoints">Mobile present</label>
              <input id="mobilePresentPoints" type="number" className="form-control" {...register("mobilePresentPoints", { required: true })} />
            </div>
            <div className="col-md-3">
              <label className="form-label" htmlFor="industryPresentPoints">Industry present</label>
              <input id="industryPresentPoints" type="number" className="form-control" {...register("industryPresentPoints", { required: true })} />
            </div>
            <div className="col-md-3">
              <label className="form-label" htmlFor="assignedPoints">Already assigned</label>
              <input id="assignedPoints" type="number" className="form-control" {...register("assignedPoints", { required: true })} />
            </div>
          </div>
        </FormSection>

        <FormSection icon="bi-cash-coin" title="Expected Deal Size" description="Tiered points based on the lead's Expected Value.">
          <div className="row g-3">
            <div className="col-md-4">
              <label className="form-label" htmlFor="expectedValueHighThreshold">High tier threshold</label>
              <input id="expectedValueHighThreshold" type="number" className="form-control" {...register("expectedValueHighThreshold", { required: true })} />
            </div>
            <div className="col-md-2">
              <label className="form-label" htmlFor="expectedValueHighPoints">High tier points</label>
              <input id="expectedValueHighPoints" type="number" className="form-control" {...register("expectedValueHighPoints", { required: true })} />
            </div>
            <div className="col-md-4">
              <label className="form-label" htmlFor="expectedValueMediumThreshold">Medium tier threshold</label>
              <input id="expectedValueMediumThreshold" type="number" className="form-control" {...register("expectedValueMediumThreshold", { required: true })} />
            </div>
            <div className="col-md-2">
              <label className="form-label" htmlFor="expectedValueMediumPoints">Medium tier points</label>
              <input id="expectedValueMediumPoints" type="number" className="form-control" {...register("expectedValueMediumPoints", { required: true })} />
            </div>
            <div className="col-md-3">
              <label className="form-label" htmlFor="expectedValueLowPoints">Any value above zero</label>
              <input id="expectedValueLowPoints" type="number" className="form-control" {...register("expectedValueLowPoints", { required: true })} />
            </div>
          </div>
        </FormSection>

        <FormSection icon="bi-signpost-split" title="Source &amp; Timeline" description="Points for how the lead arrived, and how soon they're looking to buy.">
          <div className="row g-3">
            <div className="col-md-4">
              <label className="form-label" htmlFor="sourceReferralPoints">Referral source</label>
              <input id="sourceReferralPoints" type="number" className="form-control" {...register("sourceReferralPoints", { required: true })} />
            </div>
            <div className="col-md-4">
              <label className="form-label" htmlFor="sourceWarmChannelPoints">LinkedIn / Website / Landing Page / Exhibition</label>
              <input id="sourceWarmChannelPoints" type="number" className="form-control" {...register("sourceWarmChannelPoints", { required: true })} />
            </div>
            <div className="col-md-4">
              <label className="form-label" htmlFor="urgentTimelinePoints">Urgent timeline (month/quarter/ASAP)</label>
              <input id="urgentTimelinePoints" type="number" className="form-control" {...register("urgentTimelinePoints", { required: true })} />
            </div>
          </div>
        </FormSection>

        <FormSection icon="bi-telephone-outbound" title="Engagement" description="New factor: points for completed activities (calls, meetings) logged against the lead.">
          <div className="row g-3">
            <div className="col-md-4">
              <label className="form-label" htmlFor="pointsPerCompletedActivity">Points per completed activity</label>
              <input id="pointsPerCompletedActivity" type="number" className="form-control" {...register("pointsPerCompletedActivity", { required: true })} />
            </div>
            <div className="col-md-4">
              <label className="form-label" htmlFor="engagementMaxPoints">Engagement cap</label>
              <input id="engagementMaxPoints" type="number" className="form-control" {...register("engagementMaxPoints", { required: true })} />
            </div>
            <div className="col-md-4">
              <label className="form-label" htmlFor="maxScore">Overall score cap</label>
              <input id="maxScore" type="number" className="form-control" {...register("maxScore", { required: true })} />
            </div>
          </div>
        </FormSection>

        <FormActionBar>
          <button type="submit" className="btn btn-primary" disabled={isSubmitting}>
            {isSubmitting ? "Saving..." : "Save"}
          </button>
          <button type="button" className="btn btn-outline-secondary" onClick={() => navigate("/leads")}>
            Cancel
          </button>
        </FormActionBar>
      </form>
    </div>
  );
}
