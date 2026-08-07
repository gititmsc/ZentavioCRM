/**
 * Authentication service for the Staffing Management System login flow.
 * Calls StaffingManagementSystem.Api -> POST /api/auth/login (AuthController -> IAuthService).
 */
import { AxiosError } from "axios";
import { apiClient } from "@/services/apiClient";
import {
  TOKEN_STORAGE_KEY,
  REFRESH_TOKEN_STORAGE_KEY,
  USER_STORAGE_KEY,
  AUTH_STATE_STORAGE_KEY,
  getToken,
  getRefreshToken,
  clearSession,
} from "@/services/authStorage";

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe: boolean;
}

export interface AuthUser {
  id: string;
  fullName: string;
  email: string;
  role: string;
  permissions: string[];
}

export interface AuthResult {
  token: string;
  refreshToken: string;
  user: AuthUser;
}

/** Standard API envelope returned by every endpoint. */
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

interface LoginResponseData {
  token: string;
  expiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  user: {
    id: string;
    fullName: string;
    email: string;
    role: string;
    permissions: string[];
  };
}

/** Attempts to sign the user in via the Staffing Management System API. */
async function login(request: LoginRequest): Promise<ApiResponse<AuthResult>> {
  try {
    const response = await apiClient.post<ApiResponse<LoginResponseData>>(
      "/api/auth/login",
      {
        email: request.email,
        password: request.password,
        rememberMe: request.rememberMe,
      },
    );

    if (!response.data.success || !response.data.data) {
      return {
        success: false,
        message: response.data.message || "Invalid email or password.",
        errors: response.data.errors,
      };
    }

    return {
      success: true,
      message: response.data.message,
      data: {
        token: response.data.data.token,
        refreshToken: response.data.data.refreshToken,
        user: response.data.data.user,
      },
    };
  } catch (error) {
    const axiosError = error as AxiosError<ApiResponse<LoginResponseData>>;
    const apiMessage = axiosError.response?.data?.message;

    return {
      success: false,
      message: apiMessage ?? "Unable to reach the server. Please try again.",
      errors: axiosError.response?.data?.errors,
    };
  }
}

function persistSession(result: AuthResult, rememberMe: boolean): void {
  const userPayload = JSON.stringify(result.user);
  const storages = rememberMe
    ? [window.localStorage, window.sessionStorage]
    : [window.localStorage, window.sessionStorage];

  for (const storage of storages) {
    storage.setItem(TOKEN_STORAGE_KEY, result.token);
    storage.setItem(REFRESH_TOKEN_STORAGE_KEY, result.refreshToken);
    storage.setItem(USER_STORAGE_KEY, userPayload);
    storage.setItem(AUTH_STATE_STORAGE_KEY, "signed-in");
  }
}

function getStoredUser(): AuthUser | null {
  const raw = window.localStorage.getItem(USER_STORAGE_KEY) ?? window.sessionStorage.getItem(USER_STORAGE_KEY);
  return raw ? (JSON.parse(raw) as AuthUser) : null;
}

/**
 * Best-effort server-side logout (revokes the refresh token) followed by clearing the local
 * session. The local session is always cleared even if the API call fails/never returns, so the
 * user is never stuck "logged in" on the client just because the server was unreachable.
 */
async function logout(): Promise<void> {
  const refreshToken = getRefreshToken();

  if (refreshToken) {
    try {
      await apiClient.post("/api/auth/logout", { refreshToken });
    } catch {
      // Ignored — the user is signing out regardless of whether the server confirms it.
    }
  }

  clearSession();
}

export const authService = {
  login,
  logout,
  getToken,
  getStoredUser,
  persistSession,
};
