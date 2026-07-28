/**
 * Documents API — generic file attachments for any CRM record. Wraps ZentavioCRM.Api's
 * DocumentsController.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";

export interface DocumentFile {
  id: string;
  entityType: string;
  entityId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  uploadedByUserName: string | null;
  createdAtUtc: string;
}

const getForEntity = (entityType: string, entityId: string) =>
  callApi<DocumentFile[]>(apiClient.get("/api/documents", { params: { entityType, entityId } }));

const upload = (entityType: string, entityId: string, file: File) => {
  const formData = new FormData();
  formData.append("entityType", entityType);
  formData.append("entityId", entityId);
  formData.append("file", file);
  return callApi<DocumentFile>(
    apiClient.post("/api/documents", formData, { headers: { "Content-Type": "multipart/form-data" } })
  );
};

const downloadUrl = (id: string) => `/api/documents/${id}/download`;

const download = async (id: string, fileName: string) => {
  const response = await apiClient.get(downloadUrl(id), { responseType: "blob" });
  const url = window.URL.createObjectURL(response.data as Blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  window.URL.revokeObjectURL(url);
};

const remove = (id: string) => callApi<boolean>(apiClient.delete(`/api/documents/${id}`));

export const documentService = { getForEntity, upload, download, remove };
