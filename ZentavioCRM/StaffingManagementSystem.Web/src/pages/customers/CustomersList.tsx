import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { customerService, type CustomerListItem } from "@/services/customerService";
import { PermissionCodes } from "@/services/permissionCodes";
import { ImportExportBar } from "@/components/import-export/ImportExportBar";

export default function CustomersList() {
  const navigate = useNavigate();
  const { hasPermission } = useAuth();
  const canCreate = hasPermission(PermissionCodes.CustomersCreate);

  const [customers, setCustomers] = useState<CustomerListItem[]>([]);
  const [search, setSearch] = useState("");
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const pageSize = 20;

  const load = async (searchTerm: string, pageNumber: number) => {
    setIsLoading(true);
    const result = await customerService.search({ search: searchTerm || undefined, page: pageNumber, pageSize });
    setIsLoading(false);
    if (!result.success || !result.data) {
      setError(result.message || "Unable to load customers.");
      return;
    }
    setCustomers(result.data.items);
    setTotalCount(result.data.totalCount);
  };

  useEffect(() => {
    load(search, page);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setPage(1);
    load(search, 1);
  };

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  return (
    <div>
      <div className="d-flex justify-content-between align-items-center mb-4">
        <h1 className="h4 mb-0">Customers</h1>
        {canCreate && (
          <button type="button" className="btn btn-primary" onClick={() => navigate("/customers/new")}>
            <i className="bi bi-plus-lg me-1" aria-hidden="true" />
            New Customer
          </button>
        )}
      </div>

      <ImportExportBar
        entityLabel="Customers"
        onExport={customerService.exportCsv}
        onImport={customerService.importCsv}
        onImportComplete={() => load(search, page)}
      />

      <form className="d-flex mb-3" style={{ maxWidth: 360 }} onSubmit={handleSearchSubmit}>
        <input
          className="form-control me-2"
          placeholder="Search by name, number, email..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        <button type="submit" className="btn btn-outline-secondary">
          <i className="bi bi-search" aria-hidden="true" />
        </button>
      </form>

      {error && <div className="alert alert-danger">{error}</div>}

      <div className="card shadow-sm border-0">
        <div className="table-responsive">
          <table className="table table-hover align-middle mb-0">
            <thead className="table-light">
              <tr>
                <th>Number</th>
                <th>Name</th>
                <th>Type</th>
                <th>Industry</th>
                <th>Owner</th>
                <th>Status</th>
              </tr>
            </thead>
            <tbody>
              {isLoading && (
                <tr>
                  <td colSpan={6} className="text-center text-muted py-4">
                    Loading...
                  </td>
                </tr>
              )}
              {!isLoading && customers.length === 0 && (
                <tr>
                  <td colSpan={6} className="text-center text-muted py-4">
                    No customers found.
                  </td>
                </tr>
              )}
              {customers.map((customer) => (
                <tr
                  key={customer.id}
                  role="button"
                  onClick={() => navigate(`/customers/${customer.id}/edit`)}
                >
                  <td>{customer.customerNumber}</td>
                  <td>{customer.displayName}</td>
                  <td>{customer.type}</td>
                  <td>{customer.industry ?? <span className="text-muted">&mdash;</span>}</td>
                  <td>{customer.assignedToUserName ?? <span className="text-muted">Unassigned</span>}</td>
                  <td>
                    <span className={`badge ${customer.isActive ? "text-bg-success" : "text-bg-secondary"}`}>
                      {customer.isActive ? "Active" : "Inactive"}
                    </span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      {totalPages > 1 && (
        <div className="d-flex justify-content-between align-items-center mt-3">
          <span className="text-muted small">
            Page {page} of {totalPages} &middot; {totalCount} total
          </span>
          <div className="btn-group">
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
            >
              Previous
            </button>
            <button
              type="button"
              className="btn btn-outline-secondary btn-sm"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
            >
              Next
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
