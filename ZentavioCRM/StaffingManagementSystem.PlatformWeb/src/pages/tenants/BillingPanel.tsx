import { useEffect, useState } from "react";
import {
  tenantService,
  BILLING_CYCLES,
  PAYMENT_STATUSES,
  type BillingCycle,
  type PaymentStatus,
  type Tenant,
  type TenantPayment,
} from "@/services/tenantService";
import { PaymentStatusBadge } from "@/components/PaymentStatusBadge";
import { useAuth } from "@/context/AuthContext";

interface BillingPanelProps {
  tenant: Tenant;
  onTenantChange: (tenant: Tenant) => void;
  onError: (message: string) => void;
  onBanner: (message: string) => void;
}

/** Manual, gateway-free billing: status/amount/cycle/due-date editor plus an append-only payment
 * history ledger. Nothing here talks to a real payment processor — every action is a platform
 * admin recording what they know happened. */
export function BillingPanel({ tenant, onTenantChange, onError, onBanner }: BillingPanelProps) {
  const { isSuperAdmin } = useAuth();
  const [payments, setPayments] = useState<TenantPayment[]>([]);
  const [loadingPayments, setLoadingPayments] = useState(true);
  const [editingBilling, setEditingBilling] = useState(false);
  const [recordingPayment, setRecordingPayment] = useState(false);

  const [paymentStatus, setPaymentStatus] = useState<PaymentStatus>(tenant.paymentStatus);
  const [billingAmount, setBillingAmount] = useState(tenant.billingAmount != null ? String(tenant.billingAmount) : "");
  const [billingCurrency, setBillingCurrency] = useState(tenant.billingCurrency ?? "USD");
  const [billingCycle, setBillingCycle] = useState<BillingCycle | "">(tenant.billingCycle ?? "");
  const [nextDueDate, setNextDueDate] = useState(tenant.nextDueDateUtc ? tenant.nextDueDateUtc.slice(0, 10) : "");
  const [savingBilling, setSavingBilling] = useState(false);

  const [paymentAmount, setPaymentAmount] = useState("");
  const [paymentNote, setPaymentNote] = useState("");
  const [savingPayment, setSavingPayment] = useState(false);

  const loadPayments = async () => {
    setLoadingPayments(true);
    const response = await tenantService.getPayments(tenant.id);
    if (response.success && response.data) {
      setPayments(response.data);
    }
    setLoadingPayments(false);
  };

  useEffect(() => {
    void loadPayments();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tenant.id]);

  const resetBillingForm = () => {
    setPaymentStatus(tenant.paymentStatus);
    setBillingAmount(tenant.billingAmount != null ? String(tenant.billingAmount) : "");
    setBillingCurrency(tenant.billingCurrency ?? "USD");
    setBillingCycle(tenant.billingCycle ?? "");
    setNextDueDate(tenant.nextDueDateUtc ? tenant.nextDueDateUtc.slice(0, 10) : "");
  };

  const handleSaveBilling = async () => {
    setSavingBilling(true);
    try {
      const response = await tenantService.updateBilling(tenant.id, {
        paymentStatus,
        billingAmount: billingAmount ? Number(billingAmount) : undefined,
        billingCurrency: billingCurrency || undefined,
        billingCycle: billingCycle || undefined,
        nextDueDateUtc: nextDueDate ? new Date(nextDueDate).toISOString() : undefined,
      });
      if (response.success && response.data) {
        onTenantChange(response.data);
        onBanner(response.message || "Billing details updated.");
        setEditingBilling(false);
      } else {
        onError(response.message || "Could not update billing details.");
      }
    } finally {
      setSavingBilling(false);
    }
  };

  const handleRecordPayment = async () => {
    if (!paymentAmount || Number(paymentAmount) <= 0) {
      onError("Enter a payment amount greater than zero.");
      return;
    }
    setSavingPayment(true);
    try {
      const response = await tenantService.recordPayment(tenant.id, {
        amount: Number(paymentAmount),
        note: paymentNote.trim() || undefined,
      });
      if (response.success) {
        onBanner("Payment recorded.");
        setPaymentAmount("");
        setPaymentNote("");
        setRecordingPayment(false);
        void loadPayments();
        const refreshed = await tenantService.getById(tenant.id);
        if (refreshed.success && refreshed.data) {
          onTenantChange(refreshed.data);
        }
      } else {
        onError(response.message || "Could not record payment.");
      }
    } finally {
      setSavingPayment(false);
    }
  };

  return (
    <div className="row g-4">
      <div className="col-lg-6">
        <div className="app-card h-100">
          <div className="app-card__header">
            <h3 className="app-card__title">
              <i className="bi bi-receipt" aria-hidden="true" />
              Billing
            </h3>
            {!editingBilling && isSuperAdmin && (
              <button
                type="button"
                className="btn btn-sm btn-outline-secondary"
                onClick={() => {
                  resetBillingForm();
                  setEditingBilling(true);
                }}
              >
                <i className="bi bi-pencil-fill me-1" />
                Edit
              </button>
            )}
          </div>
          <div className="app-card__body">
            {editingBilling ? (
              <div className="border rounded-3 p-3" style={{ borderColor: "var(--itm-border)" }}>
                <div className="mb-3">
                  <label className="form-label">Payment Status</label>
                  <select
                    className="form-select"
                    value={paymentStatus}
                    onChange={(e) => setPaymentStatus(e.target.value as PaymentStatus)}
                  >
                    {PAYMENT_STATUSES.map((status) => (
                      <option key={status} value={status}>
                        {status}
                      </option>
                    ))}
                  </select>
                  {paymentStatus === "Overdue" && tenant.status === "Active" && (
                    <div className="form-text text-warning">
                      <i className="bi bi-exclamation-triangle-fill me-1" />
                      Saving this will automatically suspend the tenant.
                    </div>
                  )}
                </div>

                <div className="row g-2 mb-3">
                  <div className="col-7">
                    <label className="form-label small">Amount</label>
                    <input
                      className="form-control"
                      type="number"
                      min={0}
                      step="0.01"
                      value={billingAmount}
                      onChange={(e) => setBillingAmount(e.target.value)}
                    />
                  </div>
                  <div className="col-5">
                    <label className="form-label small">Currency</label>
                    <input
                      className="form-control"
                      value={billingCurrency}
                      onChange={(e) => setBillingCurrency(e.target.value.toUpperCase())}
                      maxLength={3}
                    />
                  </div>
                </div>

                <div className="row g-2 mb-3">
                  <div className="col-6">
                    <label className="form-label small">Billing Cycle</label>
                    <select
                      className="form-select"
                      value={billingCycle}
                      onChange={(e) => setBillingCycle(e.target.value as BillingCycle | "")}
                    >
                      <option value="">(none)</option>
                      {BILLING_CYCLES.map((cycle) => (
                        <option key={cycle} value={cycle}>
                          {cycle}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="col-6">
                    <label className="form-label small">Next Due Date</label>
                    <input className="form-control" type="date" value={nextDueDate} onChange={(e) => setNextDueDate(e.target.value)} />
                  </div>
                </div>

                <div className="d-flex gap-2 justify-content-end">
                  <button
                    type="button"
                    className="btn btn-outline-secondary btn-sm"
                    onClick={() => setEditingBilling(false)}
                    disabled={savingBilling}
                  >
                    Cancel
                  </button>
                  <button type="button" className="btn btn-primary btn-sm" onClick={() => void handleSaveBilling()} disabled={savingBilling}>
                    {savingBilling ? "Saving..." : "Save Billing"}
                  </button>
                </div>
              </div>
            ) : (
              <div>
                <PaymentStatusBadge status={tenant.paymentStatus} />
                <div className="d-flex flex-column gap-2 mt-3">
                  <div className="d-flex justify-content-between text-muted small">
                    <span>Amount</span>
                    <span className="fw-semibold text-body">
                      {tenant.billingAmount != null
                        ? `${tenant.billingCurrency ?? ""} ${tenant.billingAmount.toLocaleString(undefined, { minimumFractionDigits: 2 })}`
                        : "Not set"}
                    </span>
                  </div>
                  <div className="d-flex justify-content-between text-muted small">
                    <span>Billing Cycle</span>
                    <span className="fw-semibold text-body">{tenant.billingCycle ?? "Not set"}</span>
                  </div>
                  <div className="d-flex justify-content-between text-muted small">
                    <span>Next Due Date</span>
                    <span className="fw-semibold text-body">
                      {tenant.nextDueDateUtc ? new Date(tenant.nextDueDateUtc).toLocaleDateString() : "Not set"}
                    </span>
                  </div>
                </div>
                <div className="text-muted small mt-3 fst-italic">
                  Manually tracked — no payment gateway is connected. Marking Overdue on an Active tenant auto-suspends it;
                  marking Paid does not auto-reactivate.
                </div>
              </div>
            )}
          </div>
        </div>
      </div>

      <div className="col-lg-6">
        <div className="app-card h-100">
          <div className="app-card__header">
            <h3 className="app-card__title">
              <i className="bi bi-cash-coin" aria-hidden="true" />
              Payment History
            </h3>
            {!recordingPayment && isSuperAdmin && (
              <button type="button" className="btn btn-sm btn-primary" onClick={() => setRecordingPayment(true)}>
                <i className="bi bi-plus-lg me-1" />
                Record Payment
              </button>
            )}
          </div>
          <div className="app-card__body">
            {recordingPayment && (
              <div className="border rounded-3 p-3 mb-3" style={{ borderColor: "var(--itm-border)" }}>
                <div className="mb-2">
                  <label className="form-label small">Amount</label>
                  <input
                    className="form-control"
                    type="number"
                    min={0.01}
                    step="0.01"
                    value={paymentAmount}
                    onChange={(e) => setPaymentAmount(e.target.value)}
                    placeholder={tenant.billingCurrency ?? "USD"}
                  />
                </div>
                <div className="mb-2">
                  <label className="form-label small">Note (optional)</label>
                  <input
                    className="form-control"
                    value={paymentNote}
                    onChange={(e) => setPaymentNote(e.target.value)}
                    maxLength={1000}
                    placeholder="e.g. Wire transfer, invoice #1042"
                  />
                </div>
                <div className="d-flex gap-2 justify-content-end mt-2">
                  <button
                    type="button"
                    className="btn btn-outline-secondary btn-sm"
                    onClick={() => {
                      setRecordingPayment(false);
                      setPaymentAmount("");
                      setPaymentNote("");
                    }}
                    disabled={savingPayment}
                  >
                    Cancel
                  </button>
                  <button type="button" className="btn btn-primary btn-sm" onClick={() => void handleRecordPayment()} disabled={savingPayment}>
                    {savingPayment ? "Recording..." : "Record Payment"}
                  </button>
                </div>
              </div>
            )}

            {loadingPayments ? (
              <div className="text-muted small">Loading payment history...</div>
            ) : payments.length === 0 ? (
              <div className="empty-state py-4">
                <div className="empty-state__icon">
                  <i className="bi bi-cash-coin" aria-hidden="true" />
                </div>
                <div className="empty-state__title">No payments recorded yet</div>
              </div>
            ) : (
              <div className="list-group list-group-flush" style={{ maxHeight: 320, overflowY: "auto" }}>
                {payments.map((payment) => (
                  <div key={payment.id} className="list-group-item px-0 py-2">
                    <div className="d-flex justify-content-between">
                      <span className="fw-semibold">
                        {payment.currency} {payment.amount.toLocaleString(undefined, { minimumFractionDigits: 2 })}
                      </span>
                      <span className="text-muted small">{new Date(payment.paidAtUtc).toLocaleDateString()}</span>
                    </div>
                    {payment.note && <div className="text-muted small mt-1">{payment.note}</div>}
                    <div className="text-muted small mt-1">Recorded by {payment.recordedByAdminEmail}</div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
