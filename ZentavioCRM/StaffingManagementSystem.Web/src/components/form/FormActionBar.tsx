import type { ReactNode } from "react";
import "./FormActionBar.css";

interface FormActionBarProps {
  children: ReactNode;
}

/** Sticky Save/Cancel bar pinned to the bottom of the viewport while scrolling a long sectioned form. */
export function FormActionBar({ children }: FormActionBarProps) {
  return <div className="itm-form-action-bar">{children}</div>;
}
