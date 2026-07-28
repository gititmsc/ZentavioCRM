/**
 * Quotations API — thin wrapper around ZentavioCRM.Api's QuotationsController.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";
import type { PagedResult } from "@/services/leadService";

export type QuotationStatus = "Draft" | "Sent" | "Accepted" | "Rejected" | "Expired";

export interface QuotationListItem {
  id: string;
  quotationNumber: string;
  version: number;
  opportunityId: string;
  opportunityName: string;
  customerId: string;
  customerName: string;
  status: QuotationStatus;
  grandTotal: number;
  validUntil: string | null;
  assignedToUserId: string | null;
  assignedToUserName: string | null;
  createdAtUtc: string;
}

export interface QuotationLineItem {
  id: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  discountPercent: number | null;
  taxPercent: number | null;
  lineTotal: number;
}

export interface Quotation {
  id: string;
  quotationNumber: string;
  version: number;
  opportunityId: string;
  opportunityName: string;
  customerId: string;
  customerName: string;
  status: QuotationStatus;
  validUntil: string | null;
  termsAndConditions: string | null;
  notes: string | null;
  subtotal: number;
  taxTotal: number;
  grandTotal: number;
  assignedToUserId: string | null;
  assignedToUserName: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  hasSalesOrder: boolean;
  lineItems: QuotationLineItem[];
}

export interface SaveQuotationLineItemRequest {
  productName: string;
  quantity: number;
  unitPrice: number;
  discountPercent: number | null;
  taxPercent: number | null;
}

export interface CreateQuotationRequest {
  opportunityId: string;
  validUntil: string | null;
  termsAndConditions: string | null;
  notes: string | null;
  assignedToUserId: string | null;
  lineItems: SaveQuotationLineItemRequest[];
}

export interface UpdateQuotationRequest {
  validUntil: string | null;
  termsAndConditions: string | null;
  notes: string | null;
  lineItems: SaveQuotationLineItemRequest[];
}

export interface QuotationSearchParams {
  search?: string;
  status?: QuotationStatus;
  opportunityId?: string;
  customerId?: string;
  page?: number;
  pageSize?: number;
}

const search = (params: QuotationSearchParams) =>
  callApi<PagedResult<QuotationListItem>>(apiClient.get("/api/quotations", { params }));

const getById = (id: string) => callApi<Quotation>(apiClient.get(`/api/quotations/${id}`));

const create = (request: CreateQuotationRequest) => callApi<Quotation>(apiClient.post("/api/quotations", request));

const update = (id: string, request: UpdateQuotationRequest) =>
  callApi<Quotation>(apiClient.put(`/api/quotations/${id}`, request));

const updateStatus = (id: string, status: QuotationStatus) =>
  callApi<Quotation>(apiClient.patch(`/api/quotations/${id}/status`, { status }));

const assign = (id: string, userId: string) =>
  callApi<Quotation>(apiClient.post(`/api/quotations/${id}/assign`, { userId }));

const createNewVersion = (id: string) => callApi<Quotation>(apiClient.post(`/api/quotations/${id}/new-version`, {}));

const remove = (id: string) => callApi<boolean>(apiClient.delete(`/api/quotations/${id}`));

export const quotationService = { search, getById, create, update, updateStatus, assign, createNewVersion, remove };
