/**
 * Tenant lifecycle, plan/usage and impersonation — calls ZentavioCRM.Api's
 * TenantsController (/api/platform/tenants/*). Every method here requires a Platform Admin
 * session (apiClient attaches the Bearer token automatically).
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";
import type { ApiResponse } from "@/services/authService";

export type TenantStatus = "Provisioning" | "Active" | "Suspended" | "Failed" | "Terminated";
export type PlanTier = "Trial" | "Starter" | "Professional" | "Enterprise";

export const PLAN_TIERS: PlanTier[] = ["Trial", "Starter", "Professional", "Enterprise"];

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

export interface ProvisionTenantRequest {
  planTier?: PlanTier;
  companyName: string;
  subdomain: string;
  adminFirstName: string;
  adminLastName?: string;
  adminEmail: string;
  adminPassword: string;
}

export interface UpdateTenantPlanRequest {
  planTier: PlanTier;
  maxUsers?: number;
  maxStorageMB?: number;
  maxRecords?: number;
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

export interface ImpersonateResult {
  token: string;
  expiresAtUtc: string;
  tenantSubdomain: string;
  impersonatedUserId: string;
  impersonatedUserEmail: string;
  impersonatedUserFullName: string;
}

async function getAll(): Promise<ApiResponse<Tenant[]>> {
  return callApi(() => apiClient.get<ApiResponse<Tenant[]>>("/api/platform/tenants"));
}

async function getById(id: string): Promise<ApiResponse<Tenant>> {
  return callApi(() => apiClient.get<ApiResponse<Tenant>>(`/api/platform/tenants/${id}`));
}

async function provision(request: ProvisionTenantRequest): Promise<ApiResponse<Tenant>> {
  return callApi(() => apiClient.post<ApiResponse<Tenant>>("/api/platform/tenants", request));
}

async function suspend(id: string, reason?: string): Promise<ApiResponse<Tenant>> {
  return callApi(() => apiClient.patch<ApiResponse<Tenant>>(`/api/platform/tenants/${id}/suspend`, { reason }));
}

async function reactivate(id: string): Promise<ApiResponse<Tenant>> {
  return callApi(() => apiClient.patch<ApiResponse<Tenant>>(`/api/platform/tenants/${id}/reactivate`));
}

async function stop(id: string, reason?: string): Promise<ApiResponse<Tenant>> {
  return callApi(() => apiClient.patch<ApiResponse<Tenant>>(`/api/platform/tenants/${id}/stop`, { reason }));
}

async function updatePlan(id: string, request: UpdateTenantPlanRequest): Promise<ApiResponse<Tenant>> {
  return callApi(() => apiClient.patch<ApiResponse<Tenant>>(`/api/platform/tenants/${id}/plan`, request));
}

async function getUsage(id: string): Promise<ApiResponse<TenantUsage>> {
  return callApi(() => apiClient.get<ApiResponse<TenantUsage>>(`/api/platform/tenants/${id}/usage`));
}

/** Requires a stated reason — see ImpersonateTenantRequest on the backend. This is the single
 * most security-sensitive action in the app; the UI should always confirm before calling it. */
async function impersonate(id: string, reason: string): Promise<ApiResponse<ImpersonateResult>> {
  return callApi(() => apiClient.post<ApiResponse<ImpersonateResult>>(`/api/platform/tenants/${id}/impersonate`, { reason }));
}

export const tenantService = {
  getAll,
  getById,
  provision,
  suspend,
  reactivate,
  stop,
  updatePlan,
  getUsage,
  impersonate,
};
