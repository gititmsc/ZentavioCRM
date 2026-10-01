import type { Tag } from "@/services/tagService";

const DEFAULT_COLOR = "#6c757d";

/** Read-only chip display for a record's structured tags — used in list grids and detail screens. */
export function TagChips({ tags }: { tags: Tag[] }) {
  if (tags.length === 0) {
    return null;
  }

  return (
    <div className="d-flex flex-wrap gap-1">
      {tags.map((tag) => {
        const color = tag.color || DEFAULT_COLOR;
        return (
          <span
            key={tag.id}
            className="badge rounded-pill"
            style={{ backgroundColor: `${color}22`, color, border: `1px solid ${color}` }}
          >
            {tag.name}
          </span>
        );
      })}
    </div>
  );
}
