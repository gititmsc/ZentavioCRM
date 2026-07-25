/**
 * Departments API — thin wrapper around ZentavioCRM.Api's DepartmentsController.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";
import type { ApiResponse } from "@/services/authService";

export interface Department {
  id: string;
  name: string;
  parentDepartmentId: string | null;
  parentDepartmentName: string | null;
  isActive: boolean;
  userCount: number;
}

export interface SaveDepartmentRequest {
  name: string;
  parentDepartmentId: string | null;
  isActive: boolean;
}

const getAll = () => callApi<Department[]>(apiClient.get("/api/departments"));

const getById = (id: string) => callApi<Department>(apiClient.get(`/api/departments/${id}`));

const create = (request: SaveDepartmentRequest) => callApi<Department>(apiClient.post("/api/departments", request));

const update = (id: string, request: SaveDepartmentRequest) =>
  callApi<Department>(apiClient.put(`/api/departments/${id}`, request));

const remove = (id: string) => callApi<boolean>(apiClient.delete(`/api/departments/${id}`));

export const departmentService = { getAll, getById, create, update, remove };

export type { ApiResponse };
