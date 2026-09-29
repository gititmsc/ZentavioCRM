import { NavLink, Outlet } from "react-router-dom";
import { ZentavioLogo } from "@/components/brand/ZentavioLogo";
import { usePlatformAuth } from "@/context/PlatformAuthContext";
import "./AdminLayout.css";

const NAV_ITEMS = [
  { to: "/", icon: "bi-building", label: "Tenants" },
  { to: "/admins", icon: "bi-people-fill", label: "Platform Admins" },
  { to: "/audit-log", icon: "bi-journal-text", label: "Audit Log" },
];

export function AdminLayout() {
  const { admin, logout } = usePlatformAuth();

  return (
    <div className="app-shell">
      <aside className="app-sidebar">
        <div className="app-sidebar__brand">
          <ZentavioLogo height={28} variant="light" />
        </div>

        <nav className="app-sidebar__nav">
          {NAV_ITEMS.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === "/"}
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
            <div className="fw-semibold">{admin?.fullName}</div>
            <div className="text-muted" style={{ fontSize: "0.78rem" }}>
              Platform Admin
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
