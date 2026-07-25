import { useAuth } from "@/context/AuthContext";

/** Landing page reached after a successful login. */
export default function Dashboard() {
  const { user } = useAuth();

  return (
    <div>
      <h1 className="h4 mb-1">Welcome{user ? `, ${user.fullName}` : ""}</h1>
      <p className="text-muted">Here's what's happening across your CRM today.</p>

      <div className="row g-3 mt-2">
        <div className="col-md-3">
          <div className="card shadow-sm border-0">
            <div className="card-body">
              <div className="text-muted small">Open Leads</div>
              <div className="h3 mb-0">—</div>
            </div>
          </div>
        </div>
        <div className="col-md-3">
          <div className="card shadow-sm border-0">
            <div className="card-body">
              <div className="text-muted small">Active Customers</div>
              <div className="h3 mb-0">—</div>
            </div>
          </div>
        </div>
        <div className="col-md-3">
          <div className="card shadow-sm border-0">
            <div className="card-body">
              <div className="text-muted small">Converted This Month</div>
              <div className="h3 mb-0">—</div>
            </div>
          </div>
        </div>
        <div className="col-md-3">
          <div className="card shadow-sm border-0">
            <div className="card-body">
              <div className="text-muted small">Pipeline Value</div>
              <div className="h3 mb-0">—</div>
            </div>
          </div>
        </div>
      </div>

      <p className="text-muted small mt-4">
        Dashboard widgets (lead funnel, pipeline value, conversion rate) come next — use the Leads and Customers
        sections in the meantime.
      </p>
    </div>
  );
}
