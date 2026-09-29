import type { TenantStatus } from "@/services/tenantService";

const ICONS: Record<TenantStatus, string> = {
  Provisioning: "bi-hourglass-split",
  Active: "bi-check-circle-fill",
  Suspended: "bi-pause-circle-fill",
  Failed: "bi-x-circle-fill",
  Terminated: "bi-stop-circle-fill",
};

export function StatusBadge({ status }: { status: TenantStatus }) {
  return (
    <span className={`status-badge status-badge--${status.toLowerCase()}`}>
      <i className={`bi ${ICONS[status]}`} aria-hidden="true" />
      {status}
    </span>
  );
}
