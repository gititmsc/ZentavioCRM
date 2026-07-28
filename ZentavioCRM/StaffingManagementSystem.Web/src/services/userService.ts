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

export const userService = { getAll, getById, create, update, getPhotoBlobUrl, uploadPhoto, deletePhoto };
