import { useEffect, useRef, useState } from "react";
import type { ApiResponse } from "@/services/authService";

export interface MergeCandidate {
  id: string;
  label: string;
  sublabel?: string;
}

interface CandidatePickerProps {
  label: string;
  selected: MergeCandidate | null;
  onSelect: (candidate: MergeCandidate | null) => void;
  searchCandidates: (term: string) => Promise<MergeCandidate[]>;
  excludeId?: string;
}

function CandidatePicker({ label, selected, onSelect, searchCandidates, excludeId }: CandidatePickerProps) {
  const [term, setTerm] = useState("");
  const [results, setResults] = useState<MergeCandidate[]>([]);
  const [isSearching, setIsSearching] = useState(false);
  const debounceRef = useRef<ReturnType<typeof setTimeout> | undefined>(undefined);

  useEffect(() => {
    if (!term.trim()) {
      setResults([]);
      return undefined;
    }
    if (debounceRef.current) clearTimeout(debounceRef.current);
    debounceRef.current = setTimeout(async () => {
      setIsSearching(true);
      const found = await searchCandidates(term.trim());
      setIsSearching(false);
      setResults(found.filter((c) => c.id !== excludeId));
    }, 300);
    return () => {
      if (debounceRef.current) clearTimeout(debounceRef.current);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [term, excludeId]);

  if (selected) {
    return (
      <div>
        <label className="form-label fw-semibold">{label}</label>
        <div className="d-flex align-items-center justify-content-between border rounded px-3 py-2">
          <div>
            <div>{selected.label}</div>
            {selected.sublabel && <div className="small text-muted">{selected.sublabel}</div>}
          </div>
          <button type="button" className="btn btn-sm btn-link" onClick={() => onSelect(null)}>
            Change
          </button>
        </div>
      </div>
    );
  }

  return (
    <div>
      <label className="form-label fw-semibold">{label}</label>
      <input
        className="form-control"
        placeholder="Search by name..."
        value={term}
        onChange={(e) => setTerm(e.target.value)}
      />
      {isSearching && <div className="small text-muted mt-1">Searching...</div>}
      {results.length > 0 && (
        <div className="list-group mt-1" style={{ maxHeight: 200, overflowY: "auto" }}>
          {results.map((c) => (
            <button
              key={c.id}
              type="button"
              className="list-group-item list-group-item-action"
              onClick={() => {
                onSelect(c);
                setTerm("");
                setResults([]);
              }}
            >
              <div>{c.label}</div>
              {c.sublabel && <div className="small text-muted">{c.sublabel}</div>}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}

interface MergeModalProps {
  /** "Lead" or "Customer" — drives copy only. */
  entityLabel: "Lead" | "Customer";
  onClose: () => void;
  onMerged: () => void;
  searchCandidates: (term: string) => Promise<MergeCandidate[]>;
  merge: (survivingId: string, losingId: string) => Promise<ApiResponse<unknown>>;
}

/** A record-vs-record merge picker, reused by LeadsList and CustomersList. */
export function MergeModal({ entityLabel, onClose, onMerged, searchCandidates, merge }: MergeModalProps) {
  const [surviving, setSurviving] = useState<MergeCandidate | null>(null);
  const [losing, setLosing] = useState<MergeCandidate | null>(null);
  const [isMerging, setIsMerging] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleMerge = async () => {
    if (!surviving || !losing) return;
    if (!window.confirm(`Merge "${losing.label}" into "${surviving.label}"? This cannot be undone.`)) {
      return;
    }

    setIsMerging(true);
    setError(null);
    const result = await merge(surviving.id, losing.id);
    setIsMerging(false);

    if (!result.success) {
      setError(result.message || "Merge failed.");
      return;
    }
    onMerged();
    onClose();
  };

  return (
    <div className="modal d-block" tabIndex={-1} style={{ backgroundColor: "rgba(0,0,0,0.5)" }}>
      <div className="modal-dialog">
        <div className="modal-content">
          <div className="modal-header">
            <h5 className="modal-title">Merge Duplicate {entityLabel}s</h5>
            <button type="button" className="btn-close" onClick={onClose} aria-label="Close" />
          </div>
          <div className="modal-body d-flex flex-column gap-3">
            <p className="text-muted small mb-0">
              Pick the record to keep and the duplicate to merge into it. Activity history, tags
              {entityLabel === "Customer"
                ? ", opportunities, quotations, sales orders, contacts, and addresses"
                : ""}{" "}
              from the duplicate move onto the surviving record.
            </p>
            <CandidatePicker
              label="Keep this record"
              selected={surviving}
              onSelect={setSurviving}
              searchCandidates={searchCandidates}
              excludeId={losing?.id}
            />
            <CandidatePicker
              label={`Merge away this duplicate ${entityLabel === "Customer" ? "(will be archived)" : "(will be deleted)"}`}
              selected={losing}
              onSelect={setLosing}
              searchCandidates={searchCandidates}
              excludeId={surviving?.id}
            />
            {error && <div className="alert alert-danger mb-0 py-2">{error}</div>}
          </div>
          <div className="modal-footer">
            <button type="button" className="btn btn-outline-secondary" onClick={onClose}>
              Cancel
            </button>
            <button
              type="button"
              className="btn btn-danger"
              disabled={!surviving || !losing || isMerging}
              onClick={handleMerge}
            >
              {isMerging ? "Merging..." : "Merge"}
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
