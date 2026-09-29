import type { ReactNode } from "react";

interface PageHeaderProps {
  eyebrow?: string;
  icon?: string;
  title: string;
  subtitle?: string;
  actions?: ReactNode;
}

/** Consistent title block used at the top of every page — icon + title, optional eyebrow label
 * and subtitle, and a slot for page-level actions (buttons) aligned to the right. */
export function PageHeader({ eyebrow, icon, title, subtitle, actions }: PageHeaderProps) {
  return (
    <div className="page-header">
      <div>
        {eyebrow && <div className="page-header__eyebrow">{eyebrow}</div>}
        <h1 className="page-header__title">
          {icon && <i className={`bi ${icon}`} aria-hidden="true" />}
          {title}
        </h1>
        {subtitle && <div className="page-header__subtitle">{subtitle}</div>}
      </div>
      {actions && <div className="page-header__actions">{actions}</div>}
    </div>
  );
}
