/**
 * Platform admin roster — calls ZentavioCRM.Api's PlatformAdminsController -> /api/platform/admins.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";
import type { PlatformAdmin } from "@/services/platformAuthService";

export interface CreatePlatformAdminRequest {
  email: string;
  firstName: string;
  lastName: string;
  password: string;
}

const getAll = () => callApi<PlatformAdmin[]>(apiClient.get("/api/platform/admins"));

const create = (request: CreatePlatformAdminRequest) =>
  callApi<PlatformAdmin>(apiClient.post("/api/platform/admins", request));

export const platformAdminService = { getAll, create };
