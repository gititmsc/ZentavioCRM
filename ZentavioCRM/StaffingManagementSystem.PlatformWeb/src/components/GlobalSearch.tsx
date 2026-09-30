import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { searchService, type GlobalSearchResult } from "@/services/searchService";

/** Topbar search box — finds a tenant (name/subdomain/admin email) or platform admin (name/email)
 * by a single free-text term. Debounced, dismissible, keyboard/click-outside aware. */
export function GlobalSearch() {
  const navigate = useNavigate();
  const containerRef = useRef<HTMLDivElement>(null);
  const [term, setTerm] = useState("");
  const [results, setResults] = useState<GlobalSearchResult[]>([]);
  const [open, setOpen] = useState(false);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const trimmed = term.trim();
    if (trimmed.length < 2) {
      setResults([]);
      setLoading(false);
      return;
    }

    setLoading(true);
    const handle = setTimeout(() => {
      void (async () => {
        const response = await searchService.search(trimmed);
        if (response.success && response.data) {
          setResults(response.data);
        }
        setLoading(false);
      })();
    }, 250);

    return () => clearTimeout(handle);
  }, [term]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (containerRef.current && !containerRef.current.contains(event.target as Node)) {
        setOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  const handleSelect = (result: GlobalSearchResult) => {
    setOpen(false);
    setTerm("");
    setResults([]);
    if (result.type === "Tenant") {
      navigate(`/tenants/${result.id}`);
    } else {
      navigate("/admins");
    }
  };

  return (
    <div ref={containerRef} className="position-relative" style={{ width: 320 }}>
      <div className="search-input mb-0">
        <i className="bi bi-search" aria-hidden="true" />
        <input
          className="form-control"
          placeholder="Search tenants, admins..."
          value={term}
          onChange={(e) => {
            setTerm(e.target.value);
            setOpen(true);
          }}
          onFocus={() => setOpen(true)}
        />
      </div>

      {open && term.trim().length >= 2 && (
        <div
          className="app-card position-absolute"
          style={{ top: "calc(100% + 6px)", left: 0, right: 0, zIndex: 1050, maxHeight: 360, overflowY: "auto", padding: 0 }}
        >
          {loading ? (
            <div className="text-muted small p-3">Searching...</div>
          ) : results.length === 0 ? (
            <div className="text-muted small p-3">No matches found.</div>
          ) : (
            <div className="list-group list-group-flush">
              {results.map((result) => (
                <button
                  key={`${result.type}-${result.id}`}
                  type="button"
                  className="list-group-item list-group-item-action d-flex align-items-center gap-2 py-2"
                  onClick={() => handleSelect(result)}
                >
                  <i
                    className={`bi ${result.type === "Tenant" ? "bi-buildings-fill" : "bi-shield-lock-fill"} text-muted`}
                    aria-hidden="true"
                  />
                  <div className="text-start">
                    <div className="small fw-semibold">{result.label}</div>
                    <div className="text-muted" style={{ fontSize: "0.74rem" }}>
                      {result.subLabel}
                    </div>
                  </div>
                  <span className="badge text-bg-light text-dark border ms-auto" style={{ fontSize: "0.66rem" }}>
                    {result.type === "Tenant" ? "Tenant" : "Admin"}
                  </span>
                </button>
              ))}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
