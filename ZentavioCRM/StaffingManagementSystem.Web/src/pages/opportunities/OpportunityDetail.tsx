import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import {
  opportunityService,
  type Opportunity,
  type OpportunityContactRole,
  type OpportunityStage,
} from "@/services/opportunityService";
import { userService, type ManagedUser } from "@/services/userService";
import { quotationService, type QuotationListItem } from "@/services/quotationService";
import { PermissionCodes } from "@/services/permissionCodes";
import { HistoryPanel } from "@/components/history/HistoryPanel";
import { DocumentsPanel } from "@/components/documents/DocumentsPanel";
import { ActivityTimelinePanel } from "@/components/activities/ActivityTimelinePanel";

const QUOTATION_STATUS_BADGE: Record<string, string> = {
  Draft: "text-bg-secondary",
  Sent: "text-bg-info",
  Accepted: "text-bg-success",
  Rejected: "text-bg-danger",
  Expired: "text-bg-dark",
};

const NEXT_STAGES: Record<OpportunityStage, OpportunityStage[]> = {
  Qualification: ["Discovery", "ClosedWon", "ClosedLost"],
  Discovery: ["Proposal", "ClosedWon", "ClosedLost"],
  Proposal: ["Negotiation", "ClosedWon", "ClosedLost"],
  Negotiation: ["VerbalCommit", "ClosedWon", "ClosedLost"],
  VerbalCommit: ["ClosedWon", "ClosedLost"],
  ClosedWon: [],
  ClosedLost: [],
};

const CONTACT_ROLE_LABEL: Record<OpportunityContactRole, string> = {
  Champion: "Champion",
  EconomicBuyer: "Economic Buyer",
  Blocker: "Blocker",
  Influencer: "Influencer",
  DecisionMaker: "Decision Maker",
  TechnicalEvaluator: "Technical Evaluator",
  Other: "Other",
};

const CONTACT_ROLE_BADGE: Record<OpportunityContactRole, string> = {
  Champion: "text-bg-success",
  EconomicBuyer: "text-bg-primary",
  Blocker: "text-bg-danger",
  Influencer: "text-bg-info",
  DecisionMaker: "text-bg-dark",
  TechnicalEvaluator: "text-bg-secondary",
  Other: "text-bg-light border",
};

export default function OpportunityDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { hasPermission } = useAuth();

  const [opportunity, setOpportunity] = useState<Opportunity | null>(null);
  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [quotations, setQuotations] = useState<QuotationListItem[]>([]);
  const [assignToUserId, setAssignToUserId] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [actionError, setActionError] = useState<string | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const canEdit = hasPermission(PermissionCodes.OpportunitiesEdit);
  const canAssign = hasPermission(PermissionCodes.OpportunitiesAssign);
  const canCreateQuotation = hasPermission(PermissionCodes.QuotationsCreate);
  const canDelete = hasPermission(PermissionCodes.OpportunitiesDelete);

  const load = async () => {
    if (!id) return;
    setIsLoading(true);
    const [opportunityResult, usersResult, quotationsResult] = await Promise.all([
      opportunityService.getById(id),
      userService.getAll(),
      quotationService.search({ opportunityId: id, pageSize: 50 }),
    ]);
    setIsLoading(false);

    if (!opportunityResult.success || !opportunityResult.data) {
      setError(opportunityResult.message || "Unable to load opportunity.");
      return;
    }
    setOpportunity(opportunityResult.data);
    setAssignToUserId(opportunityResult.data.assignedToUserId ?? "");

    if (usersResult.success && usersResult.data) setUsers(usersResult.data);
    if (quotationsResult.success && quotationsResult.data) setQuotations(quotationsResult.data.items);
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

  const handleDelete = async () => {
    if (!id) return;
    if (!window.confirm(`Delete opportunity ${opportunity?.opportunityNumber ?? ""}? This cannot be undone.`)) return;

    setActionError(null);
    setIsDeleting(true);
    const result = await opportunityService.remove(id);
    setIsDeleting(false);

    if (!result.success) {
      setActionError(result.message || "Unable to delete opportunity.");
      return;
    }
    navigate("/opportunities", { replace: true });
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
          {canDelete && (
            <button type="button" className="btn btn-outline-danger" disabled={isDeleting} onClick={handleDelete}>
              <i className="bi bi-trash me-1" aria-hidden="true" />
              {isDeleting ? "Deleting..." : "Delete"}
            </button>
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
                <div>
                  {opportunity.value != null
                    ? `${opportunity.currencyCode} ${opportunity.value.toLocaleString()}`
                    : "—"}
                </div>
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

          {opportunity.contacts.length > 0 && (
            <div className="card shadow-sm border-0 mb-4">
              <div className="card-header bg-white fw-semibold">Buying Committee</div>
              <div className="table-responsive">
                <table className="table mb-0 align-middle">
                  <thead className="table-light">
                    <tr>
                      <th>Contact</th>
                      <th>Role</th>
                      <th>Notes</th>
                    </tr>
                  </thead>
                  <tbody>
                    {opportunity.contacts.map((c) => (
                      <tr key={c.id}>
                        <td>
                          <Link to={`/customers/${opportunity.customerId}/edit`}>{c.contactPersonName}</Link>
                          {c.contactPersonDesignation && (
                            <div className="text-muted small">{c.contactPersonDesignation}</div>
                          )}
                        </td>
                        <td>
                          <span className={`badge ${CONTACT_ROLE_BADGE[c.role]}`}>{CONTACT_ROLE_LABEL[c.role]}</span>
                        </td>
                        <td className="text-muted small">{c.notes ?? "—"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          <div className="card shadow-sm border-0 mb-4">
            <div className="card-header bg-white fw-semibold d-flex justify-content-between align-items-center">
              <span>Quotations</span>
              {canCreateQuotation && (
                <button
                  type="button"
                  className="btn btn-sm btn-outline-primary"
                  onClick={() => navigate(`/quotations/new?opportunityId=${opportunity.id}`)}
                >
                  <i className="bi bi-plus-lg me-1" aria-hidden="true" />
                  Create Quotation
                </button>
              )}
            </div>
            {quotations.length === 0 ? (
              <div className="card-body text-muted small">
                No quotations yet — once you're ready to price this deal, create one to move it toward a sales order.
              </div>
            ) : (
              <div className="table-responsive">
                <table className="table mb-0 align-middle">
                  <thead className="table-light">
                    <tr>
                      <th>Number</th>
                      <th className="text-end">Total</th>
                      <th>Status</th>
                    </tr>
                  </thead>
                  <tbody>
                    {quotations.map((q) => (
                      <tr key={q.id} role="button" onClick={() => navigate(`/quotations/${q.id}`)}>
                        <td>
                          {q.quotationNumber}
                          {q.version > 1 && <span className="text-muted"> v{q.version}</span>}
                        </td>
                        <td className="text-end">{q.grandTotal.toLocaleString()}</td>
                        <td>
                          <span className={`badge ${QUOTATION_STATUS_BADGE[q.status]}`}>{q.status}</span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>

          <ActivityTimelinePanel relatedToType="Opportunity" relatedToId={opportunity.id} users={users} />

          <DocumentsPanel entityType="Opportunity" entityId={opportunity.id} />

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
