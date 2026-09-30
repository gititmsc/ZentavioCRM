import { useEffect, useState } from "react";
import { tenantService, type TenantNote } from "@/services/tenantService";

interface NotesPanelProps {
  tenantId: string;
  onError: (message: string) => void;
}

/** Free-text notes platform admins leave for each other on a tenant — append-only, no edit or
 * delete for now, same as the audit log's own history-preserving philosophy. */
export function NotesPanel({ tenantId, onError }: NotesPanelProps) {
  const [notes, setNotes] = useState<TenantNote[]>([]);
  const [loading, setLoading] = useState(true);
  const [draft, setDraft] = useState("");
  const [saving, setSaving] = useState(false);

  const load = async () => {
    setLoading(true);
    const response = await tenantService.getNotes(tenantId);
    if (response.success && response.data) {
      setNotes(response.data);
    } else {
      onError(response.message || "Could not load notes.");
    }
    setLoading(false);
  };

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [tenantId]);

  const handleAdd = async () => {
    if (!draft.trim()) return;
    setSaving(true);
    try {
      const response = await tenantService.addNote(tenantId, draft.trim());
      if (response.success) {
        setDraft("");
        void load();
      } else {
        onError(response.message || "Could not add note.");
      }
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="app-card">
      <div className="app-card__header">
        <h3 className="app-card__title">
          <i className="bi bi-sticky-fill" aria-hidden="true" />
          Notes
        </h3>
      </div>
      <div className="app-card__body">
        <div className="d-flex gap-2 mb-4">
          <textarea
            className="form-control"
            rows={2}
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            maxLength={2000}
            placeholder="Leave a note for other platform admins about this tenant..."
          />
          <button
            type="button"
            className="btn btn-primary align-self-end"
            onClick={() => void handleAdd()}
            disabled={saving || !draft.trim()}
          >
            {saving ? "Adding..." : "Add"}
          </button>
        </div>

        {loading ? (
          <div className="text-muted small">Loading notes...</div>
        ) : notes.length === 0 ? (
          <div className="empty-state">
            <div className="empty-state__icon">
              <i className="bi bi-sticky-fill" aria-hidden="true" />
            </div>
            <div className="empty-state__title">No notes yet</div>
          </div>
        ) : (
          <div className="list-group list-group-flush">
            {notes.map((note) => (
              <div key={note.id} className="list-group-item px-0 py-3">
                <div style={{ whiteSpace: "pre-wrap" }}>{note.note}</div>
                <div className="text-muted small mt-2">
                  {note.createdByAdminEmail} &middot; {new Date(note.createdAtUtc).toLocaleString()}
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
