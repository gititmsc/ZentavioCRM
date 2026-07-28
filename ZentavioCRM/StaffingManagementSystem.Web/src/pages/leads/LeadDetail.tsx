import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { leadService, type Lead, type LeadStatus } from "@/services/leadService";
import { userService, type ManagedUser } from "@/services/userService";
import { activityService, type Activity, type ActivityType } from "@/services/activityService";
import { PermissionCodes } from "@/services/permissionCodes";
import { HistoryPanel } from "@/components/history/HistoryPanel";

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

const ACTIVITY_TYPES: ActivityType[] = ["Call", "Email", "Meeting", "Task", "Note", "Visit", "WhatsApp", "Sms"];

export default function LeadDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { hasPermission } = useAuth();

  const [lead, setLead] = useState<Lead | null>(null);
  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [timeline, setTimeline] = useState<Activity[]>([]);
  const [assignToUserId, setAssignToUserId] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [actionError, setActionError] = useState<string | null>(null);

  const [newActivityType, setNewActivityType] = useState<ActivityType>("Note");
  const [newActivitySubject, setNewActivitySubject] = useState("");

  const canEdit = hasPermission(PermissionCodes.LeadsEdit);
  const canAssign = hasPermission(PermissionCodes.LeadsAssign);
  const canConvert = hasPermission(PermissionCodes.LeadsConvert);

  const load = async () => {
    if (!id) return;
    setIsLoading(true);
    const [leadResult, usersResult, timelineResult] = await Promise.all([
      leadService.getById(id),
      userService.getAll(),
      activityService.getTimeline("Lead", id),
    ]);
    setIsLoading(false);

    if (!leadResult.success || !leadResult.data) {
      setError(leadResult.message || "Unable to load lead.");
      return;
    }
    setLead(leadResult.data);
    setAssignToUserId(leadResult.data.assignedToUserId ?? "");

    if (usersResult.success && usersResult.data) setUsers(usersResult.data);
    if (timelineResult.success && timelineResult.data) setTimeline(timelineResult.data);
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

  const handleAddActivity = async () => {
    if (!id || !newActivitySubject.trim()) return;
    const result = await activityService.create("Lead", id, {
      type: newActivityType,
      subject: newActivitySubject.trim(),
      description: null,
      dueAtUtc: null,
      assignedToUserId: null,
    });
    if (result.success) {
      setNewActivitySubject("");
      const timelineResult = await activityService.getTimeline("Lead", id);
      if (timelineResult.success && timelineResult.data) setTimeline(timelineResult.data);
    }
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
      <div className="d-flex justify-content-between align-items-start mb-4">
        <div>
          <div className="text-muted small">{lead.leadNumber}</div>
          <h1 className="h4 mb-0">{lead.companyName}</h1>
        </div>
        <div className="d-flex gap-2">
          {canEdit && lead.status !== "Converted" && (
            <Link to={`/leads/${lead.id}/edit`} className="btn btn-outline-secondary">
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
        </div>
      </div>

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
          <div className="card shadow-sm border-0 mb-4">
            <div className="card-header bg-white fw-semibold">Lead Details</div>
            <div className="card-body row g-3">
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
          </div>

          <div className="card shadow-sm border-0">
            <div className="card-header bg-white fw-semibold">Timeline</div>
            <div className="card-body">
              <div className="d-flex gap-2 mb-3">
                <select
                  className="form-select"
                  style={{ maxWidth: 140 }}
                  value={newActivityType}
                  onChange={(e) => setNewActivityType(e.target.value as ActivityType)}
                >
                  {ACTIVITY_TYPES.map((type) => (
                    <option key={type} value={type}>
                      {type}
                    </option>
                  ))}
                </select>
                <input
                  className="form-control"
                  placeholder="Log a call, note, or task..."
                  value={newActivitySubject}
                  onChange={(e) => setNewActivitySubject(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === "Enter") {
                      e.preventDefault();
                      handleAddActivity();
                    }
                  }}
                />
                <button type="button" className="btn btn-outline-primary" onClick={handleAddActivity}>
                  Log
                </button>
              </div>

              {timeline.length === 0 && <div className="text-muted">No activity logged yet.</div>}

              <ul className="list-unstyled mb-0">
                {timeline.map((activity) => (
                  <li key={activity.id} className="border-bottom py-2">
                    <div className="d-flex justify-content-between">
                      <span className="fw-semibold">
                        <i className="bi bi-clock-history me-2 text-muted" aria-hidden="true" />
                        {activity.type}: {activity.subject}
                      </span>
                      <span className="text-muted small">{new Date(activity.createdAtUtc).toLocaleString()}</span>
                    </div>
                    {activity.description && <div className="text-muted small ms-4">{activity.description}</div>}
                  </li>
                ))}
              </ul>
            </div>
          </div>

          <HistoryPanel entityType="Lead" entityId={lead.id} />
        </div>

        <div className="col-lg-4">
          <div className="card shadow-sm border-0 mb-4">
            <div className="card-header bg-white fw-semibold">Status</div>
            <div className="card-body">
              <div className="mb-3">
                <span className="badge text-bg-primary fs-6">{lead.status}</span>
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
            </div>
          </div>

          {canAssign && lead.status !== "Converted" && (
            <div className="card shadow-sm border-0 mb-4">
              <div className="card-header bg-white fw-semibold">Assignment</div>
              <div className="card-body">
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
              </div>
            </div>
          )}

          <div className="card shadow-sm border-0">
            <div className="card-header bg-white fw-semibold">Meta</div>
            <div className="card-body small text-muted">
              <div>Created: {new Date(lead.createdAtUtc).toLocaleString()}</div>
              {lead.updatedAtUtc && <div>Updated: {new Date(lead.updatedAtUtc).toLocaleString()}</div>}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
