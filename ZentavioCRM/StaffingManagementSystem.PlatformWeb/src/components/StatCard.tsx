interface StatCardProps {
  icon: string;
  label: string;
  value: string | number;
  tint: "accent" | "success" | "warning" | "danger" | "muted";
}

const TINTS: Record<StatCardProps["tint"], { bg: string; color: string }> = {
  accent: { bg: "color-mix(in srgb, var(--itm-accent) 14%, white)", color: "var(--itm-accent)" },
  success: { bg: "color-mix(in srgb, var(--itm-success) 14%, white)", color: "var(--itm-success)" },
  warning: { bg: "color-mix(in srgb, var(--itm-warning) 14%, white)", color: "var(--itm-warning)" },
  danger: { bg: "color-mix(in srgb, var(--itm-danger) 14%, white)", color: "var(--itm-danger)" },
  muted: { bg: "color-mix(in srgb, var(--itm-muted) 14%, white)", color: "var(--itm-muted)" },
};

/** Single dashboard summary tile — icon chip + big number + label. */
export function StatCard({ icon, label, value, tint }: StatCardProps) {
  const colors = TINTS[tint];
  return (
    <div className="stat-card">
      <div className="stat-card__icon" style={{ background: colors.bg, color: colors.color }}>
        <i className={`bi ${icon}`} aria-hidden="true" />
      </div>
      <div>
        <div className="stat-card__value">{value}</div>
        <div className="stat-card__label">{label}</div>
      </div>
    </div>
  );
}
