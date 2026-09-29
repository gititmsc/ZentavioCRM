import { createContext, useContext, useMemo, useState, type ReactNode } from "react";
import { platformAuthService, type PlatformAdmin } from "@/services/platformAuthService";

interface PlatformAuthContextValue {
  admin: PlatformAdmin | null;
  isAuthenticated: boolean;
  setSession: (admin: PlatformAdmin) => void;
  logout: () => void;
}

const PlatformAuthContext = createContext<PlatformAuthContextValue | undefined>(undefined);

export function PlatformAuthProvider({ children }: { children: ReactNode }) {
  const [admin, setAdmin] = useState<PlatformAdmin | null>(() => platformAuthService.getStoredAdmin());

  const value = useMemo<PlatformAuthContextValue>(
    () => ({
      admin,
      isAuthenticated: admin !== null,
      setSession: (nextAdmin: PlatformAdmin) => setAdmin(nextAdmin),
      logout: () => {
        platformAuthService.logout();
        setAdmin(null);
      },
    }),
    [admin]
  );

  return <PlatformAuthContext.Provider value={value}>{children}</PlatformAuthContext.Provider>;
}

export function usePlatformAuth(): PlatformAuthContextValue {
  const ctx = useContext(PlatformAuthContext);
  if (!ctx) throw new Error("usePlatformAuth must be used within a PlatformAuthProvider");
  return ctx;
}
