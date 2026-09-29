/**
 * Platform Admin login — calls ZentavioCRM.Api's PlatformAuthController -> POST /api/platform/auth/login.
 */
import { apiClient } from "@/services/apiClient";
import { callApi, type ApiResponse } from "@/services/apiHelpers";
import { TOKEN_STORAGE_KEY, ADMIN_STORAGE_KEY, clearSession } from "@/services/authStorage";

export interface PlatformAdmin {
  id: string;
  email: string;
  fullName: string;
  isActive: boolean;
  createdAtUtc: string;
  lastLoginAtUtc: string | null;
}

export interface PlatformLoginRequest {
  email: string;
  password: string;
}

interface PlatformLoginResponse {
  token: string;
  expiresAtUtc: string;
  admin: PlatformAdmin;
}

async function login(request: PlatformLoginRequest): Promise<ApiResponse<PlatformLoginResponse>> {
  const result = await callApi<PlatformLoginResponse>(apiClient.post("/api/platform/auth/login", request));

  if (result.success && result.data) {
    window.localStorage.setItem(TOKEN_STORAGE_KEY, result.data.token);
    window.localStorage.setItem(ADMIN_STORAGE_KEY, JSON.stringify(result.data.admin));
  }

  return result;
}

function logout(): void {
  clearSession();
}

function getStoredAdmin(): PlatformAdmin | null {
  const raw = window.localStorage.getItem(ADMIN_STORAGE_KEY);
  return raw ? (JSON.parse(raw) as PlatformAdmin) : null;
}

export const platformAuthService = { login, logout, getStoredAdmin };
