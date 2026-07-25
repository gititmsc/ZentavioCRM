import { Navigate, Route, Routes } from "react-router-dom";
import Login from "@/pages/login/Login";
import Dashboard from "@/pages/dashboard/Dashboard";
import DepartmentsList from "@/pages/departments/DepartmentsList";
import DepartmentForm from "@/pages/departments/DepartmentForm";
import UsersList from "@/pages/users/UsersList";
import UserForm from "@/pages/users/UserForm";
import RolesList from "@/pages/roles/RolesList";
import RoleForm from "@/pages/roles/RoleForm";
import CustomersList from "@/pages/customers/CustomersList";
import CustomerForm from "@/pages/customers/CustomerForm";
import LeadsList from "@/pages/leads/LeadsList";
import LeadForm from "@/pages/leads/LeadForm";
import LeadDetail from "@/pages/leads/LeadDetail";
import OpportunitiesList from "@/pages/opportunities/OpportunitiesList";
import OpportunityForm from "@/pages/opportunities/OpportunityForm";
import OpportunityDetail from "@/pages/opportunities/OpportunityDetail";
import { ProtectedRoute } from "@/routes/ProtectedRoute";
import { MainLayout } from "@/layouts/MainLayout";

export function AppRoutes() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/login" replace />} />
      <Route path="/login" element={<Login />} />

      <Route
        element={
          <ProtectedRoute>
            <MainLayout />
          </ProtectedRoute>
        }
      >
        <Route path="/dashboard" element={<Dashboard />} />

        <Route path="/departments" element={<DepartmentsList />} />
        <Route path="/departments/new" element={<DepartmentForm />} />
        <Route path="/departments/:id/edit" element={<DepartmentForm />} />

        <Route path="/users" element={<UsersList />} />
        <Route path="/users/new" element={<UserForm />} />
        <Route path="/users/:id/edit" element={<UserForm />} />

        <Route path="/roles" element={<RolesList />} />
        <Route path="/roles/new" element={<RoleForm />} />
        <Route path="/roles/:id/edit" element={<RoleForm />} />

        <Route path="/customers" element={<CustomersList />} />
        <Route path="/customers/new" element={<CustomerForm />} />
        <Route path="/customers/:id/edit" element={<CustomerForm />} />

        <Route path="/leads" element={<LeadsList />} />
        <Route path="/leads/new" element={<LeadForm />} />
        <Route path="/leads/:id" element={<LeadDetail />} />
        <Route path="/leads/:id/edit" element={<LeadForm />} />

        <Route path="/opportunities" element={<OpportunitiesList />} />
        <Route path="/opportunities/new" element={<OpportunityForm />} />
        <Route path="/opportunities/:id" element={<OpportunityDetail />} />
        <Route path="/opportunities/:id/edit" element={<OpportunityForm />} />
      </Route>

      <Route path="*" element={<Navigate to="/login" replace />} />
    </Routes>
  );
}
