/**
 * Roles API — thin wrapper around ZentavioCRM.Api's RolesController.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";

export interface Role {
  id: string;
  name: string;
  description: string | null;
  isSystemRole: boolean;
  permissionCodes: string[];
}

export interface SaveRoleRequest {
  name: string;
  description: string | null;
  permissionCodes: string[];
}

/** Every permission code in the system, grouped by module — e.g. { "Leads": ["Leads.View", ...] }. */
export type PermissionCatalog = Record<string, string[]>;

const getAll = () => callApi<Role[]>(apiClient.get("/api/roles"));

const getById = (id: string) => callApi<Role>(apiClient.get(`/api/roles/${id}`));

const getPermissionCatalog = () => callApi<PermissionCatalog>(apiClient.get("/api/roles/permissions"));

const create = (request: SaveRoleRequest) => callApi<Role>(apiClient.post("/api/roles", request));

const update = (id: string, request: SaveRoleRequest) => callApi<Role>(apiClient.put(`/api/roles/${id}`, request));

const remove = (id: string) => callApi<boolean>(apiClient.delete(`/api/roles/${id}`));

export const roleService = { getAll, getById, getPermissionCatalog, create, update, remove };
