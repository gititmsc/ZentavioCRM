import { NavLink, Outlet } from "react-router-dom";
import { ZentavioLogo } from "@/components/brand/ZentavioLogo";
import { Avatar } from "@/components/Avatar";
import { useAuth } from "@/context/AuthContext";
import "./MainLayout.css";

const NAV_ITEMS = [
  { to: "/dashboard", icon: "bi-grid-1x2-fill", label: "Dashboard" },
  { to: "/tenants", icon: "bi-buildings-fill", label: "Tenants" },
  { to: "/audit-log", icon: "bi-clock-history", label: "Audit Log" },
  { to: "/admins", icon: "bi-shield-lock-fill", label: "Platform Admins" },
];

export function MainLayout() {
  const { admin, logout } = useAuth();

  return (
    <div className="app-shell">
      <aside className="app-sidebar">
        <div className="app-sidebar__brand">
          <ZentavioLogo height={24} variant="light" />
          <span className="app-sidebar__brand-tag">
            <i className="bi bi-shield-fill-check" aria-hidden="true" />
            Platform Admin
          </span>
        </div>

        <div className="app-sidebar__section-label">Manage</div>
        <nav className="app-sidebar__nav">
          {NAV_ITEMS.map((item) => (
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

        <div className="app-sidebar__footer">ZentavioCRM Platform &middot; v1.0</div>
      </aside>

      <div className="app-main">
        <header className="app-topbar">
          <div className="app-topbar__admin-menu">
            {admin && <Avatar name={admin.fullName} size={32} />}
            <div className="text-start" style={{ lineHeight: 1.2 }}>
              <div className="fw-semibold" style={{ fontSize: "0.86rem" }}>
                {admin?.fullName}
              </div>
              <div className="text-muted" style={{ fontSize: "0.74rem" }}>
                {admin?.email}
              </div>
            </div>
          </div>
          <button type="button" className="btn btn-sm btn-outline-secondary" onClick={logout}>
            <i className="bi bi-box-arrow-right me-1" aria-hidden="true" />
            Sign Out
          </button>
        </header>

        <main className="app-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
