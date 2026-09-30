/** Global search — calls SearchController (/api/platform/search?q=...). */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";
import type { ApiResponse } from "@/services/authService";

export interface GlobalSearchResult {
  type: "Tenant" | "PlatformAdmin";
  id: string;
  label: string;
  subLabel: string;
}

async function search(term: string): Promise<ApiResponse<GlobalSearchResult[]>> {
  return callApi(() => apiClient.get<ApiResponse<GlobalSearchResult[]>>("/api/platform/search", { params: { q: term } }));
}

export const searchService = { search };
