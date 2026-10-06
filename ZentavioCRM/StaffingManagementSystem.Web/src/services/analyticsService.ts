/**
 * Analytics API — query engine behind custom dashboards and the report builder, plus saved
 * dashboard/report storage. Thin wrapper around ZentavioCRM.Api's AnalyticsController.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";

export type AnalyticsEntity = "Leads" | "Opportunities" | "Customers" | "Quotations" | "SalesOrders";
export type AnalyticsFieldKind = "Text" | "Number" | "Money" | "Date";
export type AnalyticsMetric = "Count" | "Sum" | "Average";
export type FilterOp = "eq" | "neq" | "in" | "gte" | "lte";
export type TimeBucket = "day" | "week" | "month" | "year";

export interface AnalyticsCatalogField {
  key: string;
  label: string;
  kind: AnalyticsFieldKind;
  groupable: boolean;
}

export interface AnalyticsCatalogEntity {
  entity: AnalyticsEntity;
  label: string;
  fields: AnalyticsCatalogField[];
}

export interface AnalyticsFilter {
  field: string;
  op: FilterOp;
  values: string[];
}

interface AnalyticsBaseRequest {
  entity: AnalyticsEntity;
  dateField?: string | null;
  /** yyyy-MM-dd, inclusive */
  from?: string | null;
  /** yyyy-MM-dd, inclusive */
  to?: string | null;
  mineOnly: boolean;
  filters: AnalyticsFilter[];
}

export interface AnalyticsQueryRequest extends AnalyticsBaseRequest {
  metric: AnalyticsMetric;
  metricField?: string | null;
  /** A groupable text field key, or a date field with a bucket ("createdAt:month"). Null = one overall total. */
  groupBy?: string | null;
  limit: number;
  sortDescending: boolean;
}

export interface AnalyticsRowsRequest extends AnalyticsBaseRequest {
  columns: string[];
  sortBy?: string | null;
  sortDescending: boolean;
  limit: number;
}

export interface AnalyticsQueryRow {
  key: string;
  label: string;
  value: number;
  count: number;
}

export interface AnalyticsQueryResult {
  rows: AnalyticsQueryRow[];
  total: number;
  totalCount: number;
  valueKind: "count" | "money" | "number";
  groupKind: "none" | "category" | "time";
  bucket: TimeBucket | null;
  truncated: boolean;
}

export interface AnalyticsColumn {
  key: string;
  label: string;
  kind: AnalyticsFieldKind;
}

export interface AnalyticsRowsResult {
  columns: AnalyticsColumn[];
  rows: Record<string, string | number | null>[];
  totalCount: number;
  truncated: boolean;
}

export type SavedAnalyticsKind = "Dashboard" | "Report";

export interface SavedAnalyticsItem {
  id: string;
  kind: SavedAnalyticsKind;
  name: string;
  description: string | null;
  ownerUserId: string;
  ownerName: string;
  isShared: boolean;
  isMine: boolean;
  canEdit: boolean;
  configJson: string;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface SaveAnalyticsItemRequest {
  kind: SavedAnalyticsKind;
  name: string;
  description: string | null;
  isShared: boolean;
  configJson: string;
}

const getCatalog = () => callApi<AnalyticsCatalogEntity[]>(apiClient.get("/api/analytics/catalog"));

const query = (request: AnalyticsQueryRequest) =>
  callApi<AnalyticsQueryResult>(apiClient.post("/api/analytics/query", request));

const rows = (request: AnalyticsRowsRequest) =>
  callApi<AnalyticsRowsResult>(apiClient.post("/api/analytics/rows", request));

const exportRows = async (request: AnalyticsRowsRequest, format: "csv" | "xlsx"): Promise<Blob> => {
  const response = await apiClient.post(`/api/analytics/export?format=${format}`, request, { responseType: "blob" });
  return response.data;
};

const listSaved = (kind: SavedAnalyticsKind) =>
  callApi<SavedAnalyticsItem[]>(apiClient.get("/api/analytics/saved", { params: { kind } }));

const getSaved = (id: string) => callApi<SavedAnalyticsItem>(apiClient.get(`/api/analytics/saved/${id}`));

const createSaved = (request: SaveAnalyticsItemRequest) =>
  callApi<SavedAnalyticsItem>(apiClient.post("/api/analytics/saved", request));

const updateSaved = (id: string, request: SaveAnalyticsItemRequest) =>
  callApi<SavedAnalyticsItem>(apiClient.put(`/api/analytics/saved/${id}`, request));

const removeSaved = (id: string) => callApi<boolean>(apiClient.delete(`/api/analytics/saved/${id}`));

export const analyticsService = {
  getCatalog,
  query,
  rows,
  exportRows,
  listSaved,
  getSaved,
  createSaved,
  updateSaved,
  removeSaved,
};
