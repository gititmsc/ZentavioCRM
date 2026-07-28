import { useEffect, useRef, useState } from "react";
import { documentService, type DocumentFile } from "@/services/documentService";

function formatSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

/** Shared "Documents" card — file attachments for any CRM record (Customer, Opportunity, ...). */
export function DocumentsPanel({ entityType, entityId }: { entityType: string; entityId: string }) {
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [documents, setDocuments] = useState<DocumentFile[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isUploading, setIsUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setIsLoading(true);
    const result = await documentService.getForEntity(entityType, entityId);
    setIsLoading(false);
    if (result.success && result.data) setDocuments(result.data);
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [entityType, entityId]);

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    e.target.value = "";
    if (!file) return;

    setError(null);
    setIsUploading(true);
    const result = await documentService.upload(entityType, entityId, file);
    setIsUploading(false);

    if (!result.success) {
      setError(result.message || "Unable to upload file.");
      return;
    }
    load();
  };

  const handleDelete = async (id: string) => {
    if (!window.confirm("Remove this file?")) return;
    const result = await documentService.remove(id);
    if (result.success) load();
  };

  return (
    <div className="card shadow-sm border-0 mt-4">
      <div className="card-header bg-white fw-semibold d-flex justify-content-between align-items-center">
        <span>Documents</span>
        <button
          type="button"
          className="btn btn-sm btn-outline-primary"
          disabled={isUploading}
          onClick={() => fileInputRef.current?.click()}
        >
          <i className="bi bi-upload me-1" aria-hidden="true" />
          {isUploading ? "Uploading..." : "Upload"}
        </button>
        <input ref={fileInputRef} type="file" className="d-none" onChange={handleFileChange} />
      </div>
      <div className="card-body">
        {error && <div className="alert alert-danger py-2">{error}</div>}
        {isLoading && <div className="text-muted small">Loading...</div>}
        {!isLoading && documents.length === 0 && <div className="text-muted small">No files attached yet.</div>}
        <ul className="list-unstyled mb-0">
          {documents.map((doc) => (
            <li key={doc.id} className="d-flex justify-content-between align-items-center border-bottom py-2">
              <div>
                <button
                  type="button"
                  className="btn btn-link p-0 text-decoration-none"
                  onClick={() => documentService.download(doc.id, doc.fileName)}
                >
                  <i className="bi bi-file-earmark me-1" aria-hidden="true" />
                  {doc.fileName}
                </button>
                <div className="text-muted small">
                  {formatSize(doc.sizeBytes)} &middot; {doc.uploadedByUserName ?? "System"} &middot;{" "}
                  {new Date(doc.createdAtUtc).toLocaleString()}
                </div>
              </div>
              <button type="button" className="btn btn-sm btn-outline-danger" onClick={() => handleDelete(doc.id)}>
                <i className="bi bi-trash" aria-hidden="true" />
              </button>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
