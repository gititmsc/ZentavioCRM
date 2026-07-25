import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { useNavigate, useParams } from "react-router-dom";
import { leadService, type LeadSource, type SaveLeadRequest } from "@/services/leadService";
import { userService, type ManagedUser } from "@/services/userService";

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
  const [serverError, setServerError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(isEditMode);

  const {
    register,
    handleSubmit,
    reset,
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
      budget: null,
      timeline: null,
      expectedValue: null,
      assignedToUserId: null,
      territory: null,
      notes: null,
    },
  });

  useEffect(() => {
    (async () => {
      const usersResult = await userService.getAll();
      if (usersResult.success && usersResult.data) setUsers(usersResult.data);

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
            budget: l.budget,
            timeline: l.timeline,
            expectedValue: l.expectedValue,
            assignedToUserId: l.assignedToUserId,
            territory: l.territory,
            notes: l.notes,
          });
        }
      }

      setIsLoading(false);
    })();
  }, [id, isEditMode, reset]);

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

  return (
    <div>
      <h1 className="h4 mb-4">{isEditMode ? "Edit Lead" : "New Lead"}</h1>

      <div className="card shadow-sm border-0" style={{ maxWidth: 780 }}>
        <div className="card-body">
          {serverError && <div className="alert alert-danger">{serverError}</div>}

          <form onSubmit={handleSubmit(onSubmit)} noValidate>
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
                <input type="email" className="form-control" {...register("email")} />
              </div>

              <div className="col-md-6">
                <label className="form-label">Mobile</label>
                <input className="form-control" {...register("mobile")} />
              </div>

              <div className="col-md-4">
                <label className="form-label">Industry</label>
                <input className="form-control" {...register("industry")} />
              </div>

              <div className="col-md-4">
                <label className="form-label">Source</label>
                <select className="form-select" {...register("source")}>
                  {SOURCES.map((source) => (
                    <option key={source} value={source}>
                      {source}
                    </option>
                  ))}
                </select>
              </div>

              <div className="col-md-4">
                <label className="form-label">Campaign</label>
                <input className="form-control" {...register("campaign")} />
              </div>

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

              <div className="col-md-6">
                <label className="form-label">Territory</label>
                <input className="form-control" {...register("territory")} />
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
                onClick={() => navigate(isEditMode && id ? `/leads/${id}` : "/leads")}
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
