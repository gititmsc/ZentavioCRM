import { useEffect, useState } from "react";
import { tagService, type Tag } from "@/services/tagService";
import { useAuth } from "@/context/AuthContext";
import { PermissionCodes } from "@/services/permissionCodes";

interface TagPickerProps {
  selectedTagIds: string[];
  onChange: (tagIds: string[]) => void;
}

const DEFAULT_COLOR = "#6c757d";

/** Toggleable chip list for picking existing tags, plus an inline "create new tag" input for users with Tags.Manage. Fetches the full tag list once on mount — tenant-wide tag vocabularies are small enough that a simple full list beats a typeahead here. */
export function TagPicker({ selectedTagIds, onChange }: TagPickerProps) {
  const { hasPermission } = useAuth();
  const canCreateTags = hasPermission(PermissionCodes.TagsManage);

  const [allTags, setAllTags] = useState<Tag[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [newTagName, setNewTagName] = useState("");
  const [isCreating, setIsCreating] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    (async () => {
      const result = await tagService.getAll();
      if (result.success && result.data) {
        setAllTags(result.data);
      }
      setIsLoading(false);
    })();
  }, []);

  const toggle = (tagId: string) => {
    onChange(
      selectedTagIds.includes(tagId) ? selectedTagIds.filter((id) => id !== tagId) : [...selectedTagIds, tagId]
    );
  };

  const handleCreate = async () => {
    const name = newTagName.trim();
    if (!name) return;

    setIsCreating(true);
    setError(null);
    const result = await tagService.create({ name, color: null });
    setIsCreating(false);

    if (!result.success || !result.data) {
      setError(result.message || "Unable to create tag.");
      return;
    }

    setAllTags((prev) => [...prev, result.data!].sort((a, b) => a.name.localeCompare(b.name)));
    onChange([...selectedTagIds, result.data.id]);
    setNewTagName("");
  };

  if (isLoading) {
    return <div className="text-muted small">Loading tags...</div>;
  }

  return (
    <div>
      {error && <div className="alert alert-danger py-1 px-2 small mb-2">{error}</div>}

      <div className="d-flex flex-wrap gap-2 mb-2">
        {allTags.length === 0 && <span className="text-muted small">No tags yet.</span>}
        {allTags.map((tag) => {
          const isSelected = selectedTagIds.includes(tag.id);
          const color = tag.color || DEFAULT_COLOR;
          return (
            <button
              key={tag.id}
              type="button"
              className="btn btn-sm rounded-pill"
              style={
                isSelected
                  ? { backgroundColor: color, borderColor: color, color: "#fff" }
                  : { backgroundColor: "transparent", borderColor: color, color }
              }
              onClick={() => toggle(tag.id)}
            >
              {isSelected && <i className="bi bi-check-lg me-1" aria-hidden="true" />}
              {tag.name}
            </button>
          );
        })}
      </div>

      {canCreateTags && (
        <div className="input-group input-group-sm" style={{ maxWidth: 280 }}>
          <input
            className="form-control"
            placeholder="New tag name..."
            value={newTagName}
            onChange={(e) => setNewTagName(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault();
                handleCreate();
              }
            }}
          />
          <button
            type="button"
            className="btn btn-outline-secondary"
            disabled={isCreating || !newTagName.trim()}
            onClick={handleCreate}
          >
            <i className="bi bi-plus-lg" aria-hidden="true" />
          </button>
        </div>
      )}
    </div>
  );
}
