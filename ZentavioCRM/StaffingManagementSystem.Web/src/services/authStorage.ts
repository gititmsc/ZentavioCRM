/**
 * Shared localStorage/sessionStorage key constants + tiny read/write helpers for the auth session.
 * Extracted into its own module (rather than living inside authService.ts) so apiClient.ts's
 * response interceptor can read/write the exact same keys without creating an
 * authService <-> apiClient import cycle.
 */

export const TOKEN_STORAGE_KEY = "sms_auth_token";
export const REFRESH_TOKEN_STORAGE_KEY = "sms_refresh_token";
export const USER_STORAGE_KEY = "sms_auth_user";
export const AUTH_STATE_STORAGE_KEY = "sms_auth_state";

/**
 * Whichever storage currently holds the access token — localStorage (from a "Remember Me" login)
 * takes precedence over sessionStorage. Falls back to sessionStorage when neither has a token yet,
 * matching the "not remembered" default used at login.
 */
export function getActiveStorage(): Storage {
  return window.localStorage.getItem(TOKEN_STORAGE_KEY) !== null
    ? window.localStorage
    : window.sessionStorage;
}

export function getToken(): string | null {
  return (
    window.localStorage.getItem(TOKEN_STORAGE_KEY) ??
    window.sessionStorage.getItem(TOKEN_STORAGE_KEY)
  );
}

export function getRefreshToken(): string | null {
  return (
    window.localStorage.getItem(REFRESH_TOKEN_STORAGE_KEY) ??
    window.sessionStorage.getItem(REFRESH_TOKEN_STORAGE_KEY)
  );
}

/** Overwrites the access + refresh token in whichever storage the session already lives in
 * (localStorage for a "Remember Me" login, sessionStorage otherwise). */
export function persistRefreshedTokens(
  token: string,
  refreshToken: string,
): void {
  const storage = getActiveStorage();
  storage.setItem(TOKEN_STORAGE_KEY, token);
  storage.setItem(REFRESH_TOKEN_STORAGE_KEY, refreshToken);
}

export function clearSession(): void {
  window.localStorage.removeItem(TOKEN_STORAGE_KEY);
  window.localStorage.removeItem(REFRESH_TOKEN_STORAGE_KEY);
  window.localStorage.removeItem(USER_STORAGE_KEY);
  window.sessionStorage.removeItem(TOKEN_STORAGE_KEY);
  window.sessionStorage.removeItem(REFRESH_TOKEN_STORAGE_KEY);
  window.sessionStorage.removeItem(USER_STORAGE_KEY);

  for (const storage of [window.localStorage, window.sessionStorage]) {
    storage.setItem(AUTH_STATE_STORAGE_KEY, "signed-out");
  }
}
