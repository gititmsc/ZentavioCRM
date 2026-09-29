/**
 * Platform audit trail — calls ZentavioCRM.Api's PlatformAuditLogController -> /api/platform/audit-log.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";

export interface PlatformAuditLogEntry {
  id: string;
  platformAdminId: string | null;
  platformAdminEmail: string | null;
  action: string;
  tenantId: string | null;
  tenantName: string | null;
  summary: string;
  createdAtUtc: string;
}

const getAll = (tenantId?: string) =>
  callApi<PlatformAuditLogEntry[]>(apiClient.get("/api/platform/audit-log", { params: { tenantId } }));

export const auditLogService = { getAll };
