/**
 * Users administration API — thin wrapper around ZentavioCRM.Api's UsersController.
 * Distinct from authService, which only handles the current session's login/logout.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";

export interface ManagedUser {
  id: string;
  employeeCode: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  mobile: string | null;
  roleId: string;
  roleName: string;
  departmentId: string | null;
  departmentName: string | null;
  reportingManagerId: string | null;
  reportingManagerName: string | null;
  isActive: boolean;
  lastLoginAtUtc: string | null;
}

export interface CreateUserRequest {
  employeeCode: string;
  firstName: string;
  lastName: string;
  email: string;
  mobile: string | null;
  password: string;
  roleId: string;
  departmentId: string | null;
  reportingManagerId: string | null;
}

export interface UpdateUserRequest {
  firstName: string;
  lastName: string;
  mobile: string | null;
  roleId: string;
  departmentId: string | null;
  reportingManagerId: string | null;
  isActive: boolean;
}

const getAll = () => callApi<ManagedUser[]>(apiClient.get("/api/users"));

const getById = (id: string) => callApi<ManagedUser>(apiClient.get(`/api/users/${id}`));

const create = (request: CreateUserRequest) => callApi<ManagedUser>(apiClient.post("/api/users", request));

const update = (id: string, request: UpdateUserRequest) =>
  callApi<ManagedUser>(apiClient.put(`/api/users/${id}`, request));

export const userService = { getAll, getById, create, update };
