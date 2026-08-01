import type { ReactNode } from "react";
import "./FormSection.css";

interface FormSectionProps {
  /** Bootstrap icon class, e.g. "bi-building". */
  icon: string;
  title: string;
  description?: string;
  /** Right-aligned header slot, e.g. an "Add Contact" button. */
  actions?: ReactNode;
  children: ReactNode;
  className?: string;
}

/**
 * A labeled, icon-badged card used to group related fields — the building block of the
 * "sectioned card" form layout (Basic Info / Assignment / Tracking, etc.) that replaced the old
 * single flat card per form.
 */
export function FormSection({ icon, title, description, actions, children, className }: FormSectionProps) {
  return (
    <section className={`itm-form-section ${className ?? ""}`}>
      <header className="itm-form-section__header">
        <div className="itm-form-section__heading">
          <span className="itm-form-section__icon">
            <i className={`bi ${icon}`} aria-hidden="true" />
          </span>
          <div>
            <h2 className="itm-form-section__title">{title}</h2>
            {description && <p className="itm-form-section__description">{description}</p>}
          </div>
        </div>
        {actions && <div className="itm-form-section__actions">{actions}</div>}
      </header>
      <div className="itm-form-section__body">{children}</div>
    </section>
  );
}
