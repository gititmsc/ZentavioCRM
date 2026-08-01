/**
 * Opportunities API — thin wrapper around ZentavioCRM.Api's OpportunitiesController.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";
import type { PagedResult } from "@/services/leadService";

export type OpportunityStage =
  | "Qualification"
  | "Discovery"
  | "Proposal"
  | "Negotiation"
  | "VerbalCommit"
  | "ClosedWon"
  | "ClosedLost";

export type OpportunityContactRole =
  | "Champion"
  | "EconomicBuyer"
  | "Blocker"
  | "Influencer"
  | "DecisionMaker"
  | "TechnicalEvaluator"
  | "Other";

export interface OpportunityListItem {
  id: string;
  opportunityNumber: string;
  name: string;
  customerId: string;
  customerName: string;
  value: number | null;
  currencyCode: string;
  probability: number | null;
  expectedCloseDate: string | null;
  stage: OpportunityStage;
  assignedToUserId: string | null;
  assignedToUserName: string | null;
  createdAtUtc: string;
}

export interface OpportunityLineItem {
  id: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  discountPercent: number | null;
  lineTotal: number;
}

export interface OpportunityContact {
  id: string;
  contactPersonId: string;
  contactPersonName: string;
  contactPersonDesignation: string | null;
  role: OpportunityContactRole;
  notes: string | null;
}

export interface Opportunity {
  id: string;
  opportunityNumber: string;
  name: string;
  customerId: string;
  customerName: string;
  value: number | null;
  currencyCode: string;
  probability: number | null;
  products: string | null;
  competitors: string | null;
  expectedCloseDate: string | null;
  stage: OpportunityStage;
  assignedToUserId: string | null;
  assignedToUserName: string | null;
  sourceLeadId: string | null;
  notes: string | null;
  nextStep: string | null;
  nextStepDate: string | null;
  lostReason: string | null;
  closedAtUtc: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  lineItems: OpportunityLineItem[];
  contacts: OpportunityContact[];
}

export interface SaveOpportunityLineItemRequest {
  productName: string;
  quantity: number;
  unitPrice: number;
  discountPercent: number | null;
}

export interface SaveOpportunityContactRequest {
  contactPersonId: string;
  role: OpportunityContactRole;
  notes: string | null;
}

export interface SaveOpportunityRequest {
  name: string;
  customerId: string;
  value: number | null;
  currencyCode: string | null;
  probability: number | null;
  products: string | null;
  competitors: string | null;
  expectedCloseDate: string | null;
  assignedToUserId: string | null;
  notes: string | null;
  nextStep: string | null;
  nextStepDate: string | null;
  lineItems: SaveOpportunityLineItemRequest[];
  contacts: SaveOpportunityContactRequest[];
}

export interface OpportunitySearchParams {
  search?: string;
  stage?: OpportunityStage;
  customerId?: string;
  assignedToUserId?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

const search = (params: OpportunitySearchParams) =>
  callApi<PagedResult<OpportunityListItem>>(apiClient.get("/api/opportunities", { params }));

const getById = (id: string) => callApi<Opportunity>(apiClient.get(`/api/opportunities/${id}`));

const create = (request: SaveOpportunityRequest) => callApi<Opportunity>(apiClient.post("/api/opportunities", request));

const update = (id: string, request: SaveOpportunityRequest) =>
  callApi<Opportunity>(apiClient.put(`/api/opportunities/${id}`, request));

const updateStage = (id: string, stage: OpportunityStage, reason?: string) =>
  callApi<Opportunity>(apiClient.patch(`/api/opportunities/${id}/stage`, { stage, reason }));

const assign = (id: string, userId: string) =>
  callApi<Opportunity>(apiClient.post(`/api/opportunities/${id}/assign`, { userId }));

const remove = (id: string) => callApi<boolean>(apiClient.delete(`/api/opportunities/${id}`));

export const opportunityService = { search, getById, create, update, updateStage, assign, remove };
