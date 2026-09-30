import { Navigate, Route, Routes } from "react-router-dom";
import { ProtectedRoute } from "@/routes/ProtectedRoute";
import { MainLayout } from "@/layouts/MainLayout";
import Login from "@/pages/login/Login";
import Signup from "@/pages/signup/Signup";
import { Dashboard } from "@/pages/dashboard/Dashboard";
import { TenantsList } from "@/pages/tenants/TenantsList";
import { TenantDetail } from "@/pages/tenants/TenantDetail";
import { AuditLogPage } from "@/pages/auditlog/AuditLogPage";
import { PlatformAdminsPage } from "@/pages/admins/PlatformAdminsPage";

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/signup" element={<Signup />} />

      <Route
        element={
          <ProtectedRoute>
            <MainLayout />
          </ProtectedRoute>
        }
      >
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="/dashboard" element={<Dashboard />} />
        <Route path="/tenants" element={<TenantsList />} />
        <Route path="/tenants/:id" element={<TenantDetail />} />
        <Route path="/audit-log" element={<AuditLogPage />} />
        <Route path="/admins" element={<PlatformAdminsPage />} />
      </Route>

      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
}
