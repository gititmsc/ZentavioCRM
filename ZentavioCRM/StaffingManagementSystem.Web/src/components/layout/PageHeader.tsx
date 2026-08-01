import type { ReactNode } from "react";
import { Link } from "react-router-dom";
import "./PageHeader.css";

interface PageHeaderProps {
  title: ReactNode;
  subtitle?: ReactNode;
  backTo?: string;
  backLabel?: string;
  actions?: ReactNode;
  /** Small badge/pill rendered next to the title, e.g. a status chip on a detail page. */
  badge?: ReactNode;
}

/** Shared page header — back link, title (+ optional badge), subtitle, and a right-aligned action slot. Used across every list/form/detail screen for a consistent modern-CRM header. */
export function PageHeader({ title, subtitle, backTo, backLabel = "Back", actions, badge }: PageHeaderProps) {
  return (
    <div className="itm-page-header">
      <div>
        {backTo && (
          <Link to={backTo} className="itm-page-header__back">
            <i className="bi bi-arrow-left" aria-hidden="true" />
            {backLabel}
          </Link>
        )}
        <div className="itm-page-header__title-row">
          <h1 className="itm-page-header__title">{title}</h1>
          {badge}
        </div>
        {subtitle && <div className="itm-page-header__subtitle">{subtitle}</div>}
      </div>
      {actions && <div className="itm-page-header__actions">{actions}</div>}
    </div>
  );
}
