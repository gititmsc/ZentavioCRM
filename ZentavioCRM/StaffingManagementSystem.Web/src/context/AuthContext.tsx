import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useState,
  type ReactNode,
} from "react";
import { authService, type AuthUser } from "@/services/authService";
import {
  AUTH_STATE_STORAGE_KEY,
  TOKEN_STORAGE_KEY,
} from "@/services/authStorage";

interface AuthContextValue {
  user: AuthUser | null;
  isAuthenticated: boolean;
  setSession: (user: AuthUser) => void;
  logout: () => void;
  hasPermission: (code: string) => boolean;
  /** Bumped whenever the signed-in user's own profile photo changes, so any
   *  <UserAvatar> showing the current user (e.g. the topbar) knows to refetch
   *  even though its own props (userId/fullName) never changed. */
  avatarVersion: number;
  bumpAvatarVersion: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() =>
    authService.getStoredUser(),
  );
  const [avatarVersion, setAvatarVersion] = useState(0);

  useEffect(() => {
    const syncAuthState = () => {
      const nextUser = authService.getStoredUser();
      const hasSession = Boolean(
        localStorage.getItem(TOKEN_STORAGE_KEY) ??
        sessionStorage.getItem(TOKEN_STORAGE_KEY),
      );
      const authState =
        localStorage.getItem(AUTH_STATE_STORAGE_KEY) ??
        sessionStorage.getItem(AUTH_STATE_STORAGE_KEY);

      const isSignedOut = authState === "signed-out";
      const isAuthPage =
        window.location.pathname === "/login" ||
        window.location.pathname.startsWith("/forgot-password") ||
        window.location.pathname.startsWith("/reset-password");

      if (!hasSession || isSignedOut) {
        setUser(null);
        if (!isAuthPage) {
          window.location.assign("/login?reason=logged-out");
        }
        return;
      }

      if (nextUser) {
        setUser(nextUser);
      } else {
        setUser(null);
      }
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
      user,
      isAuthenticated: user !== null,
      setSession: (nextUser: AuthUser) => setUser(nextUser),
      logout: () => {
        setUser(null);
        void authService.logout();
      },
      hasPermission: (code: string) =>
        user?.permissions?.includes(code) ?? false,
      avatarVersion,
      bumpAvatarVersion: () => setAvatarVersion((v) => v + 1),
    }),
    [user, avatarVersion],
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
