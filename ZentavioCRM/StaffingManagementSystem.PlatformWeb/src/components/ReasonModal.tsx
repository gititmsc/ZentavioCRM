import { useState } from "react";

interface ReasonModalProps {
  title: string;
  description: string;
  confirmLabel: string;
  confirmVariant?: "primary" | "danger" | "warning";
  reasonRequired?: boolean;
  onCancel: () => void;
  onConfirm: (reason: string) => Promise<void>;
}

/** Shared confirm-with-reason dialog for Suspend/Stop/Impersonate — every one of these platform
 * actions is audit-logged with a reason, so the UI collects it in one consistent place. */
export function ReasonModal({
  title,
  description,
  confirmLabel,
  confirmVariant = "primary",
  reasonRequired = false,
  onCancel,
  onConfirm,
}: ReasonModalProps) {
  const [reason, setReason] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const handleConfirm = async () => {
    if (reasonRequired && !reason.trim()) {
      setError("A reason is required.");
      return;
    }
    setSubmitting(true);
    setError(null);
    try {
      await onConfirm(reason.trim());
    } catch {
      setError("Something went wrong. Please try again.");
      setSubmitting(false);
    }
  };

  return (
    <div className="modal d-block" tabIndex={-1} style={{ background: "rgba(15, 23, 42, 0.45)" }}>
      <div className="modal-dialog modal-dialog-centered">
        <div className="modal-content" style={{ borderRadius: "var(--itm-radius-card)" }}>
          <div className="modal-header">
            <h5 className="modal-title">{title}</h5>
            <button type="button" className="btn-close" onClick={onCancel} aria-label="Close" />
          </div>
          <div className="modal-body">
            <p className="text-muted">{description}</p>
            {error && (
              <div className="alert alert-danger py-2" role="alert">
                {error}
              </div>
            )}
            <label className="form-label">Reason{reasonRequired ? "" : " (optional)"}</label>
            <textarea
              className="form-control"
              rows={3}
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder="Why are you taking this action?"
              maxLength={500}
            />
          </div>
          <div className="modal-footer">
            <button type="button" className="btn btn-outline-secondary" onClick={onCancel} disabled={submitting}>
              Cancel
            </button>
            <button type="button" className={`btn btn-${confirmVariant}`} onClick={() => void handleConfirm()} disabled={submitting}>
              {submitting ? "Working..." : confirmLabel}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
