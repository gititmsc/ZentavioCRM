/**
 * Leads API — thin wrapper around ZentavioCRM.Api's LeadsController.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";
import type { ImportResult } from "@/services/importTypes";

/** Standard paged list envelope returned by every list endpoint. */
export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export type LeadStatus =
  | "New"
  | "Assigned"
  | "Contacted"
  | "Qualified"
  | "Nurturing"
  | "ProposalSent"
  | "Converted"
  | "Lost"
  | "Junk";

export type LeadSource =
  | "Website"
  | "LandingPage"
  | "Referral"
  | "Exhibition"
  | "WhatsApp"
  | "Facebook"
  | "LinkedIn"
  | "EmailCampaign"
  | "GoogleAds"
  | "ManualEntry"
  | "ApiIntegration";

export interface LeadListItem {
  id: string;
  leadNumber: string;
  companyName: string;
  contactName: string;
  source: LeadSource;
  status: LeadStatus;
  expectedValue: number | null;
  assignedToUserId: string | null;
  assignedToUserName: string | null;
  createdAtUtc: string;
}

export interface Lead {
  id: string;
  leadNumber: string;
  companyName: string;
  contactName: string;
  email: string | null;
  mobile: string | null;
  industry: string | null;
  source: LeadSource;
  campaign: string | null;
  utmSource: string | null;
  utmMedium: string | null;
  utmCampaign: string | null;
  utmTerm: string | null;
  utmContent: string | null;
  budget: number | null;
  timeline: string | null;
  expectedValue: number | null;
  assignedToUserId: string | null;
  assignedToUserName: string | null;
  territory: string | null;
  territoryId: string | null;
  territoryName: string | null;
  status: LeadStatus;
  leadScore: number | null;
  aiScore: number | null;
  notes: string | null;
  nextFollowUpDate: string | null;
  lostReason: string | null;
  convertedCustomerId: string | null;
  convertedAtUtc: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface SaveLeadRequest {
  companyName: string;
  contactName: string;
  email: string | null;
  mobile: string | null;
  industry: string | null;
  source: LeadSource;
  campaign: string | null;
  utmSource: string | null;
  utmMedium: string | null;
  utmCampaign: string | null;
  utmTerm: string | null;
  utmContent: string | null;
  budget: number | null;
  timeline: string | null;
  expectedValue: number | null;
  assignedToUserId: string | null;
  territory: string | null;
  territoryId: string | null;
  notes: string | null;
  nextFollowUpDate: string | null;
}

export interface LeadSearchParams {
  search?: string;
  status?: LeadStatus;
  assignedToUserId?: string;
  page?: number;
  pageSize?: number;
}

export interface ConvertLeadResult {
  customerId: string;
  customerNumber: string;
}

export interface ConvertLeadToOpportunityRequest {
  opportunityName?: string | null;
  customerDisplayName?: string | null;
  value?: number | null;
  expectedCloseDate?: string | null;
  assignToUserId?: string | null;
}

export interface ConvertLeadToOpportunityResult {
  customerId: string;
  customerNumber: string;
  opportunityId: string;
  opportunityNumber: string;
}

export interface DuplicateMatch {
  type: "Lead" | "Customer";
  id: string;
  name: string;
  email: string | null;
  mobile: string | null;
}

export interface DuplicateCheckResult {
  matches: DuplicateMatch[];
}

const search = (params: LeadSearchParams) =>
  callApi<PagedResult<LeadListItem>>(apiClient.get("/api/leads", { params }));

const getById = (id: string) => callApi<Lead>(apiClient.get(`/api/leads/${id}`));

const create = (request: SaveLeadRequest) => callApi<Lead>(apiClient.post("/api/leads", request));

const update = (id: string, request: SaveLeadRequest) => callApi<Lead>(apiClient.put(`/api/leads/${id}`, request));

const updateStatus = (id: string, status: LeadStatus, reason?: string) =>
  callApi<Lead>(apiClient.patch(`/api/leads/${id}/status`, { status, reason }));

const assign = (id: string, userId: string) => callApi<Lead>(apiClient.post(`/api/leads/${id}/assign`, { userId }));

const convert = (id: string, displayName?: string, assignToUserId?: string) =>
  callApi<ConvertLeadResult>(apiClient.post(`/api/leads/${id}/convert`, { displayName, assignToUserId }));

const convertToOpportunity = (id: string, request: ConvertLeadToOpportunityRequest = {}) =>
  callApi<ConvertLeadToOpportunityResult>(apiClient.post(`/api/leads/${id}/convert-to-opportunity`, request));

const remove = (id: string) => callApi<boolean>(apiClient.delete(`/api/leads/${id}`));

const checkDuplicates = (email?: string | null, mobile?: string | null, excludeLeadId?: string) =>
  callApi<DuplicateCheckResult>(apiClient.get("/api/leads/check-duplicates", { params: { email, mobile, excludeLeadId } }));

const exportCsv = async (): Promise<Blob> => {
  const response = await apiClient.get("/api/leads/export", { responseType: "blob" });
  return response.data;
};

const importCsv = (file: File) => {
  const formData = new FormData();
  formData.append("file", file);
  return callApi<ImportResult>(
    apiClient.post("/api/leads/import", formData, { headers: { "Content-Type": "multipart/form-data" } })
  );
};

export const leadService = {
  search,
  getById,
  create,
  update,
  updateStatus,
  assign,
  convert,
  convertToOpportunity,
  remove,
  checkDuplicates,
  exportCsv,
  importCsv,
};
