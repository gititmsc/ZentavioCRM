/**
 * Small dependency-free SVG/CSS charts for the Dashboard. Deliberately minimal — if the Custom
 * Dashboard phase needs richer interactions this is the single place to swap for a chart library.
 */

export interface BarListItem {
  label: string;
  value: number;
  /** Optional right-aligned text; defaults to the value. */
  display?: string;
  /** Optional muted secondary text under the label. */
  sublabel?: string;
}

/** Horizontal bars, widest = 100%. Used for the funnel, lead sources, and leaderboards. */
export function BarList({ items, emptyText = "No data in this range." }: { items: BarListItem[]; emptyText?: string }) {
  if (items.length === 0 || items.every((i) => i.value === 0)) {
    return <div className="text-muted small py-2">{emptyText}</div>;
  }
  const max = Math.max(...items.map((i) => i.value), 1);
  return (
    <div className="d-flex flex-column gap-2">
      {items.map((item) => (
        <div key={item.label}>
          <div className="d-flex justify-content-between small">
            <span className="text-truncate me-2">
              {item.label}
              {item.sublabel && <span className="text-muted ms-2">{item.sublabel}</span>}
            </span>
            <span className="fw-semibold text-nowrap">{item.display ?? item.value.toLocaleString()}</span>
          </div>
          <div className="progress" style={{ height: 8 }} role="presentation">
            <div className="progress-bar" style={{ width: `${(item.value / max) * 100}%`, background: "var(--itm-accent)" }} />
          </div>
        </div>
      ))}
    </div>
  );
}

export interface SeriesPoint {
  label: string;
  value: number;
}

export interface Series {
  name: string;
  color: string;
  points: SeriesPoint[];
}

/** Multi-series line chart over a shared set of x labels. */
export function LineChart({ series, height = 180, formatValue }: { series: Series[]; height?: number; formatValue?: (n: number) => string }) {
  const labels = series[0]?.points.map((p) => p.label) ?? [];
  const allValues = series.flatMap((s) => s.points.map((p) => p.value));
  if (labels.length === 0 || allValues.every((v) => v === 0)) {
    return <div className="text-muted small py-2">No data in this range.</div>;
  }

  const width = 600;
  const pad = { top: 12, right: 12, bottom: 24, left: 36 };
  const innerW = width - pad.left - pad.right;
  const innerH = height - pad.top - pad.bottom;
  const max = Math.max(...allValues, 1);
  const x = (i: number) => pad.left + (labels.length === 1 ? innerW / 2 : (i / (labels.length - 1)) * innerW);
  const y = (v: number) => pad.top + innerH - (v / max) * innerH;
  const fmt = formatValue ?? ((n: number) => n.toLocaleString());

  // Show at most ~6 x labels so they don't collide.
  const labelStep = Math.max(1, Math.ceil(labels.length / 6));

  return (
    <div>
      <svg viewBox={`0 0 ${width} ${height}`} width="100%" role="img" aria-label="Trend chart">
        {[0, 0.5, 1].map((t) => (
          <g key={t}>
            <line x1={pad.left} x2={width - pad.right} y1={y(max * t)} y2={y(max * t)} stroke="#e5e7eb" strokeWidth={1} />
            <text x={pad.left - 6} y={y(max * t) + 4} textAnchor="end" fontSize={10} fill="#6b7280">
              {fmt(Math.round(max * t))}
            </text>
          </g>
        ))}
        {labels.map((label, i) =>
          i % labelStep === 0 ? (
            <text key={i} x={x(i)} y={height - 6} textAnchor="middle" fontSize={10} fill="#6b7280">
              {label}
            </text>
          ) : null
        )}
        {series.map((s) => (
          <g key={s.name}>
            <polyline
              fill="none"
              stroke={s.color}
              strokeWidth={2}
              points={s.points.map((p, i) => `${x(i)},${y(p.value)}`).join(" ")}
            />
            {s.points.map((p, i) => (
              <circle key={i} cx={x(i)} cy={y(p.value)} r={3} fill={s.color}>
                <title>{`${s.name} — ${p.label}: ${fmt(p.value)}`}</title>
              </circle>
            ))}
          </g>
        ))}
      </svg>
      <div className="d-flex gap-3 small mt-1">
        {series.map((s) => (
          <span key={s.name} className="d-inline-flex align-items-center gap-1">
            <span style={{ width: 10, height: 10, borderRadius: 2, background: s.color, display: "inline-block" }} />
            {s.name}
          </span>
        ))}
      </div>
    </div>
  );
}

