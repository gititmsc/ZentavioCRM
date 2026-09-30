import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { useNavigate } from "react-router-dom";
import { authService, type PlatformAdmin } from "@/services/authService";
import { AUTH_STATE_STORAGE_KEY, TOKEN_STORAGE_KEY } from "@/services/authStorage";

interface AuthContextValue {
  admin: PlatformAdmin | null;
  isAuthenticated: boolean;
  setSession: (admin: PlatformAdmin) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [admin, setAdmin] = useState<PlatformAdmin | null>(() => authService.getStoredAdmin());
  const navigate = useNavigate();

  useEffect(() => {
    const syncAuthState = () => {
      const nextAdmin = authService.getStoredAdmin();
      const hasSession = Boolean(
        localStorage.getItem(TOKEN_STORAGE_KEY) ?? sessionStorage.getItem(TOKEN_STORAGE_KEY)
      );
      const authState = localStorage.getItem(AUTH_STATE_STORAGE_KEY) ?? sessionStorage.getItem(AUTH_STATE_STORAGE_KEY);
      const isSignedOut = authState === "signed-out";
      const isAuthPage = window.location.pathname === "/login";

      if (!hasSession || isSignedOut) {
        setAdmin(null);
        if (!isAuthPage) {
          window.location.assign("/login?reason=logged-out");
        }
        return;
      }

      setAdmin(nextAdmin);
    };

    const handleVisibilityChange = () => {
      if (document.visibilityState === "visible") {
        syncAuthState();
      }
    };

    window.addEventListener("storage", syncAuthState);
    window.addEventListener("focus", syncAuthState);
    document.addEventListener("visibilitychange", handleVisibilityChange);

    return () => {
      window.removeEventListener("storage", syncAuthState);
      window.removeEventListener("focus", syncAuthState);
      document.removeEventListener("visibilitychange", handleVisibilityChange);
    };
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({
      admin,
      isAuthenticated: admin !== null,
      setSession: (nextAdmin: PlatformAdmin) => setAdmin(nextAdmin),
      logout: () => {
        setAdmin(null);
        authService.logout();
        // Explicit SPA navigation rather than relying solely on ProtectedRoute's conditional
        // render — guarantees the admin lands on /login the instant they click Sign Out.
        navigate("/login", { replace: true });
      },
    }),
    [admin, navigate]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
}
