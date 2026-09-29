/** Platform-level audit trail — calls PlatformAuditLogController (/api/platform/audit-log). */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";
import type { ApiResponse } from "@/services/authService";

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

async function getAll(tenantId?: string): Promise<ApiResponse<PlatformAuditLogEntry[]>> {
  return callApi(() =>
    apiClient.get<ApiResponse<PlatformAuditLogEntry[]>>("/api/platform/audit-log", {
      params: tenantId ? { tenantId } : undefined,
    })
  );
}

export const auditLogService = { getAll };
