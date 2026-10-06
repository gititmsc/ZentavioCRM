import { useEffect, useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { PermissionCodes } from "@/services/permissionCodes";
import {
  analyticsService,
  type AnalyticsCatalogEntity,
  type SavedAnalyticsItem,
} from "@/services/analyticsService";
import { PageHeader } from "@/components/layout/PageHeader";
import { WidgetView } from "@/components/analytics/WidgetView";
import { WidgetEditor } from "@/components/analytics/WidgetEditor";
import {
  blankWidget,
  parseDashboardConfig,
  templateWidgets,
  WIDGET_WIDTHS,
  type DashboardConfig,
  type Widget,
} from "@/components/analytics/widgets";
import { addDays, PRESET_LABELS, resolveRange, toDateInput, type BasePreset } from "@/utils/dateRange";

/** Saved-dashboard viewer and builder: pick a dashboard, change its date range / scope, or edit its widgets. */
export default function CustomDashboards() {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const { hasPermission } = useAuth();
  const canManageShared = hasPermission(PermissionCodes.AnalyticsManageShared);

  const [items, setItems] = useState<SavedAnalyticsItem[]>([]);
  const [catalog, setCatalog] = useState<AnalyticsCatalogEntity[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // The dashboard being viewed (null while creating a brand-new, unsaved one).
  const [current, setCurrent] = useState<SavedAnalyticsItem | null>(null);
  const [widgets, setWidgets] = useState<Widget[]>([]);
  const [isEditing, setIsEditing] = useState(false);
  const [draftName, setDraftName] = useState("");
  const [draftDescription, setDraftDescription] = useState("");
  const [draftShared, setDraftShared] = useState(false);
  const [editingWidget, setEditingWidget] = useState<Widget | null>(null);
  const [isSaving, setIsSaving] = useState(false);

  const [preset, setPreset] = useState<BasePreset>("last30");
  const [customFrom, setCustomFrom] = useState(() => toDateInput(addDays(new Date(), -30)));
  const [customTo, setCustomTo] = useState(() => toDateInput(new Date()));
  const [mineOnly, setMineOnly] = useState(false);
  const range = useMemo(() => resolveRange(preset, customFrom, customTo), [preset, customFrom, customTo]);

  const select = (item: SavedAnalyticsItem | null) => {
    setCurrent(item);
    setWidgets(item ? parseDashboardConfig(item.configJson).widgets : []);
    setIsEditing(false);
    setEditingWidget(null);
  };

  const loadList = async (preferId?: string) => {
    const [listResult, catalogResult] = await Promise.all([analyticsService.listSaved("Dashboard"), analyticsService.getCatalog()]);
    setIsLoading(false);
    if (!listResult.success || !listResult.data) {
      setError(listResult.message || "Unable to load dashboards.");
      return;
    }
    setError(null);
    setItems(listResult.data);
    if (catalogResult.success && catalogResult.data) setCatalog(catalogResult.data);

    const wanted = preferId ?? id;
    const pick = listResult.data.find((i) => i.id === wanted) ?? listResult.data[0] ?? null;
    select(pick);
    if (pick && pick.id !== id) navigate(`/dashboards/${pick.id}`, { replace: true });
  };

  useEffect(() => {
    loadList();
    // Initial load only; later selection changes go through chooseDashboard.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const chooseDashboard = (itemId: string) => {
    const item = items.find((i) => i.id === itemId) ?? null;
    select(item);
    if (item) navigate(`/dashboards/${item.id}`);
  };

  const startNew = (useTemplate: boolean) => {
    setCurrent(null);
    setWidgets(useTemplate ? templateWidgets() : []);
    setDraftName("My dashboard");
    setDraftDescription("");
    setDraftShared(false);
    setEditingWidget(null);
    setIsEditing(true);
  };

  const startEdit = () => {
    if (!current) return;
    setDraftName(current.name);
    setDraftDescription(current.description ?? "");
    setDraftShared(current.isShared);
    setIsEditing(true);
  };

  const cancelEdit = () => {
    if (current) {
      select(current);
    } else {
      select(items[0] ?? null);
    }
  };

  const buildConfigJson = () => JSON.stringify({ version: 1, widgets } satisfies DashboardConfig);

  const save = async () => {
    if (!draftName.trim()) {
      setError("Give the dashboard a name.");
      return;
    }
    setIsSaving(true);
    setError(null);
    const request = {
      kind: "Dashboard" as const,
      name: draftName.trim(),
      description: draftDescription.trim() || null,
      isShared: draftShared,
      configJson: buildConfigJson(),
    };
    const result = current ? await analyticsService.updateSaved(current.id, request) : await analyticsService.createSaved(request);
    setIsSaving(false);
    if (!result.success || !result.data) {
      setError(result.message || "Unable to save this dashboard.");
      return;
    }
    await loadList(result.data.id);
  };

  const duplicate = async () => {
    if (!current) return;
    const result = await analyticsService.createSaved({
      kind: "Dashboard",
      name: `Copy of ${current.name}`,
      description: current.description,
      isShared: false,
      configJson: current.configJson,
    });
    if (!result.success || !result.data) {
      setError(result.message || "Unable to duplicate this dashboard.");
      return;
    }
    await loadList(result.data.id);
  };

  const remove = async () => {
    if (!current) return;
    if (!window.confirm(`Delete the dashboard "${current.name}"?`)) return;
    const result = await analyticsService.removeSaved(current.id);
    if (!result.success) {
      setError(result.message || "Unable to delete this dashboard.");
      return;
    }
    navigate("/dashboards", { replace: true });
    await loadList(undefined);
  };

  const applyWidget = (widget: Widget) => {
    setWidgets((list) => (list.some((w) => w.id === widget.id) ? list.map((w) => (w.id === widget.id ? widget : w)) : [...list, widget]));
    setEditingWidget(null);
  };

  const move = (index: number, delta: -1 | 1) =>
    setWidgets((list) => {
      const target = index + delta;
      if (target < 0 || target >= list.length) return list;
      const copy = [...list];
      [copy[index], copy[target]] = [copy[target], copy[index]];
      return copy;
    });

  if (isLoading) {
    return <div className="text-muted">Loading...</div>;
  }

  const hasDashboard = current !== null || isEditing;
  const canEditCurrent = current?.canEdit ?? true;

  return (
    <div>
      <PageHeader
        title="Custom Dashboards"
        subtitle="Build your own views from leads, opportunities, customers, quotations and orders."
        backTo="/dashboard"
        backLabel="Back to Dashboard"
        actions={
          !isEditing && (
            <div className="d-flex gap-2">
              <button type="button" className="btn btn-outline-secondary" onClick={() => startNew(false)}>
                <i className="bi bi-plus-lg me-1" aria-hidden="true" />
                New dashboard
              </button>
              <button type="button" className="btn btn-primary" onClick={() => startNew(true)}>
                <i className="bi bi-magic me-1" aria-hidden="true" />
                From template
              </button>
            </div>
          )
        }
      />

      {error && <div className="alert alert-danger">{error}</div>}

      {!hasDashboard && (
        <div className="card shadow-sm border-0 p-4 text-center">
          <p className="mb-2 fw-semibold">You don't have any dashboards yet.</p>
          <p className="text-muted small mb-3">Start from a ready-made sales overview, or build one widget by widget.</p>
          <div className="d-flex justify-content-center gap-2">
            <button type="button" className="btn btn-primary" onClick={() => startNew(true)}>
              Start from template
            </button>
            <button type="button" className="btn btn-outline-secondary" onClick={() => startNew(false)}>
              Blank dashboard
            </button>
          </div>
        </div>
      )}

      {hasDashboard && (
        <>
          <div className="card shadow-sm border-0 p-3 mb-3">
            {isEditing ? (
              <div className="row g-2 align-items-end">
                <div className="col-md-4">
                  <label className="form-label small mb-1">Name</label>
                  <input className="form-control form-control-sm" value={draftName} onChange={(e) => setDraftName(e.target.value)} />
                </div>
                <div className="col-md-4">
                  <label className="form-label small mb-1">Description</label>
                  <input className="form-control form-control-sm" value={draftDescription} onChange={(e) => setDraftDescription(e.target.value)} />
                </div>
                <div className="col-md-4 d-flex align-items-center gap-3">
                  {canManageShared && (
                    <div className="form-check">
                      <input
                        className="form-check-input"
                        type="checkbox"
                        id="dashShared"
                        checked={draftShared}
                        onChange={(e) => setDraftShared(e.target.checked)}
                      />
                      <label className="form-check-label small" htmlFor="dashShared">
                        Share with the whole team
                      </label>
                    </div>
                  )}
                  <div className="ms-auto d-flex gap-2">
                    <button type="button" className="btn btn-primary btn-sm" onClick={save} disabled={isSaving}>
                      {isSaving ? "Saving..." : "Save"}
                    </button>
                    <button type="button" className="btn btn-outline-secondary btn-sm" onClick={cancelEdit} disabled={isSaving}>
                      Cancel
                    </button>
                  </div>
                </div>
              </div>
            ) : (
              <div className="d-flex flex-wrap gap-2 align-items-center">
                <select
                  className="form-select form-select-sm"
                  style={{ width: "auto", minWidth: 200 }}
                  value={current?.id ?? ""}
                  onChange={(e) => chooseDashboard(e.target.value)}
                  aria-label="Dashboard"
                >
                  {items.map((i) => (
                    <option key={i.id} value={i.id}>
                      {i.name}
                      {i.isShared && !i.isMine ? ` (shared by ${i.ownerName})` : i.isShared ? " (shared)" : ""}
                    </option>
                  ))}
                </select>

                <div className="btn-group btn-group-sm" role="group" aria-label="Record scope">
                  <button type="button" className={`btn ${mineOnly ? "btn-primary" : "btn-outline-secondary"}`} onClick={() => setMineOnly(true)}>
                    My records
                  </button>
                  <button type="button" className={`btn ${!mineOnly ? "btn-primary" : "btn-outline-secondary"}`} onClick={() => setMineOnly(false)}>
                    Team / all I can see
                  </button>
                </div>

                <select
                  className="form-select form-select-sm"
                  style={{ width: "auto" }}
                  value={preset}
                  onChange={(e) => setPreset(e.target.value as BasePreset)}
                  aria-label="Date range"
                >
                  {(Object.keys(PRESET_LABELS) as BasePreset[]).map((p) => (
                    <option key={p} value={p}>
                      {PRESET_LABELS[p]}
                    </option>
                  ))}
                </select>
                {preset === "custom" && (
                  <>
                    <input type="date" className="form-control form-control-sm" style={{ width: "auto" }} value={customFrom} max={customTo} onChange={(e) => setCustomFrom(e.target.value)} aria-label="From date" />
                    <input type="date" className="form-control form-control-sm" style={{ width: "auto" }} value={customTo} min={customFrom} onChange={(e) => setCustomTo(e.target.value)} aria-label="To date" />
                  </>
                )}

                <div className="ms-auto d-flex gap-2">
                  {current && canEditCurrent && (
                    <button type="button" className="btn btn-outline-secondary btn-sm" onClick={startEdit}>
                      <i className="bi bi-pencil me-1" aria-hidden="true" />
                      Edit
                    </button>
                  )}
                  {current && (
                    <button type="button" className="btn btn-outline-secondary btn-sm" onClick={duplicate}>
                      <i className="bi bi-files me-1" aria-hidden="true" />
                      Duplicate
                    </button>
                  )}
                  {current && canEditCurrent && (
                    <button type="button" className="btn btn-outline-danger btn-sm" onClick={remove}>
                      <i className="bi bi-trash me-1" aria-hidden="true" />
                      Delete
                    </button>
                  )}
                </div>
              </div>
            )}
            {!isEditing && current?.description && <div className="text-muted small mt-2">{current.description}</div>}
          </div>

          {isEditing && !editingWidget && (
            <div className="mb-3">
              <button type="button" className="btn btn-outline-primary btn-sm" onClick={() => setEditingWidget(blankWidget(catalog[0]?.entity ?? "Leads"))} disabled={catalog.length === 0}>
                <i className="bi bi-plus-lg me-1" aria-hidden="true" />
                Add widget
              </button>
              {catalog.length === 0 && <span className="text-muted small ms-2">You don't have view access to any data yet.</span>}
            </div>
          )}

          {editingWidget && <WidgetEditor key={editingWidget.id} widget={editingWidget} catalog={catalog} onSave={applyWidget} onCancel={() => setEditingWidget(null)} />}

          {widgets.length === 0 && (
            <div className="text-muted small">{isEditing ? "No widgets yet — add one above." : "This dashboard has no widgets."}</div>
          )}

          <div className="row g-3">
            {widgets.map((widget, index) => (
              <div key={widget.id} className={WIDGET_WIDTHS.find((w) => w.value === widget.width)?.col ?? "col-lg-6"}>
                <div className="card shadow-sm border-0 h-100">
                  <div className="card-body">
                    <div className="d-flex align-items-start mb-2">
                      <h6 className="mb-0 me-auto">{widget.title}</h6>
                      {isEditing && (
                        <div className="btn-group btn-group-sm">
                          <button type="button" className="btn btn-outline-secondary" onClick={() => move(index, -1)} disabled={index === 0} aria-label="Move up">
                            <i className="bi bi-arrow-up" aria-hidden="true" />
                          </button>
                          <button type="button" className="btn btn-outline-secondary" onClick={() => move(index, 1)} disabled={index === widgets.length - 1} aria-label="Move down">
                            <i className="bi bi-arrow-down" aria-hidden="true" />
                          </button>
                          <button type="button" className="btn btn-outline-secondary" onClick={() => setEditingWidget(widget)} aria-label="Edit widget">
                            <i className="bi bi-pencil" aria-hidden="true" />
                          </button>
                          <button
                            type="button"
                            className="btn btn-outline-danger"
                            onClick={() => setWidgets((list) => list.filter((w) => w.id !== widget.id))}
                            aria-label="Remove widget"
                          >
                            <i className="bi bi-x-lg" aria-hidden="true" />
                          </button>
                        </div>
                      )}
                    </div>
                    <WidgetView widget={widget} from={range.from} to={range.to} mineOnly={mineOnly} />
                  </div>
                </div>
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  );
}
