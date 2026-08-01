import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { leadService, type Lead, type LeadStatus } from "@/services/leadService";
import { userService, type ManagedUser } from "@/services/userService";
import { PermissionCodes } from "@/services/permissionCodes";
import { HistoryPanel } from "@/components/history/HistoryPanel";
import { ActivityTimelinePanel } from "@/components/activities/ActivityTimelinePanel";
import { PageHeader } from "@/components/layout/PageHeader";
import { FormSection } from "@/components/form/FormSection";

const NEXT_STATUSES: Record<LeadStatus, LeadStatus[]> = {
  New: ["Contacted", "Lost", "Junk"],
  Assigned: ["Contacted", "Lost", "Junk"],
  Contacted: ["Qualified", "Nurturing", "Lost", "Junk"],
  Qualified: ["ProposalSent", "Nurturing", "Lost"],
  Nurturing: ["Contacted", "Qualified", "Lost"],
  ProposalSent: ["Qualified", "Lost"],
  Converted: [],
  Lost: [],
  Junk: [],
};

const STATUS_BADGE: Record<LeadStatus, string> = {
  New: "text-bg-secondary",
  Assigned: "text-bg-info",
  Contacted: "text-bg-info",
  Qualified: "text-bg-primary",
  Nurturing: "text-bg-warning",
  ProposalSent: "text-bg-warning",
  Converted: "text-bg-success",
  Lost: "text-bg-danger",
  Junk: "text-bg-dark",
};

