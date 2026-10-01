/**
 * Tags API — thin wrapper around ZentavioCRM.Api's TagsController. Structured, reusable labels
 * shared across Leads and Customers — distinct from Customer's original freeform comma-separated
 * "Tags" text field, which is unchanged and still used by CSV import/export.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";
import type { ApiResponse } from "@/services/authService";

export interface Tag {
  id: string;
  name: string;
  color: string | null;
  createdAtUtc: string;
}

export interface TagWithUsage extends Tag {
  leadCount: number;
  customerCount: number;
}

export interface SaveTagRequest {
  name: string;
  color: string | null;
}

const getAll = () => callApi<Tag[]>(apiClient.get("/api/tags"));

const getAllWithUsage = () => callApi<TagWithUsage[]>(apiClient.get("/api/tags/with-usage"));

const create = (request: SaveTagRequest) => callApi<Tag>(apiClient.post("/api/tags", request));

const update = (id: string, request: SaveTagRequest) => callApi<Tag>(apiClient.put(`/api/tags/${id}`, request));

const remove = (id: string) => callApi<boolean>(apiClient.delete(`/api/tags/${id}`));

export const tagService = { getAll, getAllWithUsage, create, update, remove };

export type { ApiResponse };
