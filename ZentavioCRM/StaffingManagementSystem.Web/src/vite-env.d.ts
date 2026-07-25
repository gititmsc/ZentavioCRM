/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string;
  /** Overrides tenant subdomain resolution — see apiClient.ts. Handy for plain localhost dev. */
  readonly VITE_TENANT_SUBDOMAIN?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
