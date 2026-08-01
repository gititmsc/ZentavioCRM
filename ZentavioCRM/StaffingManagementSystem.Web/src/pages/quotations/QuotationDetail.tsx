import { useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { quotationService, type Quotation, type QuotationStatus } from "@/services/quotationService";
import { salesOrderService } from "@/services/salesOrderService";
import { userService, type ManagedUser } from "@/services/userService";
import { PermissionCodes } from "@/services/permissionCodes";
import { HistoryPanel } from "@/components/history/HistoryPanel";
import { PageHeader } from "@/components/layout/PageHeader";
import { FormSection } from "@/components/form/FormSection";

const NEXT_STATUSES: Record<QuotationStatus, QuotationStatus[]> = {
  Draft: ["Sent"],
  Sent: ["Accepted", "Rejected", "Expired"],
  Accepted: [],
  Rejected: [],
  Expired: [],
};

const STATUS_BADGE: Record<QuotationStatus, string> = {
  Draft: "text-bg-secondary",
  Sent: "text-bg-info",
  Accepted: "text-bg-success",
  Rejected: "text-bg-danger",
  Expired: "text-bg-dark",
};

export default function QuotationDetail() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { hasPermission } = useAuth();

  const [quotation, setQuotation] = useState<Quotation | null>(null);
  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [assignToUserId, setAssignToUserId] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [isConverting, setIsConverting] = useState(false);

  const canEdit = hasPermission(PermissionCodes.QuotationsEdit);
  const canAssign = hasPermission(PermissionCodes.QuotationsAssign);
  const canCreate = hasPermission(PermissionCodes.QuotationsCreate);
  const canConvertToOrder = hasPermission(PermissionCodes.SalesOrdersCreate);

  const load = async () => {
    if (!id) return;
    setIsLoading(true);
    const [quotationResult, usersResult] = await Promise.all([quotationService.getById(id), userService.getAll()]);
    setIsLoading(false);

    if (!quotationResult.success || !quotationResult.data) {
      setError(quotationResult.message || "Unable to load quotation.");
      return;
    }
    setQuotation(quotationResult.data);
    setAssignToUserId(quotationResult.data.assignedToUserId ?? "");

    if (usersResult.success && usersResult.data) setUsers(usersResult.data);
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const handleStatusChange = async (status: QuotationStatus) => {
    if (!id) return;
    setActionError(null);
    const result = await quotationService.updateStatus(id, status);
    if (!result.success) {
      setActionError(result.message || "Unable to update quotation status.");
      return;
    }
    load();
  };

  const handleAssign = async () => {
    if (!id || !assignToUserId) return;
    setActionError(null);
    const result = await quotationService.assign(id, assignToUserId);
    if (!result.success) {
      setActionError(result.message || "Unable to assign quotation.");
      return;
    }
    load();
  };

  const handleNewVersion = async () => {
    if (!id) return;
    setActionError(null);
    const result = await quotationService.createNewVersion(id);
    if (!result.success || !result.data) {
      setActionError(result.message || "Unable to create a new version.");
      return;
    }
    navigate(`/quotations/${result.data.id}`);
  };

  const handleConvertToSalesOrder = async () => {
    if (!id || !quotation) return;
    setActionError(null);
    setIsConverting(true);
    const result = await salesOrderService.convertFromQuotation({
      quotationId: id,
      expectedDeliveryDate: null,
      notes: null,
      assignedToUserId: quotation.assignedToUserId,
    });
    setIsConverting(false);

    if (!result.success || !result.data) {
      setActionError(result.message || "Unable to convert this quotation to a sales order.");
      return;
    }
    navigate(`/sales-orders/${result.data.id}`);
  };

  if (isLoading) {
    return <div className="text-muted">Loading...</div>;
  }

  if (error || !quotation) {
    return <div className="alert alert-danger">{error || "Quotation not found."}</div>;
  }

  const nextStatuses = NEXT_STATUSES[quotation.status];
  const isDraft = quotation.status === "Draft";

  return (
    <div>
      <PageHeader
        title={`Quotation for ${quotation.customerName}`}
        subtitle={
          <>
            {quotation.quotationNumber} &middot; Version {quotation.version}
          </>
        }
        badge={<span className={`badge fs-6 ${STATUS_BADGE[quotation.status]}`}>{quotation.status}</span>}
        backTo="/quotations"
        backLabel="Back to Quotations"
        actions={
          <>
            {canEdit && isDraft && (
              <Link to={`/quotations/${quotation.id}/edit`} className="btn btn-outline-secondary">
                <i className="bi bi-pencil me-1" aria-hidden="true" />
                Edit
              </Link>
            )}
            {canCreate && (
              <button type="button" className="btn btn-outline-secondary" onClick={handleNewVersion}>
                <i className="bi bi-file-earmark-plus me-1" aria-hidden="true" />
                New Version
              </button>
            )}
          </>
        }
      />

      {actionError && <div className="alert alert-danger">{actionError}</div>}

      {quotation.status === "Accepted" && !quotation.hasSalesOrder && canConvertToOrder && (
        <div className="alert alert-success d-flex justify-content-between align-items-center">
          <div>This quotation was accepted — ready to convert to a sales order.</div>
          <button type="button" className="btn btn-success btn-sm" disabled={isConverting} onClick={handleConvertToSalesOrder}>
            {isConverting ? "Converting..." : "Convert to Sales Order"}
          </button>
        </div>
      )}
      {quotation.hasSalesOrder && <div className="alert alert-info">A sales order has already been created from this quotation.</div>}

      <div className="row g-4">
        <div className="col-lg-8">
          <FormSection icon="bi-file-earmark-text" title="Quotation Details" description="Opportunity, customer, validity, and terms.">
            <div className="row g-3">
              <div className="col-md-6">
                <div className="text-muted small">Opportunity</div>
                <div>
                  <Link to={`/opportunities/${quotation.opportunityId}`}>{quotation.opportunityName}</Link>
                </div>
              </div>
              <div className="col-md-6">
                <div className="text-muted small">Customer</div>
                <div>
                  <Link to={`/customers/${quotation.customerId}/edit`}>{quotation.customerName}</Link>
                </div>
              </div>
              <div className="col-md-6">
                <div className="text-muted small">Valid Until</div>
                <div>{quotation.validUntil ? new Date(quotation.validUntil).toLocaleDateString() : "—"}</div>
              </div>
              <div className="col-md-6">
                <div className="text-muted small">Grand Total</div>
                <div className="fw-semibold">{quotation.grandTotal.toLocaleString()}</div>
              </div>
              {quotation.termsAndConditions && (
                <div className="col-12">
                  <div className="text-muted small">Terms &amp; Conditions</div>
                  <div style={{ whiteSpace: "pre-wrap" }}>{quotation.termsAndConditions}</div>
                </div>
              )}
              {quotation.notes && (
                <div className="col-12">
                  <div className="text-muted small">Notes</div>
                  <div>{quotation.notes}</div>
                </div>
              )}
            </div>
          </FormSection>

          <FormSection icon="bi-list-check" title="Line Items">
            <div className="table-responsive">
              <table className="table mb-0">
                <thead className="table-light">
                  <tr>
                    <th>Product</th>
                    <th className="text-end">Qty</th>
                    <th className="text-end">Unit Price</th>
                    <th className="text-end">Discount</th>
                    <th className="text-end">Tax</th>
                    <th className="text-end">Total</th>
                  </tr>
                </thead>
                <tbody>
                  {quotation.lineItems.map((li) => (
                    <tr key={li.id}>
                      <td>{li.productName}</td>
                      <td className="text-end">{li.quantity}</td>
                      <td className="text-end">{li.unitPrice.toLocaleString()}</td>
                      <td className="text-end">{li.discountPercent ? `${li.discountPercent}%` : "—"}</td>
                      <td className="text-end">{li.taxPercent ? `${li.taxPercent}%` : "—"}</td>
                      <td className="text-end fw-semibold">{li.lineTotal.toLocaleString()}</td>
                    </tr>
                  ))}
                </tbody>
                <tfoot>
                  <tr>
                    <td colSpan={5} className="text-end text-muted">
                      Subtotal
                    </td>
                    <td className="text-end">{quotation.subtotal.toLocaleString()}</td>
                  </tr>
                  <tr>
                    <td colSpan={5} className="text-end text-muted">
                      Tax
                    </td>
                    <td className="text-end">{quotation.taxTotal.toLocaleString()}</td>
                  </tr>
                  <tr>
                    <td colSpan={5} className="text-end fw-semibold">
                      Grand Total
                    </td>
                    <td className="text-end fw-semibold">{quotation.grandTotal.toLocaleString()}</td>
                  </tr>
                </tfoot>
              </table>
            </div>
          </FormSection>

          <HistoryPanel entityType="Quotation" entityId={quotation.id} />
        </div>

        <div className="col-lg-4">
          <FormSection icon="bi-flag" title="Status">
            <div className="mb-3">
              <span className={`badge fs-6 ${STATUS_BADGE[quotation.status]}`}>{quotation.status}</span>
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
                    Mark {status}
                  </button>
                ))}
              </div>
            ) : (
              <div className="text-muted small">No further status changes available.</div>
            )}
          </FormSection>

          {canAssign && (
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
              <div>Created: {new Date(quotation.createdAtUtc).toLocaleString()}</div>
              {quotation.updatedAtUtc && <div>Updated: {new Date(quotation.updatedAtUtc).toLocaleString()}</div>}
            </div>
          </FormSection>
        </div>
      </div>
    </div>
  );
}
