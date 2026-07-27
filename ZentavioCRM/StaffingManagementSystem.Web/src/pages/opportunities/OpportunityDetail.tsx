import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { opportunityService, type Opportunity, type OpportunityStage } from "@/services/opportunityService";
import { userService, type ManagedUser } from "@/services/userService";
import { activityService, type Activity, type ActivityType } from "@/services/activityService";
import { PermissionCodes } from "@/services/permissionCodes";
import { HistoryPanel } from "@/components/history/HistoryPanel";

const NEXT_STAGES: Record<OpportunityStage, OpportunityStage[]> = {
  Qualification: ["Discovery", "ClosedWon", "ClosedLost"],
  Discovery: ["Proposal", "ClosedWon", "ClosedLost"],
  Proposal: ["Negotiation", "ClosedWon", "ClosedLost"],
  Negotiation: ["VerbalCommit", "ClosedWon", "ClosedLost"],
  VerbalCommit: ["ClosedWon", "ClosedLost"],
  ClosedWon: [],
  ClosedLost: [],
};

const ACTIVITY_TYPES: ActivityType[] = ["Call", "Email", "Meeting", "Task", "Note", "Visit", "WhatsApp", "Sms"];

export default function OpportunityDetail() {
  const { id } = useParams<{ id: string }>();
  const { hasPermission } = useAuth();

  const [opportunity, setOpportunity] = useState<Opportunity | null>(null);
  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [timeline, setTimeline] = useState<Activity[]>([]);
  const [assignToUserId, setAssignToUserId] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [actionError, setActionError] = useState<string | null>(null);

  const [newActivityType, setNewActivityType] = useState<ActivityType>("Note");
  const [newActivitySubject, setNewActivitySubject] = useState("");

  const canEdit = hasPermission(PermissionCodes.OpportunitiesEdit);
  const canAssign = hasPermission(PermissionCodes.OpportunitiesAssign);

  const load = async () => {
    if (!id) return;
    setIsLoading(true);
    const [opportunityResult, usersResult, timelineResult] = await Promise.all([
      opportunityService.getById(id),
      userService.getAll(),
      activityService.getTimeline("Opportunity", id),
    ]);
    setIsLoading(false);

    if (!opportunityResult.success || !opportunityResult.data) {
      setError(opportunityResult.message || "Unable to load opportunity.");
      return;
    }
    setOpportunity(opportunityResult.data);
    setAssignToUserId(opportunityResult.data.assignedToUserId ?? "");

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
    const result = await opportunityService.assign(id, assignToUserId);
    if (!result.success) {
      setActionError(result.message || "Unable to assign opportunity.");
      return;
    }
    load();
  };

  const handleStageChange = async (stage: OpportunityStage) => {
    if (!id) return;
    setActionError(null);

    let reason: string | undefined;
    if (stage === "ClosedLost") {
      reason = window.prompt("Reason for marking this opportunity as Closed Lost:") ?? undefined;
      if (!reason) return;
    }

    const result = await opportunityService.updateStage(id, stage, reason);
    if (!result.success) {
      setActionError(result.message || "Unable to update opportunity stage.");
      return;
    }
    load();
  };

  const handleAddActivity = async () => {
    if (!id || !newActivitySubject.trim()) return;
    const result = await activityService.create("Opportunity", id, {
      type: newActivityType,
      subject: newActivitySubject.trim(),
      description: null,
      dueAtUtc: null,
      assignedToUserId: null,
    });
    if (result.success) {
      setNewActivitySubject("");
      const timelineResult = await activityService.getTimeline("Opportunity", id);
      if (timelineResult.success && timelineResult.data) setTimeline(timelineResult.data);
    }
  };

  if (isLoading) {
    return <div className="text-muted">Loading...</div>;
  }

  if (error || !opportunity) {
    return <div className="alert alert-danger">{error || "Opportunity not found."}</div>;
  }

  const isClosed = opportunity.stage === "ClosedWon" || opportunity.stage === "ClosedLost";
  const nextStages = NEXT_STAGES[opportunity.stage];

  return (
    <div>
      <div className="d-flex justify-content-between align-items-start mb-4">
        <div>
          <div className="text-muted small">{opportunity.opportunityNumber}</div>
          <h1 className="h4 mb-0">{opportunity.name}</h1>
        </div>
        <div className="d-flex gap-2">
          {canEdit && !isClosed && (
            <Link to={`/opportunities/${opportunity.id}/edit`} className="btn btn-outline-secondary">
              Edit
            </Link>
          )}
        </div>
      </div>

      {actionError && <div className="alert alert-danger">{actionError}</div>}

      {opportunity.stage === "ClosedWon" && <div className="alert alert-success">This opportunity was won.</div>}
      {opportunity.stage === "ClosedLost" && (
        <div className="alert alert-danger">
          This opportunity was lost{opportunity.lostReason ? `: ${opportunity.lostReason}` : "."}
        </div>
      )}

      {opportunity.nextStep && !isClosed && (
        <div className="alert alert-info d-flex justify-content-between align-items-center">
          <div>
            <strong>Next Step:</strong> {opportunity.nextStep}
          </div>
          {opportunity.nextStepDate && (
            <span className="text-nowrap ms-3">Due {new Date(opportunity.nextStepDate).toLocaleDateString()}</span>
          )}
        </div>
      )}

      <div className="row g-4">
        <div className="col-lg-8">
          <div className="card shadow-sm border-0 mb-4">
            <div className="card-header bg-white fw-semibold">Opportunity Details</div>
            <div className="card-body row g-3">
              <div className="col-md-6">
                <div className="text-muted small">Customer</div>
                <div>
                  <Link to={`/customers/${opportunity.customerId}/edit`}>{opportunity.customerName}</Link>
                </div>
              </div>
              <div className="col-md-6">
                <div className="text-muted small">Value</div>
                <div>{opportunity.value != null ? opportunity.value.toLocaleString() : "—"}</div>
              </div>
              <div className="col-md-6">
                <div className="text-muted small">Probability</div>
                <div>{opportunity.probability != null ? `${opportunity.probability}%` : "—"}</div>
              </div>
              <div className="col-md-6">
                <div className="text-muted small">Expected Close Date</div>
                <div>{opportunity.expectedCloseDate ? new Date(opportunity.expectedCloseDate).toLocaleDateString() : "—"}</div>
              </div>
              <div className="col-md-6">
                <div className="text-muted small">Products</div>
                <div>{opportunity.products ?? "—"}</div>
              </div>
              <div className="col-md-6">
                <div className="text-muted small">Competitors</div>
                <div>{opportunity.competitors ?? "—"}</div>
              </div>
              {opportunity.notes && (
                <div className="col-12">
                  <div className="text-muted small">Notes</div>
                  <div>{opportunity.notes}</div>
                </div>
              )}
            </div>
          </div>

          {opportunity.lineItems.length > 0 && (
            <div className="card shadow-sm border-0 mb-4">
              <div className="card-header bg-white fw-semibold">Line Items</div>
              <div className="table-responsive">
                <table className="table mb-0">
                  <thead className="table-light">
                    <tr>
                      <th>Product</th>
                      <th className="text-end">Qty</th>
                      <th className="text-end">Unit Price</th>
                      <th className="text-end">Discount</th>
                      <th className="text-end">Total</th>
                    </tr>
                  </thead>
                  <tbody>
                    {opportunity.lineItems.map((li) => (
                      <tr key={li.id}>
                        <td>{li.productName}</td>
                        <td className="text-end">{li.quantity}</td>
                        <td className="text-end">{li.unitPrice.toLocaleString()}</td>
                        <td className="text-end">{li.discountPercent ? `${li.discountPercent}%` : "—"}</td>
                        <td className="text-end fw-semibold">{li.lineTotal.toLocaleString()}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

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

          <HistoryPanel entityType="Opportunity" entityId={opportunity.id} />
        </div>

        <div className="col-lg-4">
          <div className="card shadow-sm border-0 mb-4">
            <div className="card-header bg-white fw-semibold">Stage</div>
            <div className="card-body">
              <div className="mb-3">
                <span className="badge text-bg-primary fs-6">{opportunity.stage}</span>
              </div>
              {nextStages.length > 0 ? (
                <div className="d-flex flex-wrap gap-2">
                  {nextStages.map((stage) => (
                    <button
                      key={stage}
                      type="button"
                      className="btn btn-sm btn-outline-secondary"
                      onClick={() => handleStageChange(stage)}
                    >
                      Move to {stage}
                    </button>
                  ))}
                </div>
              ) : (
                <div className="text-muted small">No further stage changes available.</div>
              )}
            </div>
          </div>

          {canAssign && !isClosed && (
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
              <div>Created: {new Date(opportunity.createdAtUtc).toLocaleString()}</div>
              {opportunity.updatedAtUtc && <div>Updated: {new Date(opportunity.updatedAtUtc).toLocaleString()}</div>}
              {opportunity.closedAtUtc && <div>Closed: {new Date(opportunity.closedAtUtc).toLocaleString()}</div>}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
