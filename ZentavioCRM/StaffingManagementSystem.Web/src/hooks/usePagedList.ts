import { useCallback, useEffect, useState } from "react";
import type { ApiResponse } from "@/services/authService";
import type { PagedResult } from "@/services/leadService";

export interface PagedListParams {
  page: number;
  pageSize: number;
  sortBy?: string;
  sortDescending: boolean;
}

export interface UsePagedListOptions {
  defaultPageSize?: number;
  defaultSortBy?: string;
  defaultSortDescending?: boolean;
}

/**
 * Shared paging + server-side sorting state for list screens.
 *
 * Owns page/pageSize/sortBy/sortDescending state, re-fetches whenever any of them (or the
 * caller-supplied `deps`, e.g. a search box or status filter) change, and exposes handlers that
 * wire directly into <DataTable> and <Pagination> — every list page in the app uses this instead
 * of hand-rolling its own load()/page state.
 *
 * @param fetcher       The service's `.search(params)` call.
 * @param buildParams   Combines the current paging state with the page's own filters into the
 *                       exact params object `fetcher` expects.
 * @param deps          Extra reactive values (filters) that should trigger a re-fetch — same
 *                       convention as a useEffect dependency array.
 */
export function usePagedList<TItem, TParams>(
  fetcher: (params: TParams) => Promise<ApiResponse<PagedResult<TItem>>>,
  buildParams: (paging: PagedListParams) => TParams,
  deps: unknown[] = [],
  options: UsePagedListOptions = {}
) {
  const { defaultPageSize = 20, defaultSortBy, defaultSortDescending = true } = options;

  const [items, setItems] = useState<TItem[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [page, setPage] = useState(1);
  const [pageSize, setPageSizeState] = useState(defaultPageSize);
  const [sortBy, setSortBy] = useState<string | undefined>(defaultSortBy);
  const [sortDescending, setSortDescending] = useState(defaultSortDescending);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(() => {
    setIsLoading(true);
    const params = buildParams({ page, pageSize, sortBy, sortDescending });
    fetcher(params).then((result) => {
      setIsLoading(false);
      if (!result.success || !result.data) {
        setError(result.message || "Unable to load data.");
        return;
      }
      setError(null);
      setItems(result.data.items);
      setTotalCount(result.data.totalCount);
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [page, pageSize, sortBy, sortDescending, ...deps]);

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [load]);

  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  /** Wire directly into <DataTable onSortChange>: clicking the active column flips direction, clicking a new one sorts it descending first. */
  const onSortChange = (column: string) => {
    if (sortBy === column) {
      setSortDescending((prev) => !prev);
    } else {
      setSortBy(column);
      setSortDescending(true);
    }
    setPage(1);
  };

  const setPageSize = (size: number) => {
    setPageSizeState(size);
    setPage(1);
  };

  /** Call after a filter/search-box change so the list doesn't stay stranded on a now-out-of-range page. */
  const resetToFirstPage = () => setPage(1);

  return {
    items,
    totalCount,
    totalPages,
    page,
    pageSize,
    sortBy,
    sortDescending,
    isLoading,
    error,
    setPage,
    setPageSize,
    onSortChange,
    resetToFirstPage,
    reload: load,
  };
}
