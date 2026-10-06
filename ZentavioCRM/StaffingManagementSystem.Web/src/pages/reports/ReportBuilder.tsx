import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { useAuth } from "@/context/AuthContext";
import { PermissionCodes } from "@/services/permissionCodes";
import {
  analyticsService,
  type AnalyticsCatalogEntity,
  type AnalyticsColumn,
  type AnalyticsEntity,
  type AnalyticsQueryRequest,
  type AnalyticsQueryResult,
  type AnalyticsRowsRequest,
  type AnalyticsRowsResult,
  type SavedAnalyticsItem,
} from "@/services/analyticsService";
import { downloadBlob } from "@/services/importTypes";
import { PageHeader } from "@/components/layout/PageHeader";
import { BarList, ColumnChart } from "@/components/charts/SimpleCharts";
import { FilterEditor, findEntity, GroupByControl, MetricControl, dateFields } from "@/components/analytics/QueryFields";
import { formatResultValue } from "@/components/analytics/WidgetView";
import { RANGE_PRESET_LABELS, resolveRange, type RangePreset } from "@/utils/dateRange";
import { defaultReportConfig, parseReportConfig, type ReportConfig } from "./reportConfig";

function formatCell(value: string | number | null, column: AnalyticsColumn): string {
  if (value === null || value === undefined || value === "") return "—";
  switch (column.kind) {
    case "Date":
      return new Date(String(value)).toLocaleDateString();
    case "Money":
      return Number(value).toLocaleString(undefined, { style: "currency", currency: "USD" });
    case "Number":
      return Number(value).toLocaleString(undefined, { maximumFractionDigits: 2 });
    default:
      return String(value);
  }
}

