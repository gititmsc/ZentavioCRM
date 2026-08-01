/**
 * Roles API — thin wrapper around ZentavioCRM.Api's RolesController.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";
import type { PagedResult } from "@/services/leadService";

/** How much of the Leads/Customers/Opportunities record-set a user with this role can see: only their own records, their department's, or everyone's. */
export type VisibilityScope = "Own" | "Team" | "All";

export interface Role {
  id: string;
  name: string;
  description: string | null;
  isSystemRole: boolean;
  visibilityScope: VisibilityScope;
  permissionCodes: string[];
}

export interface SaveRoleRequest {
  name: string;
  description: string | null;
  visibilityScope: VisibilityScope;
  permissionCodes: string[];
}

/** Every permission code in the system, grouped by module — e.g. { "Leads": ["Leads.View", ...] }. */
export type PermissionCatalog = Record<string, string[]>;

const getAll = () => callApi<Role[]>(apiClient.get("/api/roles"));

export interface RoleSearchParams {
  search?: string;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

/** Paged, filterable, sortable — powers the Roles administration list grid. Distinct from getAll(), which the permission-catalog/role pickers rely on. */
const search = (params: RoleSearchParams) =>
  callApi<PagedResult<Role>>(apiClient.get("/api/roles/search", { params }));

const getById = (id: string) => callApi<Role>(apiClient.get(`/api/roles/${id}`));

const getPermissionCatalog = () => callApi<PermissionCatalog>(apiClient.get("/api/roles/permissions"));

const create = (request: SaveRoleRequest) => callApi<Role>(apiClient.post("/api/roles", request));

const update = (id: string, request: SaveRoleRequest) => callApi<Role>(apiClient.put(`/api/roles/${id}`, request));

const remove = (id: string) => callApi<boolean>(apiClient.delete(`/api/roles/${id}`));

export const roleService = { getAll, search, getById, getPermissionCatalog, create, update, remove };
