/**
 * Users administration API — thin wrapper around ZentavioCRM.Api's UsersController.
 * Distinct from authService, which only handles the current session's login/logout.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";
import type { PagedResult } from "@/services/leadService";

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
  territoryId: string | null;
  territoryName: string | null;
  hasProfilePhoto: boolean;
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
  territoryId: string | null;
}

export interface UpdateUserRequest {
  firstName: string;
  lastName: string;
  mobile: string | null;
  roleId: string;
  departmentId: string | null;
  reportingManagerId: string | null;
  territoryId: string | null;
  isActive: boolean;
}

const getAll = () => callApi<ManagedUser[]>(apiClient.get("/api/users"));

export interface UserSearchParams {
  search?: string;
  roleId?: string;
  departmentId?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

/** Paged, filterable, sortable — powers the Users administration list grid. Distinct from getAll(), which many dropdowns rely on. */
const search = (params: UserSearchParams) =>
  callApi<PagedResult<ManagedUser>>(apiClient.get("/api/users/search", { params }));

const getById = (id: string) => callApi<ManagedUser>(apiClient.get(`/api/users/${id}`));

const create = (request: CreateUserRequest) => callApi<ManagedUser>(apiClient.post("/api/users", request));

const update = (id: string, request: UpdateUserRequest) =>
  callApi<ManagedUser>(apiClient.put(`/api/users/${id}`, request));

/** Fetches the user's avatar as a blob: URL (so the <img> gets it with the Authorization header attached), or null if the user has none / the request fails. Caller is responsible for revoking the returned URL via URL.revokeObjectURL when done with it. */
const getPhotoBlobUrl = async (id: string): Promise<string | null> => {
  try {
    const response = await apiClient.get(`/api/users/${id}/photo`, { responseType: "blob" });
    return URL.createObjectURL(response.data as Blob);
  } catch {
    return null;
  }
};

const uploadPhoto = (id: string, file: File) => {
  const formData = new FormData();
  formData.append("file", file);
  return callApi<ManagedUser>(
    apiClient.post(`/api/users/${id}/photo`, formData, { headers: { "Content-Type": "multipart/form-data" } })
  );
};

const deletePhoto = (id: string) => callApi<boolean>(apiClient.delete(`/api/users/${id}/photo`));

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

/** Mirrors LoginResponseDto — a fresh token pair so the caller's own session keeps working after their old refresh tokens are revoked. */
export interface ChangePasswordResult {
  token: string;
  expiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
}

const changePassword = (id: string, request: ChangePasswordRequest) =>
  callApi<ChangePasswordResult>(apiClient.post(`/api/users/${id}/change-password`, request));

/** Admin-initiated reset for another user — requires Users.Manage. No current-password proof. */
const resetPassword = (id: string, newPassword: string) =>
  callApi<boolean>(apiClient.post(`/api/users/${id}/reset-password`, { newPassword }));

export const userService = {
  getAll,
  search,
  getById,
  create,
  update,
  getPhotoBlobUrl,
  uploadPhoto,
  deletePhoto,
  changePassword,
  resetPassword,
};
