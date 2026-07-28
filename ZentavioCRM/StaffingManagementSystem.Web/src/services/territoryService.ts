/**
 * Territories API — thin wrapper around ZentavioCRM.Api's TerritoriesController.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";
import type { ApiResponse } from "@/services/authService";

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

const getById = (id: string) => callApi<Territory>(apiClient.get(`/api/territories/${id}`));

const create = (request: SaveTerritoryRequest) => callApi<Territory>(apiClient.post("/api/territories", request));

const update = (id: string, request: SaveTerritoryRequest) =>
  callApi<Territory>(apiClient.put(`/api/territories/${id}`, request));

const remove = (id: string) => callApi<boolean>(apiClient.delete(`/api/territories/${id}`));

export const territoryService = { getAll, getById, create, update, remove };

export type { ApiResponse };
