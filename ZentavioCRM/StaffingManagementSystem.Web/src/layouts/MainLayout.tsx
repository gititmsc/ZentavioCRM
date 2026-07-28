import { NavLink, Outlet } from "react-router-dom";
import { ITMLogo } from "@/components/brand/ITMLogo";
import { useAuth } from "@/context/AuthContext";
import { PermissionCodes } from "@/services/permissionCodes";
import { NotificationBell } from "@/components/notifications/NotificationBell";
import "./MainLayout.css";

interface NavItem {
  to: string;
  icon: string;
  label: string;
  /** Nav item is hidden unless the user has at least one of these permissions. */
  requiresAnyOf?: string[];
}

const NAV_ITEMS: NavItem[] = [
  { to: "/dashboard", icon: "bi-speedometer2", label: "Dashboard" },
  { to: "/leads", icon: "bi-funnel-fill", label: "Leads", requiresAnyOf: [PermissionCodes.LeadsView] },
  { to: "/opportunities", icon: "bi-graph-up-arrow", label: "Opportunities", requiresAnyOf: [PermissionCodes.OpportunitiesView] },
  { to: "/quotations", icon: "bi-file-earmark-text", label: "Quotations", requiresAnyOf: [PermissionCodes.QuotationsView] },
  { to: "/sales-orders", icon: "bi-cart-check", label: "Sales Orders", requiresAnyOf: [PermissionCodes.SalesOrdersView] },
  { to: "/customers", icon: "bi-building", label: "Customers", requiresAnyOf: [PermissionCodes.CustomersView] },
  { to: "/departments", icon: "bi-diagram-3", label: "Departments", requiresAnyOf: [PermissionCodes.DepartmentsView] },
  { to: "/users", icon: "bi-people-fill", label: "Users", requiresAnyOf: [PermissionCodes.UsersView] },
  { to: "/roles", icon: "bi-shield-lock-fill", label: "Roles", requiresAnyOf: [PermissionCodes.RolesView] },
];

function initialsOf(fullName: string): string {
  const parts = fullName.trim().split(/\s+/);
  return parts
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? "")
    .join("");
}

export function MainLayout() {
  const { user, logout, hasPermission } = useAuth();

  const visibleNavItems = NAV_ITEMS.filter(
    (item) => !item.requiresAnyOf || item.requiresAnyOf.some((code) => hasPermission(code))
  );

  return (
    <div className="app-shell">
      <aside className="app-sidebar">
        <div className="app-sidebar__brand">
          <ITMLogo height={28} variant="light" />
        </div>

        <nav className="app-sidebar__nav">
          {visibleNavItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) => `app-sidebar__link${isActive ? " active" : ""}`}
            >
              <i className={`bi ${item.icon}`} aria-hidden="true" />
              <span>{item.label}</span>
            </NavLink>
          ))}
        </nav>
      </aside>

      <div className="app-main">
        <header className="app-topbar">
          <div className="app-topbar__user">
            <div className="app-topbar__avatar">{user ? initialsOf(user.fullName) : "?"}</div>
            <div>
              <div className="fw-semibold">{user?.fullName}</div>
              <div className="text-muted" style={{ fontSize: "0.78rem" }}>
                {user?.role}
              </div>
            </div>
          </div>
          <div className="d-flex align-items-center gap-2">
            <NotificationBell />
            <button type="button" className="btn btn-sm btn-outline-secondary" onClick={logout}>
              <i className="bi bi-box-arrow-right me-1" aria-hidden="true" />
              Sign Out
            </button>
          </div>
        </header>

        <main className="app-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
