/**
 * Tenant management — calls ZentavioCRM.Api's TenantsController -> /api/platform/tenants.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";

/** Matches Core.Enums.TenantStatus exactly (serialized as string by JsonStringEnumConverter). */
export type TenantStatus = "Provisioning" | "Active" | "Suspended" | "Failed" | "Terminated";
/** Matches Core.Enums.PlanTier exactly. */
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

export interface TenantUsage {
  tenantId: string;
  planTier: PlanTier;
  userCount: number;
  maxUsers: number;
  recordCount: number;
  maxRecords: number;
  databaseSizeMB: number;
  maxStorageMB: number;
  lastActivityAtUtc: string | null;
}

export interface ProvisionTenantRequest {
  planTier?: PlanTier;
  companyName: string;
  subdomain: string;
  adminFirstName: string;
  adminLastName: string;
  adminEmail: string;
  adminPassword: string;
}

export interface UpdateTenantPlanRequest {
  planTier: PlanTier;
  maxUsers?: number | null;
  maxStorageMB?: number | null;
  maxRecords?: number | null;
}

const getAll = () => callApi<Tenant[]>(apiClient.get("/api/platform/tenants"));

const getById = (id: string) => callApi<Tenant>(apiClient.get(`/api/platform/tenants/${id}`));

const getUsage = (id: string) => callApi<TenantUsage>(apiClient.get(`/api/platform/tenants/${id}/usage`));

const provision = (request: ProvisionTenantRequest) =>
  callApi<Tenant>(apiClient.post("/api/platform/tenants", request));

const updatePlan = (id: string, request: UpdateTenantPlanRequest) =>
  callApi<Tenant>(apiClient.patch(`/api/platform/tenants/${id}/plan`, request));

const suspend = (id: string, reason?: string) =>
  callApi<Tenant>(apiClient.patch(`/api/platform/tenants/${id}/suspend`, { reason }));

const reactivate = (id: string) => callApi<Tenant>(apiClient.patch(`/api/platform/tenants/${id}/reactivate`, {}));

const stop = (id: string, reason?: string) =>
  callApi<Tenant>(apiClient.patch(`/api/platform/tenants/${id}/stop`, { reason }));

export const tenantService = { getAll, getById, getUsage, provision, updatePlan, suspend, reactivate, stop };
