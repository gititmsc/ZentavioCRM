/**
 * Authentication service for the Staffing Management System login flow.
 * Calls StaffingManagementSystem.Api -> POST /api/auth/login (AuthController -> IAuthService).
 */
import { AxiosError } from "axios";
import { apiClient } from "@/services/apiClient";

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
  user: {
    id: string;
    fullName: string;
    email: string;
    role: string;
    permissions: string[];
  };
}

const TOKEN_STORAGE_KEY = "sms_auth_token";
const USER_STORAGE_KEY = "sms_auth_user";

/** Attempts to sign the user in via the Staffing Management System API. */
async function login(request: LoginRequest): Promise<ApiResponse<AuthResult>> {
  try {
    const response = await apiClient.post<ApiResponse<LoginResponseData>>("/api/auth/login", {
      email: request.email,
      password: request.password,
      rememberMe: request.rememberMe,
    });

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
  const storage = rememberMe ? window.localStorage : window.sessionStorage;
  storage.setItem(TOKEN_STORAGE_KEY, result.token);
  storage.setItem(USER_STORAGE_KEY, JSON.stringify(result.user));
}

function getToken(): string | null {
  return window.localStorage.getItem(TOKEN_STORAGE_KEY) ?? window.sessionStorage.getItem(TOKEN_STORAGE_KEY);
}

function getStoredUser(): AuthUser | null {
  const raw = window.localStorage.getItem(USER_STORAGE_KEY) ?? window.sessionStorage.getItem(USER_STORAGE_KEY);
  return raw ? (JSON.parse(raw) as AuthUser) : null;
}

function logout(): void {
  window.localStorage.removeItem(TOKEN_STORAGE_KEY);
  window.localStorage.removeItem(USER_STORAGE_KEY);
  window.sessionStorage.removeItem(TOKEN_STORAGE_KEY);
  window.sessionStorage.removeItem(USER_STORAGE_KEY);
}

export const authService = {
  login,
  logout,
  getToken,
  getStoredUser,
  persistSession,
};
