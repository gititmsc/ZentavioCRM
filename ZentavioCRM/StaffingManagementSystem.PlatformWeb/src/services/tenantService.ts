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
export type PaymentStatus = "Unpaid" | "Paid" | "Overdue";
export type BillingCycle = "OneTime" | "Monthly" | "Yearly";

export const PLAN_TIERS: PlanTier[] = ["Trial", "Starter", "Professional", "Enterprise"];
export const PAYMENT_STATUSES: PaymentStatus[] = ["Unpaid", "Paid", "Overdue"];
export const BILLING_CYCLES: BillingCycle[] = ["OneTime", "Monthly", "Yearly"];

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
  paymentStatus: PaymentStatus;
  billingAmount: number | null;
  billingCurrency: string | null;
  billingCycle: BillingCycle | null;
  nextDueDateUtc: string | null;
  trialEndsAtUtc: string | null;
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

export interface UpdateTenantMetadataRequest {
  name: string;
  adminEmail: string;
}

export interface UpdateTenantBillingRequest {
  paymentStatus?: PaymentStatus;
  billingAmount?: number;
  billingCurrency?: string;
  billingCycle?: BillingCycle;
  nextDueDateUtc?: string;
}

export interface RecordTenantPaymentRequest {
  amount: number;
  currency?: string;
  paidAtUtc?: string;
  note?: string;
}

export interface TenantPayment {
  id: string;
  tenantId: string;
  amount: number;
  currency: string;
  paidAtUtc: string;
  note: string | null;
  recordedByAdminEmail: string;
  createdAtUtc: string;
}

export interface TenantNote {
  id: string;
  tenantId: string;
  note: string;
  createdByAdminEmail: string;
  createdAtUtc: string;
}

/** One tenant/metric pair at or above 80% of its plan limit — see GetTenantsNearLimitsAsync. */
export interface TenantUsageAlert {
  tenantId: string;
  tenantName: string;
  metric: "Users" | "Records" | "Storage";
  current: number;
  max: number;
  percentUsed: number;
  atLimit: boolean;
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

/** Every Active tenant/metric pair at or above 80% of its plan limit — for the Dashboard's
 * "Tenants nearing limits" widget. */
async function getUsageAlerts(): Promise<ApiResponse<TenantUsageAlert[]>> {
  return callApi(() => apiClient.get<ApiResponse<TenantUsageAlert[]>>("/api/platform/tenants/usage-alerts"));
}

/** Requires a stated reason — see ImpersonateTenantRequest on the backend. This is the single
 * most security-sensitive action in the app; the UI should always confirm before calling it. */
async function impersonate(id: string, reason: string): Promise<ApiResponse<ImpersonateResult>> {
  return callApi(() => apiClient.post<ApiResponse<ImpersonateResult>>(`/api/platform/tenants/${id}/impersonate`, { reason }));
}

/** Company name + the denormalized directory admin-email field only — never the tenant's real
 * sign-in email, which lives in that tenant's own database. */
async function updateMetadata(id: string, request: UpdateTenantMetadataRequest): Promise<ApiResponse<Tenant>> {
  return callApi(() => apiClient.patch<ApiResponse<Tenant>>(`/api/platform/tenants/${id}/metadata`, request));
}

/** Manual billing fields only — there is no payment gateway anywhere behind this. Setting
 * paymentStatus to "Overdue" on an Active tenant auto-suspends it. */
async function updateBilling(id: string, request: UpdateTenantBillingRequest): Promise<ApiResponse<Tenant>> {
  return callApi(() => apiClient.patch<ApiResponse<Tenant>>(`/api/platform/tenants/${id}/billing`, request));
}

async function getPayments(id: string): Promise<ApiResponse<TenantPayment[]>> {
  return callApi(() => apiClient.get<ApiResponse<TenantPayment[]>>(`/api/platform/tenants/${id}/payments`));
}

/** Records a manual payment-history entry — never a real charge. */
async function recordPayment(id: string, request: RecordTenantPaymentRequest): Promise<ApiResponse<TenantPayment>> {
  return callApi(() => apiClient.post<ApiResponse<TenantPayment>>(`/api/platform/tenants/${id}/payments`, request));
}

async function getNotes(id: string): Promise<ApiResponse<TenantNote[]>> {
  return callApi(() => apiClient.get<ApiResponse<TenantNote[]>>(`/api/platform/tenants/${id}/notes`));
}

async function addNote(id: string, note: string): Promise<ApiResponse<TenantNote>> {
  return callApi(() => apiClient.post<ApiResponse<TenantNote>>(`/api/platform/tenants/${id}/notes`, { note }));
}

/** Emails the tenant's admin user a fresh welcome/set-password link — for when the original
 * welcome email never arrived. Does not touch or impersonate the account. */
async function resendWelcomeEmail(id: string): Promise<ApiResponse<boolean>> {
  return callApi(() => apiClient.post<ApiResponse<boolean>>(`/api/platform/tenants/${id}/admin-actions/resend-welcome-email`));
}

/** Emails the tenant's admin user a password-reset link, the same as their own "Forgot Password?"
 * flow — for when they're locked out and can't request one themselves. */
async function forcePasswordReset(id: string): Promise<ApiResponse<boolean>> {
  return callApi(() => apiClient.post<ApiResponse<boolean>>(`/api/platform/tenants/${id}/admin-actions/force-password-reset`));
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
  updateMetadata,
  updateBilling,
  getPayments,
  recordPayment,
  getNotes,
  addNote,
  getUsageAlerts,
  resendWelcomeEmail,
  forcePasswordReset,
};
