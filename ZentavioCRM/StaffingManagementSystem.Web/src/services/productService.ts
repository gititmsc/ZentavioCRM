/**
 * Product & Service Catalog API — thin wrapper around ZentavioCRM.Api's ProductsController.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";
import type { ApiResponse } from "@/services/authService";
import type { PagedResult } from "@/services/leadService";

export type ProductType = "Product" | "Service";

export interface Product {
  id: string;
  sku: string;
  name: string;
  type: ProductType;
  category: string | null;
  brand: string | null;
  unitOfMeasure: string | null;
  unitPrice: number;
  cost: number | null;
  taxPercent: number | null;
  description: string | null;
  durationMinutes: number | null;
  billingType: string | null;
  isActive: boolean;
  createdAtUtc: string;
  updatedAtUtc: string | null;
}

export interface SaveProductRequest {
  sku: string;
  name: string;
  type: ProductType;
  category: string | null;
  brand: string | null;
  unitOfMeasure: string | null;
  unitPrice: number;
  cost: number | null;
  taxPercent: number | null;
  description: string | null;
  durationMinutes: number | null;
  billingType: string | null;
  isActive: boolean;
}

/** Unpaged, active-only list — powers the "pick from catalog" selector on Opportunity/Quotation line items. */
const getAllActive = () => callApi<Product[]>(apiClient.get("/api/products"));

export interface ProductSearchParams {
  search?: string;
  type?: ProductType;
  category?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

/** Paged, filterable, sortable — powers the Product Catalog administration list grid. Distinct from getAllActive(), which the line-item picker relies on. */
const search = (params: ProductSearchParams) =>
  callApi<PagedResult<Product>>(apiClient.get("/api/products/search", { params }));

const getById = (id: string) => callApi<Product>(apiClient.get(`/api/products/${id}`));

const create = (request: SaveProductRequest) => callApi<Product>(apiClient.post("/api/products", request));

const update = (id: string, request: SaveProductRequest) =>
  callApi<Product>(apiClient.put(`/api/products/${id}`, request));

const remove = (id: string) => callApi<boolean>(apiClient.delete(`/api/products/${id}`));

export const productService = { getAllActive, search, getById, create, update, remove };

export type { ApiResponse };
