/** Token/admin storage for the Platform Admin session — always localStorage, no "remember me"
 * concept here since this is an internal admin tool, not a customer-facing login. */
export const TOKEN_STORAGE_KEY = "zcrm_platform_token";
export const ADMIN_STORAGE_KEY = "zcrm_platform_admin";

export function getToken(): string | null {
  return window.localStorage.getItem(TOKEN_STORAGE_KEY);
}

export function clearSession(): void {
  window.localStorage.removeItem(TOKEN_STORAGE_KEY);
  window.localStorage.removeItem(ADMIN_STORAGE_KEY);
}
