import { Navigate } from "react-router-dom";
import { usePlatformAuth } from "@/context/PlatformAuthContext";

export function ProtectedRoute({ children }: { children: React.ReactElement }) {
  const { isAuthenticated } = usePlatformAuth();
  return isAuthenticated ? children : <Navigate to="/login" replace />;
}
