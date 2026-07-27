import { useRef, useState } from "react";
import type { ImportResult } from "@/services/importTypes";
import { downloadBlob } from "@/services/importTypes";
import type { ApiResponse } from "@/services/authService";

interface ImportExportBarProps {
  entityLabel: string;
  onExport: () => Promise<Blob>;
  onImport: (file: File) => Promise<ApiResponse<ImportResult>>;
  onImportComplete?: () => void;
}

/** Shared Export/Import buttons — reused by LeadsList and CustomersList. */
export function ImportExportBar({ entityLabel, onExport, onImport, onImportComplete }: ImportExportBarProps) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [isExporting, setIsExporting] = useState(false);
  const [isImporting, setIsImporting] = useState(false);
  const [importResult, setImportResult] = useState<ImportResult | null>(null);
  const [importError, setImportError] = useState<string | null>(null);

  const handleExport = async () => {
    setIsExporting(true);
    try {
      const blob = await onExport();
      downloadBlob(blob, `${entityLabel.toLowerCase()}.csv`);
    } finally {
      setIsExporting(false);
    }
  };

  const handleImportClick = () => {
    fileInputRef.current?.click();
  };

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;

    setIsImporting(true);
    setImportError(null);
    setImportResult(null);
    const result = await onImport(file);
    setIsImporting(false);

    if (!result.success || !result.data) {
      setImportError(result.message || "Import failed.");
      return;
    }
    setImportResult(result.data);
    if (result.data.successCount > 0) onImportComplete?.();
  };

  return (
    <div className="mb-3">
      <div className="d-flex gap-2">
        <button type="button" className="btn btn-outline-secondary" onClick={handleExport} disabled={isExporting}>
          <i className="bi bi-download me-1" aria-hidden="true" />
          {isExporting ? "Exporting..." : "Export CSV"}
        </button>
        <button type="button" className="btn btn-outline-secondary" onClick={handleImportClick} disabled={isImporting}>
          <i className="bi bi-upload me-1" aria-hidden="true" />
          {isImporting ? "Importing..." : "Import CSV"}
        </button>
        <input
          ref={fileInputRef}
          type="file"
          accept=".csv"
          className="d-none"
          onChange={handleFileChange}
        />
      </div>

      {importError && (
        <div className="alert alert-danger mt-2 mb-0 py-2">{importError}</div>
      )}

      {importResult && (
        <div
          className={`alert mt-2 mb-0 py-2 ${importResult.failureCount > 0 ? "alert-warning" : "alert-success"}`}
        >
          <div>
            Imported {importResult.successCount} of {importResult.totalRows} row
            {importResult.totalRows === 1 ? "" : "s"}
            {importResult.failureCount > 0 ? `, ${importResult.failureCount} failed.` : "."}
          </div>
          {importResult.errors.length > 0 && (
            <ul className="mb-0 small mt-1">
              {importResult.errors.slice(0, 10).map((err) => (
                <li key={err.rowNumber}>
                  Row {err.rowNumber}: {err.message}
                </li>
              ))}
              {importResult.errors.length > 10 && <li>...and {importResult.errors.length - 10} more.</li>}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}
