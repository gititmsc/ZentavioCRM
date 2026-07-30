import axios, { type AxiosError, type InternalAxiosRequestConfig } from "axios";
import { getRefreshToken, getToken, persistRefreshedTokens, clearSession } from "@/services/authStorage";

/** Base URL of ZentavioCRM.Api, e.g. https://localhost:7001 */
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "https://localhost:7056";

/**
 * Which tenant this browser tab belongs to, sent as the X-Tenant header on every request so the
 * Api's TenantResolutionMiddleware can pick the right tenant database — this is the header-based
 * equivalent of production subdomain routing (acme.zentaviocrm.com), which local dev can't do
 * without editing /etc/hosts or DNS.
 *
 * Resolution order:
 *   1. VITE_TENANT_SUBDOMAIN — explicit override, handy for pointing plain localhost at one tenant.
 *   2. The first label of window.location.hostname, when it looks like a real subdomain
 *      (e.g. "acme.localhost" or "acme.zentaviocrm.com" -> "acme"). Bare "localhost" or a plain
 *      IP has no subdomain, so no header is sent — the Api then falls back to
 *      Tenancy:DefaultTenantConnectionStringName for local development.
 */
function resolveTenantSubdomain(): string | null {
  const override = import.meta.env.VITE_TENANT_SUBDOMAIN;
  if (override) {
    return override;
  }

  const hostname = window.location.hostname.toLowerCase();
  const labels = hostname.split(".");

  const looksLikeIp = /^\d+\.\d+\.\d+\.\d+$/.test(hostname);
  if (looksLikeIp || labels.length < 2) {
    return null;
  }

  const [first] = labels;
  return first === "www" ? null : first;
}

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

  const tenantSubdomain = resolveTenantSubdomain();
  if (tenantSubdomain) {
    config.headers["X-Tenant"] = tenantSubdomain;
  }

  return config;
});

/**
 * Silent-refresh handling for the 15-minute access token (see JwtSettings.AccessTokenExpiryMinutes).
 * Without this, every request made after the token expires would just fail with a 401 and the app
 * would appear to silently stop working — this is the fix for that.
 *
 * A 401 on any request (other than the auth endpoints themselves) triggers exactly one refresh
 * attempt, shared across every request that hits a 401 at the same time via `refreshPromise` so a
 * page with several concurrent API calls doesn't fire several redundant refresh calls. If the
 * refresh succeeds, the original request is retried once with the new token. If it fails — meaning
 * the refresh token itself is expired, revoked, or missing — the session is cleared and the user is
 * sent to the login page with a clear "please log in again" message, instead of failing silently.
 */
interface RetriableRequestConfig extends InternalAxiosRequestConfig {
  _isRetry?: boolean;
}

const AUTH_EXEMPT_PATHS = ["/api/auth/login", "/api/auth/refresh", "/api/auth/logout"];

let refreshPromise: Promise<string | null> | null = null;

function isAuthExempt(url?: string): boolean {
  return url != null && AUTH_EXEMPT_PATHS.some((path) => url.includes(path));
}

interface RefreshResponseData {
  success: boolean;
  data?: { token: string; refreshToken: string };
}

async function performRefresh(): Promise<string | null> {
  const refreshToken = getRefreshToken();
  if (!refreshToken) {
    return null;
  }

  try {
    // apiClient (not bare axios) so the request interceptor above still attaches the X-Tenant
    // header — the RefreshTokens table lives in the tenant's own database.
    const response = await apiClient.post<RefreshResponseData>("/api/auth/refresh", { refreshToken });

    if (!response.data.success || !response.data.data) {
      return null;
    }

    persistRefreshedTokens(response.data.data.token, response.data.data.refreshToken);
    return response.data.data.token;
  } catch {
    return null;
  }
}

function redirectToExpiredSession(): void {
  clearSession();
  if (!window.location.pathname.startsWith("/login")) {
    window.location.assign("/login?reason=expired");
  }
}

apiClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as RetriableRequestConfig | undefined;

    if (
      error.response?.status !== 401 ||
      !originalRequest ||
      originalRequest._isRetry ||
      isAuthExempt(originalRequest.url)
    ) {
      return Promise.reject(error);
    }

    originalRequest._isRetry = true;

    refreshPromise ??= performRefresh().finally(() => {
      refreshPromise = null;
    });

    const newToken = await refreshPromise;

    if (!newToken) {
      redirectToExpiredSession();
      return Promise.reject(error);
    }

    originalRequest.headers = originalRequest.headers ?? {};
    originalRequest.headers.Authorization = `Bearer ${newToken}`;
    return apiClient(originalRequest);
  }
);
