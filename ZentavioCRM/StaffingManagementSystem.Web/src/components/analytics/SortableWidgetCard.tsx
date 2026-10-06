import type { ReactNode } from "react";
import { useSortable } from "@dnd-kit/sortable";
import { CSS } from "@dnd-kit/utilities";
import type { Widget } from "./widgets";

interface SortableWidgetCardProps {
  widget: Widget;
  /** Drag handle, move/edit/remove buttons etc. — only passed while the dashboard is being edited. */
  isEditing: boolean;
  actions?: ReactNode;
  children: ReactNode;
}

/**
 * One dashboard widget as a draggable grid cell. In edit mode the whole card header acts as the drag
 * handle (so buttons and the chart body stay clickable); outside edit mode it's a plain card.
 */
export function SortableWidgetCard({ widget, isEditing, actions, children }: SortableWidgetCardProps) {
  const { attributes, listeners, setNodeRef, setActivatorNodeRef, transform, transition, isDragging } = useSortable({
    id: widget.id,
    disabled: !isEditing,
  });

  return (
    <div
      ref={setNodeRef}
      className={`itm-dash-cell${widget.width === "full" ? " itm-dash-cell--full" : ""}`}
      style={{
        transform: CSS.Transform.toString(transform),
        transition,
        opacity: isDragging ? 0.55 : 1,
        zIndex: isDragging ? 5 : undefined,
        position: "relative",
      }}
    >
      <div className={`card shadow-sm border-0 h-100 ${isEditing ? "border border-primary-subtle" : ""}`}>
        <div className="card-body">
          <div className="d-flex align-items-start mb-2 gap-2">
            {isEditing && (
              <button
                type="button"
                ref={setActivatorNodeRef}
                className="btn btn-sm btn-light"
                style={{ cursor: isDragging ? "grabbing" : "grab", touchAction: "none" }}
                aria-label={`Drag to move ${widget.title}`}
                title="Drag to move"
                {...attributes}
                {...listeners}
              >
                <i className="bi bi-grip-vertical" aria-hidden="true" />
              </button>
            )}
            <h6 className="mb-0 me-auto pt-1">{widget.title}</h6>
            {isEditing && actions}
          </div>
          {children}
        </div>
      </div>
    </div>
  );
}
