import type { PaymentStatus } from "@/services/tenantService";

const ICONS: Record<PaymentStatus, string> = {
  Paid: "bi-check-circle-fill",
  Unpaid: "bi-hourglass-split",
  Overdue: "bi-exclamation-triangle-fill",
};

export function PaymentStatusBadge({ status }: { status: PaymentStatus }) {
  return (
    <span className={`payment-status-badge payment-status-badge--${status.toLowerCase()}`}>
      <i className={`bi ${ICONS[status]}`} aria-hidden="true" />
      {status}
    </span>
  );
}
