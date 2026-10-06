import { useState } from "react";
import type { AnalyticsCatalogEntity, AnalyticsEntity } from "@/services/analyticsService";
import {
  FilterEditor,
  findEntity,
  GroupByControl,
  MetricControl,
  dateFields,
  splitGroupBy,
  joinGroupBy,
} from "./QueryFields";
import { WIDGET_TYPES, WIDGET_WIDTHS, type Widget, type WidgetType, type WidgetWidth } from "./widgets";

interface WidgetEditorProps {
  widget: Widget;
  catalog: AnalyticsCatalogEntity[];
  onSave: (widget: Widget) => void;
  onCancel: () => void;
}

/** Inline form for one widget's presentation (title/type/size) and the query that feeds it. */
export function WidgetEditor({ widget, catalog, onSave, onCancel }: WidgetEditorProps) {
  const [draft, setDraft] = useState<Widget>(widget);
  const [error, setError] = useState<string | null>(null);

  const def = findEntity(catalog, draft.query.entity);
  const setQuery = (patch: Partial<Widget["query"]>) => setDraft((d) => ({ ...d, query: { ...d.query, ...patch } }));

  const changeEntity = (entity: AnalyticsEntity) =>
    setDraft((d) => ({
      ...d,
      query: { ...d.query, entity, dateField: "createdAt", metric: "Count", metricField: null, groupBy: null, filters: [] },
    }));

  const changeType = (type: WidgetType) =>
    setDraft((d) => {
      let groupBy = d.query.groupBy ?? null;
      if (type === "kpi") {
        groupBy = null;
      } else if ((type === "line" || type === "column") && !groupBy) {
        const firstDate = dateFields(def)[0];
        groupBy = firstDate ? joinGroupBy(firstDate.key, "month") : null;
      }
      return { ...d, type, query: { ...d.query, groupBy } };
    });

  const save = () => {
    if (!draft.title.trim()) {
      setError("Give the widget a title.");
      return;
    }
    if (draft.type !== "kpi" && !draft.query.groupBy) {
      setError("This widget type needs a \"group by\" — pick what to break the numbers down by.");
      return;
    }
    if (draft.query.metric !== "Count" && !draft.query.metricField) {
      setError("Pick which field to sum or average.");
      return;
    }
    onSave({ ...draft, title: draft.title.trim() });
  };

  const isTime = dateFields(def).some((f) => f.key === splitGroupBy(draft.query.groupBy).field);

  return (
    <div className="card card-body bg-body-tertiary mb-3">
      <h6 className="mb-3">{widget.title === "New widget" ? "Add widget" : "Edit widget"}</h6>
      {error && <div className="alert alert-danger py-2">{error}</div>}

      <div className="row g-3">
        <div className="col-md-5">
          <label className="form-label small">Title</label>
          <input className="form-control form-control-sm" value={draft.title} onChange={(e) => setDraft({ ...draft, title: e.target.value })} />
        </div>
        <div className="col-md-4">
          <label className="form-label small">Display as</label>
          <select className="form-select form-select-sm" value={draft.type} onChange={(e) => changeType(e.target.value as WidgetType)}>
            {WIDGET_TYPES.map((t) => (
              <option key={t.value} value={t.value}>
                {t.label}
              </option>
            ))}
          </select>
        </div>
        <div className="col-md-3">
          <label className="form-label small">Size</label>
          <select className="form-select form-select-sm" value={draft.width} onChange={(e) => setDraft({ ...draft, width: e.target.value as WidgetWidth })}>
            {WIDGET_WIDTHS.map((w) => (
              <option key={w.value} value={w.value}>
                {w.label}
              </option>
            ))}
          </select>
        </div>

        <div className="col-md-4">
          <label className="form-label small">Data</label>
          <select
            className="form-select form-select-sm"
            value={draft.query.entity}
            onChange={(e) => changeEntity(e.target.value as AnalyticsEntity)}
          >
            {catalog.map((e) => (
              <option key={e.entity} value={e.entity}>
                {e.label}
              </option>
            ))}
          </select>
        </div>
        <div className="col-md-4">
          <label className="form-label small">Measure</label>
          <MetricControl
            def={def}
            metric={draft.query.metric}
            metricField={draft.query.metricField}
            onChange={(metric, metricField) => setQuery({ metric, metricField })}
          />
        </div>
        <div className="col-md-4">
          <label className="form-label small">Date used for the range</label>
          <select
            className="form-select form-select-sm"
            value={draft.query.dateField ?? "createdAt"}
            onChange={(e) => setQuery({ dateField: e.target.value })}
          >
            {dateFields(def).map((f) => (
              <option key={f.key} value={f.key}>
                {f.label}
              </option>
            ))}
          </select>
        </div>

        {draft.type !== "kpi" && (
          <div className="col-md-6">
            <label className="form-label small">Group by</label>
            <GroupByControl def={def} value={draft.query.groupBy} onChange={(groupBy) => setQuery({ groupBy })} allowNone={false} />
          </div>
        )}
        {draft.type !== "kpi" && !isTime && (
          <div className="col-md-3">
            <label className="form-label small">Max groups</label>
            <input
              type="number"
              min={1}
              max={100}
              className="form-control form-control-sm"
              value={draft.query.limit}
              onChange={(e) => setQuery({ limit: Math.min(100, Math.max(1, Number(e.target.value) || 1)) })}
            />
          </div>
        )}

        <div className="col-12">
          <label className="form-label small">Filters</label>
          <FilterEditor def={def} filters={draft.query.filters} onChange={(filters) => setQuery({ filters })} />
        </div>

        <div className="col-12">
          <div className="form-check">
            <input
              className="form-check-input"
              type="checkbox"
              id="ignoreRange"
              checked={!!draft.query.ignoreDateRange}
              onChange={(e) => setQuery({ ignoreDateRange: e.target.checked })}
            />
            <label className="form-check-label small" htmlFor="ignoreRange">
              Ignore the dashboard's date range (always all time) — good for "current" figures like open pipeline
            </label>
          </div>
        </div>
      </div>

      <div className="d-flex gap-2 mt-3">
        <button type="button" className="btn btn-primary btn-sm" onClick={save}>
          Apply
        </button>
        <button type="button" className="btn btn-outline-secondary btn-sm" onClick={onCancel}>
          Cancel
        </button>
      </div>
    </div>
  );
}
