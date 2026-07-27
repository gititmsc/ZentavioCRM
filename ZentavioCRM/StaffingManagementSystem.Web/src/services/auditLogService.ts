/**
 * Audit log API — thin wrapper around ZentavioCRM.Api's AuditLogsController.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";

export interface AuditLogEntry {
  id: string;
  entityType: string;
  entityId: string;
  action: string;
  summary: string;
  performedByUserName: string | null;
  createdAtUtc: string;
}

const getForEntity = (entityType: string, entityId: string) =>
  callApi<AuditLogEntry[]>(apiClient.get("/api/audit-logs", { params: { entityType, entityId } }));

export const auditLogService = { getForEntity };
