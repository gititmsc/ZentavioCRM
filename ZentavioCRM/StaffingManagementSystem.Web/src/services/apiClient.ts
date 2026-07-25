import axios from "axios";

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
  const token = window.localStorage.getItem("sms_auth_token") ?? window.sessionStorage.getItem("sms_auth_token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  const tenantSubdomain = resolveTenantSubdomain();
  if (tenantSubdomain) {
    config.headers["X-Tenant"] = tenantSubdomain;
  }

  return config;
});
