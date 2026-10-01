import { useEffect, useState } from "react";
import { tagService, type TagWithUsage } from "@/services/tagService";
import { PageHeader } from "@/components/layout/PageHeader";
import { useAuth } from "@/context/AuthContext";
import { PermissionCodes } from "@/services/permissionCodes";

const DEFAULT_COLOR = "#6c757d";
const SWATCHES = ["#6c757d", "#dc3545", "#fd7e14", "#ffc107", "#198754", "#0dcaf0", "#0d6efd", "#6f42c1", "#d63384"];

export default function TagManager() {
  const { hasPermission } = useAuth();
  const canManage = hasPermission(PermissionCodes.TagsManage);

  const [tags, setTags] = useState<TagWithUsage[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [newName, setNewName] = useState("");
  const [newColor, setNewColor] = useState<string | null>(null);
  const [isCreating, setIsCreating] = useState(false);

  const [editingId, setEditingId] = useState<string | null>(null);
  const [editName, setEditName] = useState("");
  const [editColor, setEditColor] = useState<string | null>(null);

  const load = async () => {
    setIsLoading(true);
    if (canManage) {
      const result = await tagService.getAllWithUsage();
      if (result.success && result.data) {
        setTags(result.data);
      } else {
        setError(result.message || "Unable to load tags.");
      }
    } else {
      const result = await tagService.getAll();
      if (result.success && result.data) {
        setTags(result.data.map((t) => ({ ...t, leadCount: 0, customerCount: 0 })));
      } else {
        setError(result.message || "Unable to load tags.");
      }
    }
    setIsLoading(false);
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleCreate = async (e: React.FormEvent) => {
    e.preventDefault();
    const name = newName.trim();
    if (!name) return;

    setIsCreating(true);
    setError(null);
    const result = await tagService.create({ name, color: newColor });
    setIsCreating(false);

    if (!result.success) {
      setError(result.message || "Unable to create tag.");
      return;
    }

    setNewName("");
    setNewColor(null);
    load();
  };

  const startEdit = (tag: TagWithUsage) => {
    setEditingId(tag.id);
    setEditName(tag.name);
    setEditColor(tag.color);
  };

  const cancelEdit = () => setEditingId(null);

  const saveEdit = async (id: string) => {
    const name = editName.trim();
    if (!name) return;

    const result = await tagService.update(id, { name, color: editColor });
    if (!result.success) {
      setError(result.message || "Unable to update tag.");
      return;
    }

    setEditingId(null);
    load();
  };

  const handleDelete = async (tag: TagWithUsage) => {
    const usageNote =
      tag.leadCount + tag.customerCount > 0
        ? ` It will be removed from ${tag.leadCount} lead(s) and ${tag.customerCount} customer(s).`
        : "";
    if (!window.confirm(`Delete tag "${tag.name}"?${usageNote}`)) {
      return;
    }

    const result = await tagService.remove(tag.id);
    if (!result.success) {
      window.alert(result.message || "Unable to delete tag.");
      return;
    }
    load();
  };

  return (
    <div>
      <PageHeader title="Tag Manager" subtitle="Create and manage the shared tags used on Leads and Customers." />

      {error && <div className="alert alert-danger">{error}</div>}

      {canManage && (
      <div className="card shadow-sm border-0 p-3 mb-3">
        <form className="d-flex flex-wrap align-items-center gap-2" onSubmit={handleCreate}>
          <input
            className="form-control"
            style={{ maxWidth: 240 }}
            placeholder="New tag name..."
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
          />
          <div className="d-flex gap-1">
            {SWATCHES.map((swatch) => (
              <button
                key={swatch}
                type="button"
                className="btn p-0 rounded-circle"
                style={{
                  width: 24,
                  height: 24,
                  backgroundColor: swatch,
                  border: newColor === swatch ? "2px solid #000" : "1px solid #dee2e6",
                }}
                aria-label={`Color ${swatch}`}
                onClick={() => setNewColor(swatch)}
              />
            ))}
          </div>
          <button type="submit" className="btn btn-primary" disabled={isCreating || !newName.trim()}>
            <i className="bi bi-plus-lg me-1" aria-hidden="true" />
            Add Tag
          </button>
        </form>
      </div>
      )}

      <div className="card shadow-sm border-0">
        <table className="table mb-0 align-middle">
          <thead>
            <tr>
              <th>Tag</th>
              <th>Leads</th>
              <th>Customers</th>
              <th className="text-end">Actions</th>
            </tr>
          </thead>
          <tbody>
            {isLoading && (
              <tr>
                <td colSpan={4} className="text-center text-muted py-4">
                  Loading...
                </td>
              </tr>
            )}
            {!isLoading && tags.length === 0 && (
              <tr>
                <td colSpan={4} className="text-center text-muted py-4">
                  No tags yet — add one above.
                </td>
              </tr>
            )}
            {!isLoading &&
              tags.map((tag) =>
                editingId === tag.id ? (
                  <tr key={tag.id}>
                    <td colSpan={2}>
                      <div className="d-flex align-items-center gap-2">
                        <input
                          className="form-control form-control-sm"
                          style={{ maxWidth: 200 }}
                          value={editName}
                          onChange={(e) => setEditName(e.target.value)}
                          autoFocus
                        />
                        <div className="d-flex gap-1">
                          {SWATCHES.map((swatch) => (
                            <button
                              key={swatch}
                              type="button"
                              className="btn p-0 rounded-circle"
                              style={{
                                width: 20,
                                height: 20,
                                backgroundColor: swatch,
                                border: editColor === swatch ? "2px solid #000" : "1px solid #dee2e6",
                              }}
                              aria-label={`Color ${swatch}`}
                              onClick={() => setEditColor(swatch)}
                            />
                          ))}
                        </div>
                      </div>
                    </td>
                    <td>{tag.customerCount}</td>
                    <td className="text-end">
                      <button className="btn btn-sm btn-primary me-2" onClick={() => saveEdit(tag.id)}>
                        Save
                      </button>
                      <button className="btn btn-sm btn-outline-secondary" onClick={cancelEdit}>
                        Cancel
                      </button>
                    </td>
                  </tr>
                ) : (
                  <tr key={tag.id}>
                    <td>
                      <span
                        className="badge rounded-pill"
                        style={{
                          backgroundColor: `${tag.color || DEFAULT_COLOR}22`,
                          color: tag.color || DEFAULT_COLOR,
                          border: `1px solid ${tag.color || DEFAULT_COLOR}`,
                        }}
                      >
                        {tag.name}
                      </span>
                    </td>
                    <td>{tag.leadCount}</td>
                    <td>{tag.customerCount}</td>
                    <td className="text-end">
                      {canManage && (
                        <>
                          <button
                            type="button"
                            className="btn btn-sm btn-outline-secondary me-2"
                            onClick={() => startEdit(tag)}
                          >
                            Edit
                          </button>
                          <button
                            type="button"
                            className="btn btn-sm btn-outline-danger"
                            onClick={() => handleDelete(tag)}
                          >
                            Delete
                          </button>
                        </>
                      )}
                    </td>
                  </tr>
                )
              )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
