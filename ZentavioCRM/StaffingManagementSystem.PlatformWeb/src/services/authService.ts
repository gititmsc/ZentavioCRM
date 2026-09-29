/**
 * Authentication service for the Platform Admin login flow.
 * Calls ZentavioCRM.Api -> POST /api/platform/auth/login (PlatformAuthController -> IPlatformAdminService).
 */
import { AxiosError } from "axios";
import { apiClient } from "@/services/apiClient";
import { TOKEN_STORAGE_KEY, ADMIN_STORAGE_KEY, AUTH_STATE_STORAGE_KEY, getToken } from "@/services/authStorage";

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe: boolean;
}

export interface PlatformAdmin {
  id: string;
  email: string;
  fullName: string;
  isActive: boolean;
  createdAtUtc: string;
  lastLoginAtUtc: string | null;
}

export interface AuthResult {
  token: string;
  expiresAtUtc: string;
  admin: PlatformAdmin;
}

/** Standard API envelope returned by every endpoint. */
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

async function login(request: LoginRequest): Promise<ApiResponse<AuthResult>> {
  try {
    const response = await apiClient.post<ApiResponse<AuthResult>>("/api/platform/auth/login", {
      email: request.email,
      password: request.password,
    });

    if (!response.data.success || !response.data.data) {
      return {
        success: false,
        message: response.data.message || "Invalid email or password.",
        errors: response.data.errors,
      };
    }

    return { success: true, message: response.data.message, data: response.data.data };
  } catch (error) {
    const axiosError = error as AxiosError<ApiResponse<AuthResult>>;
    const apiMessage = axiosError.response?.data?.message;

    return {
      success: false,
      message: apiMessage ?? "Unable to reach the server. Please try again.",
      errors: axiosError.response?.data?.errors,
    };
  }
}

function persistSession(result: AuthResult, rememberMe: boolean): void {
  const storage = rememberMe ? window.localStorage : window.sessionStorage;

  storage.setItem(TOKEN_STORAGE_KEY, result.token);
  storage.setItem(ADMIN_STORAGE_KEY, JSON.stringify(result.admin));

  for (const s of [window.localStorage, window.sessionStorage]) {
    s.setItem(AUTH_STATE_STORAGE_KEY, "signed-in");
  }
}

function getStoredAdmin(): PlatformAdmin | null {
  const raw = window.localStorage.getItem(ADMIN_STORAGE_KEY) ?? window.sessionStorage.getItem(ADMIN_STORAGE_KEY);
  return raw ? (JSON.parse(raw) as PlatformAdmin) : null;
}

/** No server-side logout call — there's no refresh/session token to revoke, just the local copy. */
function logout(): void {
  window.localStorage.removeItem(TOKEN_STORAGE_KEY);
  window.localStorage.removeItem(ADMIN_STORAGE_KEY);
  window.sessionStorage.removeItem(TOKEN_STORAGE_KEY);
  window.sessionStorage.removeItem(ADMIN_STORAGE_KEY);
  for (const s of [window.localStorage, window.sessionStorage]) {
    s.setItem(AUTH_STATE_STORAGE_KEY, "signed-out");
  }
}

export const authService = {
  login,
  logout,
  getToken,
  getStoredAdmin,
  persistSession,
};