/** Vertical columns for a single series (e.g. revenue per bucket). */
export function ColumnChart({ points, height = 180, color = "var(--itm-accent)", formatValue }: {
  points: SeriesPoint[];
  height?: number;
  color?: string;
  formatValue?: (n: number) => string;
}) {
  if (points.length === 0 || points.every((p) => p.value === 0)) {
    return <div className="text-muted small py-2">No data in this range.</div>;
  }
  const width = 600;
  const pad = { top: 12, right: 12, bottom: 24, left: 48 };
  const innerW = width - pad.left - pad.right;
  const innerH = height - pad.top - pad.bottom;
  const max = Math.max(...points.map((p) => p.value), 1);
  const slot = innerW / points.length;
  const barW = Math.min(36, slot * 0.7);
  const fmt = formatValue ?? ((n: number) => n.toLocaleString());
  const labelStep = Math.max(1, Math.ceil(points.length / 6));

  return (
    <svg viewBox={`0 0 ${width} ${height}`} width="100%" role="img" aria-label="Column chart">
      {[0, 0.5, 1].map((t) => {
        const yy = pad.top + innerH - t * innerH;
        return (
          <g key={t}>
            <line x1={pad.left} x2={width - pad.right} y1={yy} y2={yy} stroke="#e5e7eb" strokeWidth={1} />
            <text x={pad.left - 6} y={yy + 4} textAnchor="end" fontSize={10} fill="#6b7280">
              {fmt(Math.round(max * t))}
            </text>
          </g>
        );
      })}
      {points.map((p, i) => {
        const h = (p.value / max) * innerH;
        const cx = pad.left + slot * i + slot / 2;
        return (
          <g key={i}>
            <rect x={cx - barW / 2} y={pad.top + innerH - h} width={barW} height={h} rx={3} fill={color}>
              <title>{`${p.label}: ${fmt(p.value)}`}</title>
            </rect>
            {i % labelStep === 0 && (
              <text x={cx} y={height - 6} textAnchor="middle" fontSize={10} fill="#6b7280">
                {p.label}
              </text>
            )}
          </g>
        );
      })}
    </svg>
  );
}

const DONUT_COLORS = ["#2563eb", "#16a34a", "#f59e0b", "#dc2626", "#7c3aed", "#0891b2", "#db2777", "#65a30d"];

/** Donut with a legend. Slices beyond the palette wrap colors, so keep the item count modest (the engine caps groups anyway). */
export function DonutChart({ items, formatValue }: { items: SeriesPoint[]; formatValue?: (n: number) => string }) {
  const positive = items.filter((i) => i.value > 0);
  const total = positive.reduce((sum, i) => sum + i.value, 0);
  if (total === 0) {
    return <div className="text-muted small py-2">No data in this range.</div>;
  }
  const fmt = formatValue ?? ((n: number) => n.toLocaleString());

  // Circle with circumference 100 so each slice's dash length is simply its percentage.
  const radius = 100 / (2 * Math.PI);
  let offset = 25; // start at 12 o'clock
  return (
    <div className="d-flex flex-wrap align-items-center gap-3">
      <svg viewBox="0 0 42 42" width={150} height={150} role="img" aria-label="Distribution chart">
        <circle cx={21} cy={21} r={radius} fill="none" stroke="#f1f5f9" strokeWidth={6} />
        {positive.map((item, i) => {
          const pct = (item.value / total) * 100;
          const slice = (
            <circle
              key={item.label}
              cx={21}
              cy={21}
              r={radius}
              fill="none"
              stroke={DONUT_COLORS[i % DONUT_COLORS.length]}
              strokeWidth={6}
              strokeDasharray={`${pct} ${100 - pct}`}
              strokeDashoffset={offset}
            >
              <title>{`${item.label}: ${fmt(item.value)} (${pct.toFixed(1)}%)`}</title>
            </circle>
          );
          offset -= pct;
          return slice;
        })}
      </svg>
      <ul className="list-unstyled small mb-0 flex-grow-1">
        {positive.map((item, i) => (
          <li key={item.label} className="d-flex align-items-center gap-2">
            <span
              style={{ width: 10, height: 10, borderRadius: 2, background: DONUT_COLORS[i % DONUT_COLORS.length], display: "inline-block" }}
            />
            <span className="text-truncate">{item.label}</span>
            <span className="ms-auto fw-semibold text-nowrap">
              {fmt(item.value)} <span className="text-muted fw-normal">({((item.value / total) * 100).toFixed(0)}%)</span>
            </span>
          </li>
        ))}
      </ul>
    </div>
  );
}
