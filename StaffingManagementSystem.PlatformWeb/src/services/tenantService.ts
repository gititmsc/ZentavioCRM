/**
 * Tenant management — calls ZentavioCRM.Api's TenantsController -> /api/platform/tenants.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";

export type TenantStatus = "PendingActivation" | "Active" | "Suspended" | "Stopped";
export type PlanTier = "Trial" | "Starter" | "Professional" | "Enterprise";

export interface Tenant {
  id: string;
  name: string;
  subdomain: string;
  databaseName: string;
  status: TenantStatus;
  adminEmail: string;
  createdAtUtc: string;
  activatedAtUtc: string | null;
  planTier: PlanTier;
  maxUsers: number;
  maxStorageMB: number;
  maxRecords: number;
}

const getAll = () => callApi<Tenant[]>(apiClient.get("/api/platform/tenants"));

const getById = (id: string) => callApi<Tenant>(apiClient.get(`/api/platform/tenants/${id}`));

export const tenantService = { getAll, getById };
