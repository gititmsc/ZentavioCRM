import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { salesOrderService, type SalesOrder } from "@/services/salesOrderService";
import { userService, type ManagedUser } from "@/services/userService";
import { PermissionCodes } from "@/services/permissionCodes";
import { HistoryPanel } from "@/components/history/HistoryPanel";
import { PageHeader } from "@/components/layout/PageHeader";
import { FormSection } from "@/components/form/FormSection";

const STATUS_BADGE: Record<string, string> = {
  Draft: "text-bg-secondary",
  Confirmed: "text-bg-info",
  PartiallyDelivered: "text-bg-warning",
  Delivered: "text-bg-success",
  Cancelled: "text-bg-danger",
};

export default function SalesOrderDetail() {
  const { id } = useParams<{ id: string }>();
  const { hasPermission } = useAuth();

  const [order, setOrder] = useState<SalesOrder | null>(null);
  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [assignToUserId, setAssignToUserId] = useState("");
  const [deliveryQuantities, setDeliveryQuantities] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  const canEdit = hasPermission(PermissionCodes.SalesOrdersEdit);
  const canAssign = hasPermission(PermissionCodes.SalesOrdersAssign);

  const load = async () => {
    if (!id) return;
    setIsLoading(true);
    const [orderResult, usersResult] = await Promise.all([salesOrderService.getById(id), userService.getAll()]);
    setIsLoading(false);

    if (!orderResult.success || !orderResult.data) {
      setError(orderResult.message || "Unable to load sales order.");
      return;
    }
    setOrder(orderResult.data);
    setAssignToUserId(orderResult.data.assignedToUserId ?? "");
    setDeliveryQuantities({});

    if (usersResult.success && usersResult.data) setUsers(usersResult.data);
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  const handleAssign = async () => {
    if (!id || !assignToUserId) return;
    setActionError(null);
    const result = await salesOrderService.assign(id, assignToUserId);
    if (!result.success) {
      setActionError(result.message || "Unable to assign sales order.");
      return;
    }
    load();
  };

  const handleCancel = async () => {
    if (!id) return;
    if (!window.confirm("Cancel this sales order?")) return;
    setActionError(null);
    const result = await salesOrderService.cancel(id);
    if (!result.success) {
      setActionError(result.message || "Unable to cancel sales order.");
      return;
    }
    load();
  };

  const handleRecordDelivery = async () => {
    if (!id || !order) return;
    const lines = Object.entries(deliveryQuantities)
      .map(([lineItemId, qty]) => ({ lineItemId, deliveredQuantity: Number(qty) }))
      .filter((line) => line.deliveredQuantity > 0);

    if (lines.length === 0) return;

    setActionError(null);
    const result = await salesOrderService.recordDelivery(id, lines);
    if (!result.success) {
      setActionError(result.message || "Unable to record delivery.");
      return;
    }
    load();
  };

  if (isLoading) {
    return <div className="text-muted">Loading...</div>;
  }

  if (error || !order) {
    return <div className="alert alert-danger">{error || "Sales order not found."}</div>;
  }

  const canDeliver = canEdit && order.status !== "Cancelled" && order.status !== "Delivered";

  return (
    <div>
      <PageHeader
        title={`Sales Order ${order.salesOrderNumber}`}
        subtitle={
          <>
            From quotation <Link to={`/quotations/${order.quotationId}`}>{order.quotationNumber}</Link>
          </>
        }
        badge={<span className={`badge fs-6 ${STATUS_BADGE[order.status]}`}>{order.status}</span>}
        backTo="/sales-orders"
        backLabel="Back to Sales Orders"
        actions={
          <>
            {canEdit && order.status !== "Cancelled" && order.status !== "Delivered" && (
              <button type="button" className="btn btn-outline-danger" onClick={handleCancel}>
                <i className="bi bi-x-circle me-1" aria-hidden="true" />
                Cancel Order
              </button>
            )}
          </>
        }
      />

      {actionError && <div className="alert alert-danger">{actionError}</div>}

      <div className="row g-4">
        <div className="col-lg-8">
          <FormSection icon="bi-cart-check" title="Order Details" description="Customer, totals, and delivery expectations.">
            <div className="row g-3">
              <div className="col-md-6">
                <div className="text-muted small">Customer</div>
                <div>
                  <Link to={`/customers/${order.customerId}/edit`}>{order.customerName}</Link>
                </div>
              </div>
              <div className="col-md-6">
                <div className="text-muted small">Grand Total</div>
                <div className="fw-semibold">{order.grandTotal.toLocaleString()}</div>
              </div>
              <div className="col-md-6">
                <div className="text-muted small">Order Date</div>
                <div>{new Date(order.orderDate).toLocaleDateString()}</div>
              </div>
              <div className="col-md-6">
                <div className="text-muted small">Expected Delivery</div>
                <div>{order.expectedDeliveryDate ? new Date(order.expectedDeliveryDate).toLocaleDateString() : "—"}</div>
              </div>
              {order.notes && (
                <div className="col-12">
                  <div className="text-muted small">Notes</div>
                  <div>{order.notes}</div>
                </div>
              )}
            </div>
          </FormSection>

          <FormSection icon="bi-truck" title="Line Items &amp; Delivery">
            <div className="table-responsive">
              <table className="table mb-0 align-middle">
                <thead className="table-light">
                  <tr>
                    <th>Product</th>
                    <th className="text-end">Ordered</th>
                    <th className="text-end">Delivered</th>
                    <th className="text-end">Remaining</th>
                    <th className="text-end">Total</th>
                    {canDeliver && <th className="text-end">Deliver Now</th>}
                  </tr>
                </thead>
                <tbody>
                  {order.lineItems.map((li) => {
                    const remaining = li.quantity - li.deliveredQuantity;
                    return (
                      <tr key={li.id}>
                        <td>{li.productName}</td>
                        <td className="text-end">{li.quantity}</td>
                        <td className="text-end">{li.deliveredQuantity}</td>
                        <td className="text-end">{remaining}</td>
                        <td className="text-end">{li.lineTotal.toLocaleString()}</td>
                        {canDeliver && (
                          <td className="text-end" style={{ width: 120 }}>
                            {remaining > 0 && (
                              <input
                                type="number"
                                min={0}
                                max={remaining}
                                step="0.01"
                                className="form-control form-control-sm"
                                value={deliveryQuantities[li.id] ?? ""}
                                onChange={(e) =>
                                  setDeliveryQuantities((prev) => ({ ...prev, [li.id]: e.target.value }))
                                }
                              />
                            )}
                          </td>
                        )}
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
            {canDeliver && (
              <div className="mt-3">
                <button type="button" className="btn btn-primary btn-sm" onClick={handleRecordDelivery}>
                  Record Delivery
                </button>
              </div>
            )}
          </FormSection>

          <HistoryPanel entityType="SalesOrder" entityId={order.id} />
        </div>

        <div className="col-lg-4">
          <FormSection icon="bi-flag" title="Status">
            <span className={`badge fs-6 ${STATUS_BADGE[order.status]}`}>{order.status}</span>
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
              <div>Created: {new Date(order.createdAtUtc).toLocaleString()}</div>
              {order.updatedAtUtc && <div>Updated: {new Date(order.updatedAtUtc).toLocaleString()}</div>}
            </div>
          </FormSection>
        </div>
      </div>
    </div>
  );
}