const csvCell = (value: string) => (/[",\r\n]/.test(value) ? `"${value.replace(/"/g, '""')}"` : value);

export default function ReportBuilder() {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const { hasPermission } = useAuth();
  const canManageShared = hasPermission(PermissionCodes.AnalyticsManageShared);

  const [catalog, setCatalog] = useState<AnalyticsCatalogEntity[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [current, setCurrent] = useState<SavedAnalyticsItem | null>(null);
  const [config, setConfig] = useState<ReportConfig | null>(null);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [isShared, setIsShared] = useState(false);

  const [rowsResult, setRowsResult] = useState<AnalyticsRowsResult | null>(null);
  const [summaryResult, setSummaryResult] = useState<AnalyticsQueryResult | null>(null);
  const [isRunning, setIsRunning] = useState(false);
  const [runError, setRunError] = useState<string | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);

  const def = config ? findEntity(catalog, config.entity) : undefined;

  const buildRequests = (cfg: ReportConfig): { rows: AnalyticsRowsRequest; summary: AnalyticsQueryRequest } => {
    const range = resolveRange(cfg.preset, cfg.customFrom, cfg.customTo);
    const base = {
      entity: cfg.entity,
      dateField: cfg.dateField,
      from: range.from || null,
      to: range.to || null,
      mineOnly: cfg.mineOnly,
      filters: cfg.filters,
    };
    return {
      rows: { ...base, columns: cfg.columns, sortBy: cfg.sortBy, sortDescending: cfg.sortDescending, limit: cfg.rowLimit },
      summary: {
        ...base,
        metric: cfg.metric,
        metricField: cfg.metricField,
        groupBy: cfg.groupBy,
        limit: cfg.groupLimit,
        sortDescending: cfg.sortDescending,
      },
    };
  };

  const run = async (cfg: ReportConfig) => {
    setIsRunning(true);
    setRunError(null);
    setNotice(null);
    const requests = buildRequests(cfg);

    if (cfg.mode === "rows") {
      const result = await analyticsService.rows(requests.rows);
      setIsRunning(false);
      if (!result.success || !result.data) {
        setRunError(result.message || "Unable to run this report.");
        return;
      }
      setRowsResult(result.data);
      setSummaryResult(null);
    } else {
      const result = await analyticsService.query(requests.summary);
      setIsRunning(false);
      if (!result.success || !result.data) {
        setRunError(result.message || "Unable to run this report.");
        return;
      }
      setSummaryResult(result.data);
      setRowsResult(null);
    }
  };

  useEffect(() => {
    (async () => {
      const catalogResult = await analyticsService.getCatalog();
      const loadedCatalog = catalogResult.success && catalogResult.data ? catalogResult.data : [];
      setCatalog(loadedCatalog);

      let item: SavedAnalyticsItem | null = null;
      if (id) {
        const itemResult = await analyticsService.getSaved(id);
        if (!itemResult.success || !itemResult.data) {
          setError(itemResult.message || "Report not found.");
          setIsLoading(false);
          return;
        }
        item = itemResult.data;
      }

      const firstEntity: AnalyticsEntity = loadedCatalog[0]?.entity ?? "Leads";
      const cfg = (item && parseReportConfig(item.configJson)) || defaultReportConfig(firstEntity);
      setCurrent(item);
      setConfig(cfg);
      setName(item?.name ?? "");
      setDescription(item?.description ?? "");
      setIsShared(item?.isShared ?? false);
      setIsLoading(false);
      if (item) run(cfg);
    })();
    // Load once per report id.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  if (isLoading) return <div className="text-muted">Loading...</div>;
  if (!config) {
    return (
      <div>
        <PageHeader title="Report" backTo="/reports" backLabel="Back to Reports" />
        <div className="alert alert-danger">{error ?? "Unable to open this report."}</div>
      </div>
    );
  }

  const patch = (changes: Partial<ReportConfig>) => setConfig((c) => (c ? { ...c, ...changes } : c));

  const changeEntity = (entity: AnalyticsEntity) => {
    setRowsResult(null);
    setSummaryResult(null);
    setConfig({ ...defaultReportConfig(entity), mode: config.mode, preset: config.preset, customFrom: config.customFrom, customTo: config.customTo, mineOnly: config.mineOnly });
  };

  const toggleColumn = (key: string) =>
    patch({ columns: config.columns.includes(key) ? config.columns.filter((c) => c !== key) : [...config.columns, key] });

  const exportRows = async (format: "csv" | "xlsx") => {
    setNotice(null);
    try {
      const blob = await analyticsService.exportRows(buildRequests(config).rows, format);
      downloadBlob(blob, `${(name || config.entity).toLowerCase().replace(/\s+/g, "-")}.${format}`);
    } catch {
      setNotice("Export failed — check the filters and try again.");
    }
  };

  const exportSummaryCsv = () => {
    if (!summaryResult) return;
    const lines = [["Group", "Value", "Records"].join(",")];
    for (const r of summaryResult.rows) lines.push([csvCell(r.label), String(r.value), String(r.count)].join(","));
    downloadBlob(new Blob(["﻿" + lines.join("\r\n")], { type: "text/csv" }), `${(name || "summary").toLowerCase().replace(/\s+/g, "-")}.csv`);
  };

  const isCopy = current !== null && !current.canEdit;

  const save = async () => {
    if (!name.trim()) {
      setNotice("Give the report a name before saving.");
      return;
    }
    setIsSaving(true);
    setNotice(null);
    const request = {
      kind: "Report" as const,
      name: name.trim(),
      description: description.trim() || null,
      isShared: isCopy ? false : isShared,
      configJson: JSON.stringify(config),
    };
    const result = current && !isCopy ? await analyticsService.updateSaved(current.id, request) : await analyticsService.createSaved(request);
    setIsSaving(false);
    if (!result.success || !result.data) {
      setNotice(result.message || "Unable to save this report.");
      return;
    }
    if (!current || isCopy) {
      navigate(`/reports/${result.data.id}`, { replace: true });
      return;
    }
    setCurrent(result.data);
    setNotice("Saved.");
  };

  return (
    <div>
      <PageHeader
        title={current ? current.name : "New Report"}
        subtitle={current && !current.isMine ? `Shared by ${current.ownerName}` : "Choose the data, narrow it down, then run, export or save."}
        backTo="/reports"
        backLabel="Back to Reports"
      />

      {error && <div className="alert alert-danger">{error}</div>}

      <div className="row g-3">
        <div className="col-lg-4">
          <div className="card shadow-sm border-0">
            <div className="card-body">
              <div className="mb-3">
                <label className="form-label small">Report on</label>
                <select className="form-select form-select-sm" value={config.entity} onChange={(e) => changeEntity(e.target.value as AnalyticsEntity)}>
                  {catalog.map((e) => (
                    <option key={e.entity} value={e.entity}>
                      {e.label}
                    </option>
                  ))}
                </select>
              </div>

              <div className="btn-group btn-group-sm w-100 mb-3" role="group" aria-label="Report type">
                <button type="button" className={`btn ${config.mode === "rows" ? "btn-primary" : "btn-outline-secondary"}`} onClick={() => patch({ mode: "rows" })}>
                  Detail rows
                </button>
                <button type="button" className={`btn ${config.mode === "summary" ? "btn-primary" : "btn-outline-secondary"}`} onClick={() => patch({ mode: "summary" })}>
                  Summary
                </button>
              </div>

              <div className="mb-3">
                <label className="form-label small">Date range</label>
                <div className="d-flex gap-2 mb-2">
                  <select
                    className="form-select form-select-sm"
                    value={config.dateField}
                    onChange={(e) => patch({ dateField: e.target.value })}
                    aria-label="Date field"
                  >
                    {dateFields(def).map((f) => (
                      <option key={f.key} value={f.key}>
                        {f.label}
                      </option>
                    ))}
                  </select>
                  <select
                    className="form-select form-select-sm"
                    value={config.preset}
                    onChange={(e) => patch({ preset: e.target.value as RangePreset })}
                    aria-label="Date range preset"
                  >
                    {(Object.keys(RANGE_PRESET_LABELS) as RangePreset[]).map((p) => (
                      <option key={p} value={p}>
                        {RANGE_PRESET_LABELS[p]}
                      </option>
                    ))}
                  </select>
                </div>
                {config.preset === "custom" && (
                  <div className="d-flex gap-2">
                    <input type="date" className="form-control form-control-sm" value={config.customFrom} max={config.customTo} onChange={(e) => patch({ customFrom: e.target.value })} aria-label="From date" />
                    <input type="date" className="form-control form-control-sm" value={config.customTo} min={config.customFrom} onChange={(e) => patch({ customTo: e.target.value })} aria-label="To date" />
                  </div>
                )}
              </div>

              <div className="form-check mb-3">
                <input className="form-check-input" type="checkbox" id="rptMine" checked={config.mineOnly} onChange={(e) => patch({ mineOnly: e.target.checked })} />
                <label className="form-check-label small" htmlFor="rptMine">
                  Only records assigned to me
                </label>
              </div>

              <div className="mb-3">
                <label className="form-label small">Filters</label>
                <FilterEditor def={def} filters={config.filters} onChange={(filters) => patch({ filters })} />
              </div>

              {config.mode === "rows" ? (
                <>
                  <div className="mb-3">
                    <label className="form-label small">Columns {config.columns.length === 0 && <span className="text-muted">(none picked = all)</span>}</label>
                    <div className="d-flex flex-wrap gap-1">
                      {def?.fields.map((f) => (
                        <button
                          key={f.key}
                          type="button"
                          className={`btn btn-sm ${config.columns.includes(f.key) ? "btn-primary" : "btn-outline-secondary"}`}
                          onClick={() => toggleColumn(f.key)}
                        >
                          {f.label}
                        </button>
                      ))}
                    </div>
                  </div>
                  <div className="d-flex gap-2 mb-3">
                    <div className="flex-grow-1">
                      <label className="form-label small">Sort by</label>
                      <select className="form-select form-select-sm" value={config.sortBy ?? "createdAt"} onChange={(e) => patch({ sortBy: e.target.value })}>
                        {def?.fields.map((f) => (
                          <option key={f.key} value={f.key}>
                            {f.label}
                          </option>
                        ))}
                      </select>
                    </div>
                    <div>
                      <label className="form-label small">Order</label>
                      <select className="form-select form-select-sm" value={config.sortDescending ? "desc" : "asc"} onChange={(e) => patch({ sortDescending: e.target.value === "desc" })}>
                        <option value="desc">High → low</option>
                        <option value="asc">Low → high</option>
                      </select>
                    </div>
                  </div>
                  <div className="mb-3">
                    <label className="form-label small">Max rows (up to 5,000)</label>
                    <input
                      type="number"
                      min={1}
                      max={5000}
                      className="form-control form-control-sm"
                      value={config.rowLimit}
                      onChange={(e) => patch({ rowLimit: Math.min(5000, Math.max(1, Number(e.target.value) || 1)) })}
                    />
                  </div>
                </>
              ) : (
                <>
                  <div className="mb-3">
                    <label className="form-label small">Group by</label>
                    <GroupByControl def={def} value={config.groupBy} onChange={(groupBy) => patch({ groupBy })} />
                  </div>
                  <div className="mb-3">
                    <label className="form-label small">Measure</label>
                    <MetricControl def={def} metric={config.metric} metricField={config.metricField} onChange={(metric, metricField) => patch({ metric, metricField })} />
                  </div>
                  <div className="mb-3">
                    <label className="form-label small">Max groups</label>
                    <input
                      type="number"
                      min={1}
                      max={100}
                      className="form-control form-control-sm"
                      value={config.groupLimit}
                      onChange={(e) => patch({ groupLimit: Math.min(100, Math.max(1, Number(e.target.value) || 1)) })}
                    />
                  </div>
                </>
              )}

              <button type="button" className="btn btn-primary w-100" onClick={() => run(config)} disabled={isRunning}>
                <i className="bi bi-play-fill me-1" aria-hidden="true" />
                {isRunning ? "Running..." : "Run report"}
              </button>
            </div>
          </div>

          <div className="card shadow-sm border-0 mt-3">
            <div className="card-body">
              <h6 className="mb-3">Save</h6>
              <input className="form-control form-control-sm mb-2" placeholder="Report name" value={name} onChange={(e) => setName(e.target.value)} />
              <input className="form-control form-control-sm mb-2" placeholder="Description (optional)" value={description} onChange={(e) => setDescription(e.target.value)} />
              {canManageShared && !isCopy && (
                <div className="form-check mb-2">
                  <input className="form-check-input" type="checkbox" id="rptShared" checked={isShared} onChange={(e) => setIsShared(e.target.checked)} />
                  <label className="form-check-label small" htmlFor="rptShared">
                    Share with the whole team
                  </label>
                </div>
              )}
              <button type="button" className="btn btn-outline-primary btn-sm" onClick={save} disabled={isSaving}>
                {isSaving ? "Saving..." : isCopy ? "Save as my copy" : current ? "Save changes" : "Save report"}
              </button>
              {notice && <div className="small mt-2 text-muted">{notice}</div>}
            </div>
          </div>
        </div>

        <div className="col-lg-8">
          <div className="card shadow-sm border-0">
            <div className="card-body">
              <div className="d-flex align-items-center mb-3">
                <h6 className="mb-0 me-auto">Results</h6>
                {rowsResult && (
                  <div className="d-flex gap-2">
                    <button type="button" className="btn btn-outline-secondary btn-sm" onClick={() => exportRows("csv")}>
                      <i className="bi bi-filetype-csv me-1" aria-hidden="true" />
                      CSV
                    </button>
                    <button type="button" className="btn btn-outline-secondary btn-sm" onClick={() => exportRows("xlsx")}>
                      <i className="bi bi-file-earmark-excel me-1" aria-hidden="true" />
                      Excel
                    </button>
                  </div>
                )}
                {summaryResult && (
                  <button type="button" className="btn btn-outline-secondary btn-sm" onClick={exportSummaryCsv}>
                    <i className="bi bi-filetype-csv me-1" aria-hidden="true" />
                    CSV
                  </button>
                )}
              </div>

              {runError && <div className="alert alert-danger">{runError}</div>}
              {!rowsResult && !summaryResult && !runError && <div className="text-muted small">Set up the report on the left and press "Run report".</div>}

              {rowsResult && (
                <>
                  <div className="text-muted small mb-2">
                    {rowsResult.totalCount.toLocaleString()} matching record{rowsResult.totalCount === 1 ? "" : "s"}
                    {rowsResult.truncated && ` — showing the first ${rowsResult.rows.length.toLocaleString()}`}
                  </div>
                  <div className="table-responsive" style={{ maxHeight: 560 }}>
                    <table className="table table-sm table-hover align-middle mb-0">
                      <thead className="table-light" style={{ position: "sticky", top: 0 }}>
                        <tr>
                          {rowsResult.columns.map((c) => (
                            <th key={c.key} className={c.kind === "Money" || c.kind === "Number" ? "text-end" : ""}>
                              {c.label}
                            </th>
                          ))}
                        </tr>
                      </thead>
                      <tbody>
                        {rowsResult.rows.map((row, i) => (
                          <tr key={i}>
                            {rowsResult.columns.map((c) => (
                              <td key={c.key} className={c.kind === "Money" || c.kind === "Number" ? "text-end" : ""}>
                                {formatCell(row[c.key] ?? null, c)}
                              </td>
                            ))}
                          </tr>
                        ))}
                        {rowsResult.rows.length === 0 && (
                          <tr>
                            <td colSpan={rowsResult.columns.length} className="text-muted">
                              No records match.
                            </td>
                          </tr>
                        )}
                      </tbody>
                    </table>
                  </div>
                </>
              )}

              {summaryResult && (
                <>
                  <div className="mb-3">
                    <div className="text-muted small">Overall</div>
                    <div style={{ fontSize: "1.8rem", fontWeight: 700 }}>{formatResultValue(summaryResult.valueKind, summaryResult.total)}</div>
                    <div className="text-muted small">{summaryResult.totalCount.toLocaleString()} matching records</div>
                  </div>

                  {summaryResult.groupKind !== "none" && (
                    <>
                      <div className="mb-3">
                        {summaryResult.groupKind === "time" ? (
                          <ColumnChart
                            points={summaryResult.rows.map((r) => ({ label: r.label, value: r.value }))}
                            formatValue={(n) => formatResultValue(summaryResult.valueKind, n, true)}
                          />
                        ) : (
                          <BarList items={summaryResult.rows.map((r) => ({ label: r.label, value: r.value, display: formatResultValue(summaryResult.valueKind, r.value) }))} />
                        )}
                      </div>
                      <div className="table-responsive">
                        <table className="table table-sm mb-0">
                          <thead>
                            <tr>
                              <th>Group</th>
                              <th className="text-end">Value</th>
                              <th className="text-end">Records</th>
                            </tr>
                          </thead>
                          <tbody>
                            {summaryResult.rows.map((r) => (
                              <tr key={r.key}>
                                <td>{r.label}</td>
                                <td className="text-end">{formatResultValue(summaryResult.valueKind, r.value)}</td>
                                <td className="text-end text-muted">{r.count.toLocaleString()}</td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                      {summaryResult.truncated && <div className="text-muted small mt-2">Showing the top {summaryResult.rows.length} groups.</div>}
                    </>
                  )}
                </>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
