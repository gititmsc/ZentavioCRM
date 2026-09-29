import { BrowserRouter, Routes, Route } from "react-router-dom";
import { PlatformAuthProvider } from "@/context/PlatformAuthContext";
import { ProtectedRoute } from "@/routes/ProtectedRoute";
import { AdminLayout } from "@/layouts/AdminLayout";
import Login from "@/pages/Login";
import TenantsList from "@/pages/TenantsList";
import PlatformAdminsList from "@/pages/PlatformAdminsList";
import AuditLogList from "@/pages/AuditLogList";

export default function App() {
  return (
    <PlatformAuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route
            element={
              <ProtectedRoute>
                <AdminLayout />
              </ProtectedRoute>
            }
          >
            <Route path="/" element={<TenantsList />} />
            <Route path="/admins" element={<PlatformAdminsList />} />
            <Route path="/audit-log" element={<AuditLogList />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </PlatformAuthProvider>
  );
}
