/**
 * Sales Orders API — thin wrapper around ZentavioCRM.Api's SalesOrdersController.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";
import type { PagedResult } from "@/services/leadService";

export type SalesOrderStatus = "Draft" | "Confirmed" | "PartiallyDelivered" | "Delivered" | "Cancelled";

export interface SalesOrderListItem {
  id: string;
  salesOrderNumber: string;
  quotationId: string;
  quotationNumber: string;
  customerId: string;
  customerName: string;
  status: SalesOrderStatus;
  grandTotal: number;
  orderDate: string;
  expectedDeliveryDate: string | null;
  assignedToUserId: string | null;
  assignedToUserName: string | null;
}

export interface SalesOrderLineItem {
  id: string;
  productName: string;
  quantity: number;
  unitPrice: number;
  discountPercent: number | null;
  taxPercent: number | null;
  deliveredQuantity: number;
  lineTotal: number;
}

export interface SalesOrder {
  id: string;
  salesOrderNumber: string;
  quotationId: string;
  quotationNumber: string;
  customerId: string;
  customerName: string;
  status: SalesOrderStatus;
  orderDate: string;
  expectedDeliveryDate: string | null;
  notes: string | null;
  subtotal: number;
  taxTotal: number;
  grandTotal: number;
  assignedToUserId: string | null;
  assignedToUserName: string | null;
  createdAtUtc: string;
  updatedAtUtc: string | null;
  lineItems: SalesOrderLineItem[];
}

export interface ConvertQuotationToSalesOrderRequest {
  quotationId: string;
  expectedDeliveryDate: string | null;
  notes: string | null;
  assignedToUserId: string | null;
}

export interface RecordDeliveryLineRequest {
  lineItemId: string;
  deliveredQuantity: number;
}

export interface SalesOrderSearchParams {
  search?: string;
  status?: SalesOrderStatus;
  customerId?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

const search = (params: SalesOrderSearchParams) =>
  callApi<PagedResult<SalesOrderListItem>>(apiClient.get("/api/sales-orders", { params }));

const getById = (id: string) => callApi<SalesOrder>(apiClient.get(`/api/sales-orders/${id}`));

const convertFromQuotation = (request: ConvertQuotationToSalesOrderRequest) =>
  callApi<SalesOrder>(apiClient.post("/api/sales-orders/from-quotation", request));

const update = (id: string, expectedDeliveryDate: string | null, notes: string | null) =>
  callApi<SalesOrder>(apiClient.put(`/api/sales-orders/${id}`, { expectedDeliveryDate, notes }));

const assign = (id: string, userId: string) =>
  callApi<SalesOrder>(apiClient.post(`/api/sales-orders/${id}/assign`, { userId }));

const recordDelivery = (id: string, lines: RecordDeliveryLineRequest[]) =>
  callApi<SalesOrder>(apiClient.post(`/api/sales-orders/${id}/deliveries`, { lines }));

const cancel = (id: string) => callApi<SalesOrder>(apiClient.post(`/api/sales-orders/${id}/cancel`, {}));

export const salesOrderService = { search, getById, convertFromQuotation, update, assign, recordDelivery, cancel };
