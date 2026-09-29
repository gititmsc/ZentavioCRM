import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { tenantService, type Tenant } from "@/services/tenantService";
import { StatusBadge } from "@/components/StatusBadge";
import { Avatar } from "@/components/Avatar";
import { PageHeader } from "@/components/PageHeader";
import { ProvisionTenantModal } from "@/pages/tenants/ProvisionTenantModal";

export function TenantsList() {
  const navigate = useNavigate();
  const [tenants, setTenants] = useState<Tenant[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [search, setSearch] = useState("");
  const [showProvisionModal, setShowProvisionModal] = useState(false);

  const load = async () => {
    setLoading(true);
    const response = await tenantService.getAll();
    if (response.success && response.data) {
      setTenants(response.data);
      setError(null);
    } else {
      setError(response.message || "Could not load tenants.");
    }
    setLoading(false);
  };

  useEffect(() => {
    void load();
  }, []);

  const filtered = tenants.filter((t) => {
    const term = search.trim().toLowerCase();
    if (!term) return true;
    return t.name.toLowerCase().includes(term) || t.subdomain.toLowerCase().includes(term) || t.adminEmail.toLowerCase().includes(term);
  });

  return (
    <div>
      <PageHeader
        eyebrow="Accounts"
        icon="bi-buildings-fill"
        title="Tenants"
        subtitle={`${tenants.length} tenant${tenants.length === 1 ? "" : "s"} on the platform`}
        actions={
          <button type="button" className="btn btn-primary" onClick={() => setShowProvisionModal(true)}>
            <i className="bi bi-plus-lg me-1" aria-hidden="true" />
            New Tenant
          </button>
        }
      />

      <div className="search-input mb-3" style={{ maxWidth: 340 }}>
        <i className="bi bi-search" aria-hidden="true" />
        <input
          className="form-control"
          placeholder="Search by name, subdomain or admin email..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      {error && <div className="alert alert-danger">{error}</div>}

      <div className="app-card">
        <div className="table-responsive">
          <table className="table table-modern align-middle mb-0">
            <thead>
              <tr>
                <th>Company</th>
                <th>Subdomain</th>
                <th>Plan</th>
                <th>Status</th>
                <th>Created</th>
              </tr>
            </thead>
            <tbody>
              {loading ? (
                <tr>
                  <td colSpan={5} className="text-center text-muted py-5">
                    Loading tenants...
                  </td>
                </tr>
              ) : filtered.length === 0 ? (
                <tr>
                  <td colSpan={5} className="p-0">
                    <div className="empty-state">
                      <div className="empty-state__icon">
                        <i className="bi bi-buildings" aria-hidden="true" />
                      </div>
                      <div className="empty-state__title">{search ? "No tenants match your search" : "No tenants yet"}</div>
                      {!search && <div className="small">Provision your first tenant to get started.</div>}
                    </div>
                  </td>
                </tr>
              ) : (
                filtered.map((tenant) => (
                  <tr key={tenant.id} className="is-clickable" onClick={() => navigate(`/tenants/${tenant.id}`)}>
                    <td>
                      <div className="d-flex align-items-center gap-3">
                        <Avatar name={tenant.name} size={36} />
                        <div>
                          <div className="fw-semibold">{tenant.name}</div>
                          <div className="text-muted" style={{ fontSize: "0.78rem" }}>
                            {tenant.adminEmail}
                          </div>
                        </div>
                      </div>
                    </td>
                    <td className="text-muted">{tenant.subdomain}</td>
                    <td>
                      <span className="plan-badge">{tenant.planTier}</span>
                    </td>
                    <td>
                      <StatusBadge status={tenant.status} />
                    </td>
                    <td className="text-muted small">{new Date(tenant.createdAtUtc).toLocaleDateString()}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </div>

      {showProvisionModal && (
        <ProvisionTenantModal
          onClose={() => setShowProvisionModal(false)}
          onProvisioned={() => {
            setShowProvisionModal(false);
            void load();
          }}
        />
      )}
    </div>
  );
}
