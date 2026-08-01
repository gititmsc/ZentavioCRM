/**
 * Territories API — thin wrapper around ZentavioCRM.Api's TerritoriesController.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";
import type { ApiResponse } from "@/services/authService";
import type { PagedResult } from "@/services/leadService";

export interface Territory {
  id: string;
  name: string;
  parentTerritoryId: string | null;
  parentTerritoryName: string | null;
  isActive: boolean;
  userCount: number;
  leadCount: number;
}

export interface SaveTerritoryRequest {
  name: string;
  parentTerritoryId: string | null;
  isActive: boolean;
}

const getAll = () => callApi<Territory[]>(apiClient.get("/api/territories"));

export interface TerritorySearchParams {
  search?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

/** Paged, filterable, sortable — powers the Territories administration list grid. Distinct from getAll(), which territory pickers rely on. */
const search = (params: TerritorySearchParams) =>
  callApi<PagedResult<Territory>>(apiClient.get("/api/territories/search", { params }));

const getById = (id: string) => callApi<Territory>(apiClient.get(`/api/territories/${id}`));

const create = (request: SaveTerritoryRequest) => callApi<Territory>(apiClient.post("/api/territories", request));

const update = (id: string, request: SaveTerritoryRequest) =>
  callApi<Territory>(apiClient.put(`/api/territories/${id}`, request));

const remove = (id: string) => callApi<boolean>(apiClient.delete(`/api/territories/${id}`));

export const territoryService = { getAll, search, getById, create, update, remove };

export type { ApiResponse };
