import { createContext, useContext, useMemo, useState, type ReactNode } from "react";
import { authService, type AuthUser } from "@/services/authService";

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
  const [user, setUser] = useState<AuthUser | null>(() => authService.getStoredUser());
  const [avatarVersion, setAvatarVersion] = useState(0);

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: user !== null,
      setSession: (nextUser: AuthUser) => setUser(nextUser),
      logout: () => {
        authService.logout();
        setUser(null);
      },
      hasPermission: (code: string) => user?.permissions?.includes(code) ?? false,
      avatarVersion,
      bumpAvatarVersion: () => setAvatarVersion((v) => v + 1),
    }),
    [user, avatarVersion]
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