export default function LeadDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { hasPermission } = useAuth();

  const [lead, setLead] = useState<Lead | null>(null);
  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [assignToUserId, setAssignToUserId] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [actionError, setActionError] = useState<string | null>(null);

  const canEdit = hasPermission(PermissionCodes.LeadsEdit);
  const canAssign = hasPermission(PermissionCodes.LeadsAssign);
  const canConvert = hasPermission(PermissionCodes.LeadsConvert);
  const canDelete = hasPermission(PermissionCodes.LeadsDelete);
  const [isDeleting, setIsDeleting] = useState(false);

  const load = async () => {
    if (!id) return;
    setIsLoading(true);
    const [leadResult, usersResult] = await Promise.all([leadService.getById(id), userService.getAll()]);
    setIsLoading(false);

    if (!leadResult.success || !leadResult.data) {
      setError(leadResult.message || "Unable to load lead.");
      return;
    }
    setLead(leadResult.data);
    setAssignToUserId(leadResult.data.assignedToUserId ?? "");

    if (usersResult.success && usersResult.data) setUsers(usersResult.data);
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const handleAssign = async () => {
    if (!id || !assignToUserId) return;
    setActionError(null);
    const result = await leadService.assign(id, assignToUserId);
    if (!result.success) {
      setActionError(result.message || "Unable to assign lead.");
      return;
    }
    load();
  };

  const handleStatusChange = async (status: LeadStatus) => {
    if (!id) return;
    setActionError(null);

    let reason: string | undefined;
    if (status === "Lost" || status === "Junk") {
      reason = window.prompt(`Reason for marking this lead as ${status}:`) ?? undefined;
      if (!reason) return;
    }

    const result = await leadService.updateStatus(id, status, reason);
    if (!result.success) {
      setActionError(result.message || "Unable to update lead status.");
      return;
    }
    load();
  };

  const handleConvert = async () => {
    if (!id) return;
    if (!window.confirm("Convert this lead to a customer? This cannot be undone.")) return;

    setActionError(null);
    const result = await leadService.convert(id);
    if (!result.success || !result.data) {
      setActionError(result.message || "Unable to convert lead.");
      return;
    }
    navigate(`/customers/${result.data.customerId}/edit`);
  };

  const handleDelete = async () => {
    if (!id) return;
    if (!window.confirm(`Delete lead ${lead?.leadNumber ?? ""}? This cannot be undone.`)) return;

    setActionError(null);
    setIsDeleting(true);
    const result = await leadService.remove(id);
    setIsDeleting(false);

    if (!result.success) {
      setActionError(result.message || "Unable to delete lead.");
      return;
    }
    navigate("/leads", { replace: true });
  };

  const handleConvertToOpportunity = async () => {
    if (!id) return;
    if (!window.confirm("Convert this lead to an opportunity? This cannot be undone.")) return;

    setActionError(null);
    const result = await leadService.convertToOpportunity(id);
    if (!result.success || !result.data) {
      setActionError(result.message || "Unable to convert lead to an opportunity.");
      return;
    }
    navigate(`/opportunities/${result.data.opportunityId}`);
  };

  if (isLoading) {
    return <div className="text-muted">Loading...</div>;
  }

  if (error || !lead) {
    return <div className="alert alert-danger">{error || "Lead not found."}</div>;
  }

  const nextStatuses = NEXT_STATUSES[lead.status];
  const isOpenLead = lead.status !== "Converted" && lead.status !== "Lost" && lead.status !== "Junk";
  const isFollowUpOverdue =
    isOpenLead && lead.nextFollowUpDate != null && new Date(lead.nextFollowUpDate) < new Date();

  return (
    <div>
      <PageHeader
        title={lead.companyName}
        subtitle={lead.leadNumber}
        badge={<span className={`badge fs-6 ${STATUS_BADGE[lead.status]}`}>{lead.status}</span>}
        backTo="/leads"
        backLabel="Back to Leads"
        actions={
          <>
            {canEdit && lead.status !== "Converted" && (
              <Link to={`/leads/${lead.id}/edit`} className="btn btn-outline-secondary">
                <i className="bi bi-pencil me-1" aria-hidden="true" />
                Edit
              </Link>
            )}
            {canConvert && lead.status !== "Converted" && lead.status !== "Lost" && lead.status !== "Junk" && (
              <>
                <button type="button" className="btn btn-outline-success" onClick={handleConvert}>
                  <i className="bi bi-arrow-right-circle me-1" aria-hidden="true" />
                  Convert to Customer
                </button>
                <button type="button" className="btn btn-success" onClick={handleConvertToOpportunity}>
                  <i className="bi bi-graph-up-arrow me-1" aria-hidden="true" />
                  Convert to Opportunity
                </button>
              </>
            )}
            {canDelete && (
              <button type="button" className="btn btn-outline-danger" disabled={isDeleting} onClick={handleDelete}>
                <i className="bi bi-trash me-1" aria-hidden="true" />
                {isDeleting ? "Deleting..." : "Delete"}
              </button>
            )}
          </>
        }
      />

      {actionError && <div className="alert alert-danger">{actionError}</div>}

      {lead.status === "Converted" && (
        <div className="alert alert-success">
          This lead was converted to a customer.{" "}
          {lead.convertedCustomerId && (
            <Link to={`/customers/${lead.convertedCustomerId}/edit`}>View customer</Link>
          )}
        </div>
      )}

      {isOpenLead && lead.nextFollowUpDate && (
        <div className={`alert d-flex justify-content-between align-items-center ${isFollowUpOverdue ? "alert-warning" : "alert-info"}`}>
          <div>
            <strong>{isFollowUpOverdue ? "Follow-up overdue:" : "Next Follow-Up:"}</strong>{" "}
            {new Date(lead.nextFollowUpDate).toLocaleDateString()}
          </div>
        </div>
      )}

      <div className="row g-4">
        <div className="col-lg-8">
          <FormSection icon="bi-info-circle" title="Lead Details" description="Contact info, source attribution, and deal specifics.">
            <div className="row g-3">
              <div className="col-md-6">
                <div className="text-muted small">Contact Name</div>
                <div>{lead.contactName}</div>
              </div>
              <div className="col-md-6">
                <div className="text-muted small">Email</div>
                <div>{lead.email ?? "—"}</div>
              </div>
              <div className="col-md-6">
                <div className="text-muted small">Mobile</div>
                <div>{lead.mobile ?? "—"}</div>
              </div>
              <div className="col-md-6">
                <div className="text-muted small">Industry</div>
                <div>{lead.industry ?? "—"}</div>
              </div>
              <div className="col-md-6">
                <div className="text-muted small">Source</div>
                <div>{lead.source}</div>
              </div>
              <div className="col-md-6">
                <div className="text-muted small">Campaign</div>
                <div>{lead.campaign ?? "—"}</div>
              </div>
              {(lead.utmSource || lead.utmMedium || lead.utmCampaign || lead.utmTerm || lead.utmContent) && (
                <div className="col-12">
                  <div className="text-muted small mb-1">UTM Tracking</div>
                  <div className="d-flex flex-wrap gap-1">
                    {lead.utmSource && <span className="badge text-bg-light border">source: {lead.utmSource}</span>}
                    {lead.utmMedium && <span className="badge text-bg-light border">medium: {lead.utmMedium}</span>}
                    {lead.utmCampaign && <span className="badge text-bg-light border">campaign: {lead.utmCampaign}</span>}
                    {lead.utmTerm && <span className="badge text-bg-light border">term: {lead.utmTerm}</span>}
                    {lead.utmContent && <span className="badge text-bg-light border">content: {lead.utmContent}</span>}
                  </div>
                </div>
              )}
              <div className="col-md-6">
                <div className="text-muted small">Budget</div>
                <div>{lead.budget != null ? lead.budget.toLocaleString() : "—"}</div>
              </div>
              <div className="col-md-6">
                <div className="text-muted small">Expected Value</div>
                <div>{lead.expectedValue != null ? lead.expectedValue.toLocaleString() : "—"}</div>
              </div>
              <div className="col-md-6">
                <div className="text-muted small">Timeline</div>
                <div>{lead.timeline ?? "—"}</div>
              </div>
              <div className="col-md-6">
                <div className="text-muted small">Territory</div>
                <div>{lead.territory ?? "—"}</div>
              </div>
              <div className="col-md-6">
                <div className="text-muted small">Next Follow-Up</div>
                <div>{lead.nextFollowUpDate ? new Date(lead.nextFollowUpDate).toLocaleDateString() : "—"}</div>
              </div>
              <div className="col-md-6">
                <div className="text-muted small">Lead Score</div>
                <div>
                  {lead.leadScore != null ? (
                    <span className={`badge ${lead.leadScore >= 60 ? "text-bg-success" : lead.leadScore >= 30 ? "text-bg-warning" : "text-bg-secondary"}`}>
                      {lead.leadScore}/100
                    </span>
                  ) : (
                    "—"
                  )}
                </div>
              </div>
              {lead.notes && (
                <div className="col-12">
                  <div className="text-muted small">Notes</div>
                  <div>{lead.notes}</div>
                </div>
              )}
              {lead.lostReason && (
                <div className="col-12">
                  <div className="text-muted small">Reason</div>
                  <div>{lead.lostReason}</div>
                </div>
              )}
            </div>
          </FormSection>

          <ActivityTimelinePanel relatedToType="Lead" relatedToId={lead.id} users={users} />

          <div className="mt-4">
            <HistoryPanel entityType="Lead" entityId={lead.id} />
          </div>
        </div>

        <div className="col-lg-4">
          <FormSection icon="bi-flag" title="Status">
            <div className="mb-3">
              <span className={`badge fs-6 ${STATUS_BADGE[lead.status]}`}>{lead.status}</span>
            </div>
            {nextStatuses.length > 0 ? (
              <div className="d-flex flex-wrap gap-2">
                {nextStatuses.map((status) => (
                  <button
                    key={status}
                    type="button"
                    className="btn btn-sm btn-outline-secondary"
                    onClick={() => handleStatusChange(status)}
                  >
                    Move to {status}
                  </button>
                ))}
              </div>
            ) : (
              <div className="text-muted small">No further status changes available.</div>
            )}
          </FormSection>

          {canAssign && lead.status !== "Converted" && (
            <FormSection icon="bi-person-check" title="Assignment">
              <select
                className="form-select mb-2"
                value={assignToUserId}
                onChange={(e) => setAssignToUserId(e.target.value)}
              >
                <option value="">Select a user</option>
                {users.map((user) => (
                  <option key={user.id} value={user.id}>
                    {user.fullName}
                  </option>
                ))}
              </select>
              <button
                type="button"
                className="btn btn-primary w-100"
                disabled={!assignToUserId}
                onClick={handleAssign}
              >
                Assign
              </button>
            </FormSection>
          )}

          <FormSection icon="bi-clock-history" title="Meta" className="mb-0">
            <div className="small text-muted">
              <div>Created: {new Date(lead.createdAtUtc).toLocaleString()}</div>
              {lead.updatedAtUtc && <div>Updated: {new Date(lead.updatedAtUtc).toLocaleString()}</div>}
            </div>
          </FormSection>
        </div>
      </div>
    </div>
  );
}
