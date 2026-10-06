import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { analyticsService, type SavedAnalyticsItem } from "@/services/analyticsService";
import { PageHeader } from "@/components/layout/PageHeader";
import { parseReportConfig } from "./reportConfig";

export default function ReportsList() {
  const navigate = useNavigate();
  const [items, setItems] = useState<SavedAnalyticsItem[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    const result = await analyticsService.listSaved("Report");
    setIsLoading(false);
    if (!result.success || !result.data) {
      setError(result.message || "Unable to load reports.");
      return;
    }
    setError(null);
    setItems(result.data);
  };

  useEffect(() => {
    load();
  }, []);

  const remove = async (item: SavedAnalyticsItem) => {
    if (!window.confirm(`Delete the report "${item.name}"?`)) return;
    const result = await analyticsService.removeSaved(item.id);
    if (!result.success) {
      setError(result.message || "Unable to delete this report.");
      return;
    }
    load();
  };

  return (
    <div>
      <PageHeader
        title="Reports"
        subtitle="Build, save and export reports across leads, opportunities, customers, quotations and orders."
        backTo="/dashboard"
        backLabel="Back to Dashboard"
        actions={
          <button type="button" className="btn btn-primary" onClick={() => navigate("/reports/new")}>
            <i className="bi bi-plus-lg me-1" aria-hidden="true" />
            New Report
          </button>
        }
      />

      {error && <div className="alert alert-danger">{error}</div>}

      <div className="card shadow-sm border-0">
        <div className="table-responsive">
          <table className="table table-hover align-middle mb-0">
            <thead>
              <tr>
                <th>Name</th>
                <th>Data</th>
                <th>Type</th>
                <th>Owner</th>
                <th>Visibility</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {isLoading && (
                <tr>
                  <td colSpan={6} className="text-muted">
                    Loading...
                  </td>
                </tr>
              )}
              {!isLoading && items.length === 0 && (
                <tr>
                  <td colSpan={6} className="text-muted">
                    No saved reports yet — build one with "New Report".
                  </td>
                </tr>
              )}
              {items.map((item) => {
                const config = parseReportConfig(item.configJson);
                return (
                  <tr key={item.id} role="button" onClick={() => navigate(`/reports/${item.id}`)}>
                    <td>
                      <div className="fw-semibold">{item.name}</div>
                      {item.description && <div className="text-muted small">{item.description}</div>}
                    </td>
                    <td>{config?.entity ?? "—"}</td>
                    <td>{config ? (config.mode === "rows" ? "Detail rows" : "Summary") : "—"}</td>
                    <td>{item.isMine ? "You" : item.ownerName}</td>
                    <td>
                      <span className={`badge ${item.isShared ? "text-bg-info" : "text-bg-secondary"}`}>{item.isShared ? "Shared" : "Private"}</span>
                    </td>
                    <td className="text-end">
                      {item.canEdit && (
                        <button
                          type="button"
                          className="btn btn-sm btn-outline-danger"
                          onClick={(e) => {
                            e.stopPropagation();
                            remove(item);
                          }}
                        >
                          Delete
                        </button>
                      )}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
