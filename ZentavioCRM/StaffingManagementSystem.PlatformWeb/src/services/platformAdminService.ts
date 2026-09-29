/** Platform admin roster — calls PlatformAdminsController (/api/platform/admins). */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";
import type { ApiResponse } from "@/services/authService";
import type { PlatformAdmin } from "@/services/authService";

export interface CreatePlatformAdminRequest {
  email: string;
  firstName: string;
  lastName?: string;
  password: string;
}

async function getAll(): Promise<ApiResponse<PlatformAdmin[]>> {
  return callApi(() => apiClient.get<ApiResponse<PlatformAdmin[]>>("/api/platform/admins"));
}

async function create(request: CreatePlatformAdminRequest): Promise<ApiResponse<PlatformAdmin>> {
  return callApi(() => apiClient.post<ApiResponse<PlatformAdmin>>("/api/platform/admins", request));
}

export const platformAdminService = { getAll, create };
