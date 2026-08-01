import "./DataTable.css";

interface PaginationProps {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
  pageSizeOptions?: number[];
}

/** Compact 1…N page-number window around the current page, e.g. [1, "...", 4, 5, 6, "...", 12]. */
function getPageWindow(current: number, total: number): (number | "...")[] {
  const delta = 1;
  const left = Math.max(2, current - delta);
  const right = Math.min(total - 1, current + delta);
  const range: (number | "...")[] = [1];

  if (left > 2) range.push("...");
  for (let i = left; i <= right; i++) range.push(i);
  if (right < total - 1) range.push("...");
  if (total > 1) range.push(total);

  return range;
}

/** Shared pagination bar — page-size dropdown plus a compact page-number strip, used under every DataTable. */
export function Pagination({
  page,
  pageSize,
  totalCount,
  totalPages,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [10, 20, 50, 100],
}: PaginationProps) {
  if (totalCount === 0) {
    return null;
  }

  const startRow = (page - 1) * pageSize + 1;
  const endRow = Math.min(page * pageSize, totalCount);
  const pageNumbers = getPageWindow(page, totalPages);

  return (
    <div className="itm-pagination-bar d-flex flex-wrap justify-content-between align-items-center gap-3">
      <div className="d-flex align-items-center gap-2">
        <span className="text-muted small">
          Showing {startRow}&ndash;{endRow} of {totalCount}
        </span>
        <select
          className="form-select form-select-sm itm-pagesize-select"
          value={pageSize}
          onChange={(e) => onPageSizeChange(Number(e.target.value))}
          aria-label="Rows per page"
        >
          {pageSizeOptions.map((size) => (
            <option key={size} value={size}>
              {size} / page
            </option>
          ))}
        </select>
      </div>

      {totalPages > 1 && (
        <nav aria-label="Pagination">
          <ul className="pagination pagination-sm itm-pagination-list mb-0">
            <li className={`page-item ${page <= 1 ? "disabled" : ""}`}>
              <button
                type="button"
                className="page-link"
                onClick={() => onPageChange(page - 1)}
                disabled={page <= 1}
                aria-label="Previous page"
              >
                <i className="bi bi-chevron-left" aria-hidden="true" />
              </button>
            </li>
            {pageNumbers.map((p, idx) =>
              p === "..." ? (
                <li key={`ellipsis-${idx}`} className="page-item disabled">
                  <span className="page-link">&hellip;</span>
                </li>
              ) : (
                <li key={p} className={`page-item ${p === page ? "active" : ""}`}>
                  <button type="button" className="page-link" onClick={() => onPageChange(p)}>
                    {p}
                  </button>
                </li>
              )
            )}
            <li className={`page-item ${page >= totalPages ? "disabled" : ""}`}>
              <button
                type="button"
                className="page-link"
                onClick={() => onPageChange(page + 1)}
                disabled={page >= totalPages}
                aria-label="Next page"
              >
                <i className="bi bi-chevron-right" aria-hidden="true" />
              </button>
            </li>
          </ul>
        </nav>
      )}
    </div>
  );
}
