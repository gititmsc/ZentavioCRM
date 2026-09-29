import axios, { type AxiosError } from "axios";
import { getToken, clearSession } from "@/services/authStorage";

/** Base URL of ZentavioCRM.Api — the same backend the tenant app talks to. Every route this app
 * calls lives under /api/platform, which TenantResolutionMiddleware always bypasses, so this
 * client deliberately never sends an X-Tenant header. */
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "https://localhost:7056";

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    "Content-Type": "application/json",
  },
});

apiClient.interceptors.request.use((config) => {
  const token = getToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

const AUTH_EXEMPT_PATHS = ["/api/platform/auth/login"];

function isAuthExempt(url?: string): boolean {
  return url != null && AUTH_EXEMPT_PATHS.some((path) => url.includes(path));
}

/**
 * PlatformJwt tokens have no refresh flow (see authStorage.ts) — a 401 always means the session
 * is over (expired, or the admin was deactivated), so the only thing to do is clear it and send
 * the admin back to sign in, with a clear reason instead of the app silently breaking.
 */
apiClient.interceptors.response.use(
  (response) => response,
  (error: AxiosError) => {
    if (error.response?.status === 401 && !isAuthExempt(error.config?.url)) {
      clearSession();
      if (!window.location.pathname.startsWith("/login")) {
        window.location.assign("/login?reason=expired");
      }
    }
    return Promise.reject(error);
  }
);
