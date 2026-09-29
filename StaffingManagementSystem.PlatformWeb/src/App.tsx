import { BrowserRouter, Routes, Route } from "react-router-dom";
import { PlatformAuthProvider } from "@/context/PlatformAuthContext";
import { ProtectedRoute } from "@/routes/ProtectedRoute";
import Login from "@/pages/Login";
import TenantsList from "@/pages/TenantsList";

export default function App() {
  return (
    <PlatformAuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <TenantsList />
              </ProtectedRoute>
            }
          />
        </Routes>
      </BrowserRouter>
    </PlatformAuthProvider>
  );
}
