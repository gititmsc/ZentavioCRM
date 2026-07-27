/** Shared shape returned by every CSV import endpoint (Leads, Customers). */
export interface ImportRowError {
  rowNumber: number;
  message: string;
}

export interface ImportResult {
  totalRows: number;
  successCount: number;
  failureCount: number;
  errors: ImportRowError[];
}

/** Triggers a browser download of the given blob under the given filename. */
export function downloadBlob(blob: Blob, filename: string) {
  const url = window.URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
}
