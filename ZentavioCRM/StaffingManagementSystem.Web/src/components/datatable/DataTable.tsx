import type { ReactNode } from "react";
import "./DataTable.css";

export interface DataTableColumn<T> {
  /** Sort key sent to the backend (must match the API's sortBy switch case, case-insensitive). Omit to make the column unsortable. */
  key?: string;
  header: ReactNode;
  render: (item: T) => ReactNode;
  align?: "start" | "end" | "center";
  className?: string;
  headerClassName?: string;
}

interface DataTableProps<T> {
  columns: DataTableColumn<T>[];
  items: T[];
  rowKey: (item: T) => string;
  isLoading?: boolean;
  emptyMessage?: string;
  emptyIcon?: string;
  onRowClick?: (item: T) => void;
  sortBy?: string;
  sortDescending?: boolean;
  /** Called with the column's `key` when a sortable header is clicked. */
  onSortChange?: (key: string) => void;
}

/**
 * Shared "fancy" grid used by every list screen in the app. Renders inside a rounded, shadowed
 * card (see DataTable.css) instead of the bare `<table>` each page used to hand-roll, and — when
 * both `sortBy`/`onSortChange` are supplied and a column has a `key` — turns that column's header
 * into a clickable, server-side sort toggle.
 */
export function DataTable<T>({
  columns,
  items,
  rowKey,
  isLoading = false,
  emptyMessage = "No records found.",
  emptyIcon = "bi-inbox",
  onRowClick,
  sortBy,
  sortDescending = true,
  onSortChange,
}: DataTableProps<T>) {
  const alignClass = (align?: "start" | "end" | "center") =>
    align === "end" ? "text-end" : align === "center" ? "text-center" : "";

  return (
    <div className="itm-datatable-card">
      <div className="table-responsive">
        <table className="table itm-datatable align-middle mb-0">
          <thead>
            <tr>
              {columns.map((col, idx) => {
                const sortable = !!col.key && !!onSortChange;
                const isActive = sortable && sortBy === col.key;
                return (
                  <th
                    key={col.key ?? `col-${idx}`}
                    scope="col"
                    className={[col.headerClassName, alignClass(col.align), sortable ? "itm-sortable" : ""]
                      .filter(Boolean)
                      .join(" ")}
                    onClick={sortable ? () => onSortChange!(col.key!) : undefined}
                    role={sortable ? "button" : undefined}
                    aria-sort={isActive ? (sortDescending ? "descending" : "ascending") : undefined}
                  >
                    <span className="itm-th-content">
                      {col.header}
                      {sortable && (
                        <span className={`itm-sort-icon ${isActive ? "active" : ""}`}>
                          <i
                            className={`bi ${
                              isActive ? (sortDescending ? "bi-caret-down-fill" : "bi-caret-up-fill") : "bi-filter"
                            }`}
                            aria-hidden="true"
                          />
                        </span>
                      )}
                    </span>
                  </th>
                );
              })}
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr>
                <td colSpan={columns.length} className="text-center text-muted itm-empty-state">
                  <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true" />
                  Loading...
                </td>
              </tr>
            )}
            {!isLoading && items.length === 0 && (
              <tr>
                <td colSpan={columns.length} className="text-center text-muted itm-empty-state">
                  <div className="mb-2">
                    <i className={`bi ${emptyIcon}`} aria-hidden="true" />
                  </div>
                  {emptyMessage}
                </td>
              </tr>
            )}
            {!isLoading &&
              items.map((item) => (
                <tr
                  key={rowKey(item)}
                  className={onRowClick ? "itm-row-clickable" : ""}
                  role={onRowClick ? "button" : undefined}
                  onClick={onRowClick ? () => onRowClick(item) : undefined}
                >
                  {columns.map((col, idx) => (
                    <td key={col.key ?? `col-${idx}`} className={[col.className, alignClass(col.align)].filter(Boolean).join(" ")}>
                      {col.render(item)}
                    </td>
                  ))}
                </tr>
              ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
