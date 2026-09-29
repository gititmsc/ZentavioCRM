/**
 * Shared localStorage/sessionStorage key constants + tiny read/write helpers for the platform
 * admin session. Deliberately prefixed "pa_" (not "sms_", which the tenant-facing app uses) so
 * the two apps' sessions never collide if both are somehow opened against the same origin.
 *
 * Unlike the tenant app, there is no refresh token here — PlatformJwt sessions are a single
 * long-lived (480 minute) access token with no refresh endpoint (see PlatformJwtSettings /
 * PlatformAuthController). When it expires, the admin just logs in again.
 */

export const TOKEN_STORAGE_KEY = "pa_auth_token";
export const ADMIN_STORAGE_KEY = "pa_auth_admin";
export const AUTH_STATE_STORAGE_KEY = "pa_auth_state";

export function getToken(): string | null {
  return window.localStorage.getItem(TOKEN_STORAGE_KEY) ?? window.sessionStorage.getItem(TOKEN_STORAGE_KEY);
}

export function clearSession(): void {
  window.localStorage.removeItem(TOKEN_STORAGE_KEY);
  window.localStorage.removeItem(ADMIN_STORAGE_KEY);
  window.sessionStorage.removeItem(TOKEN_STORAGE_KEY);
  window.sessionStorage.removeItem(ADMIN_STORAGE_KEY);

  for (const storage of [window.localStorage, window.sessionStorage]) {
    storage.setItem(AUTH_STATE_STORAGE_KEY, "signed-out");
  }
}
