import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  DndContext,
  PointerSensor,
  useDraggable,
  useDroppable,
  useSensor,
  useSensors,
  type DragEndEvent,
} from "@dnd-kit/core";
import { CSS } from "@dnd-kit/utilities";
import { opportunityService, type OpportunityListItem, type OpportunityStage } from "@/services/opportunityService";
import { PageHeader } from "@/components/layout/PageHeader";

const STAGES: OpportunityStage[] = [
  "Qualification",
  "Discovery",
  "Proposal",
  "Negotiation",
  "VerbalCommit",
  "ClosedWon",
  "ClosedLost",
];

const STAGE_LABEL: Record<OpportunityStage, string> = {
  Qualification: "Qualification",
  Discovery: "Discovery",
  Proposal: "Proposal",
  Negotiation: "Negotiation",
  VerbalCommit: "Verbal Commit",
  ClosedWon: "Closed Won",
  ClosedLost: "Closed Lost",
};

/** Mirrors OpportunityService.cs's AllowedStageTransitions — kept in sync by hand since the server is the real source of truth and re-validates every drop anyway. */
const ALLOWED_TRANSITIONS: Record<OpportunityStage, OpportunityStage[]> = {
  Qualification: ["Discovery", "ClosedWon", "ClosedLost"],
  Discovery: ["Proposal", "ClosedWon", "ClosedLost"],
  Proposal: ["Negotiation", "ClosedWon", "ClosedLost"],
  Negotiation: ["VerbalCommit", "ClosedWon", "ClosedLost"],
  VerbalCommit: ["ClosedWon", "ClosedLost"],
  ClosedWon: [],
  ClosedLost: [],
};

type BoardColumns = Record<OpportunityStage, OpportunityListItem[]>;

function emptyColumns(): BoardColumns {
  return Object.fromEntries(STAGES.map((s) => [s, []])) as BoardColumns;
}

function OpportunityCard({ opp }: { opp: OpportunityListItem }) {
  const navigate = useNavigate();
  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
    id: opp.id,
    data: { stage: opp.stage },
  });

  return (
    <div
      ref={setNodeRef}
      style={{ transform: CSS.Translate.toString(transform), opacity: isDragging ? 0.4 : 1, cursor: "grab" }}
      {...listeners}
      {...attributes}
      className="card shadow-sm mb-2"
      onDoubleClick={() => navigate(`/opportunities/${opp.id}`)}
      title="Drag to move stage, double-click to open"
    >
      <div className="card-body p-2">
        <div className="small fw-semibold text-truncate">{opp.name}</div>
        <div className="small text-muted text-truncate">{opp.customerName}</div>
        <div className="d-flex justify-content-between small mt-1">
          <span>{opp.value != null ? `${opp.currencyCode} ${opp.value.toLocaleString()}` : <span className="text-muted">&mdash;</span>}</span>
          <span className="text-muted text-truncate ms-1">{opp.assignedToUserName ?? "Unassigned"}</span>
        </div>
      </div>
    </div>
  );
}

function StageColumn({ stage, items }: { stage: OpportunityStage; items: OpportunityListItem[] }) {
  const { setNodeRef, isOver } = useDroppable({ id: stage });
  const total = items.reduce((sum, o) => sum + (o.value ?? 0), 0);

  return (
    <div
      ref={setNodeRef}
      className={`rounded p-2 flex-shrink-0 ${isOver ? "bg-primary-subtle" : "bg-body-tertiary"}`}
      style={{ width: 260, minHeight: 320 }}
    >
      <div className="d-flex justify-content-between align-items-center mb-1">
        <span className="fw-semibold small">{STAGE_LABEL[stage]}</span>
        <span className="badge text-bg-secondary">{items.length}</span>
      </div>
      {total > 0 && <div className="small text-muted mb-2">{total.toLocaleString()}</div>}
      {items.map((o) => (
        <OpportunityCard key={o.id} opp={o} />
      ))}
    </div>
  );
}

/** Drag-and-drop Kanban view of the Opportunity pipeline — reuses the same /api/opportunities search + stage endpoints as the flat list, just grouped by stage client-side. */
export default function OpportunitiesBoard() {
  const navigate = useNavigate();
  const [columns, setColumns] = useState<BoardColumns>(emptyColumns());
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const sensors = useSensors(useSensor(PointerSensor, { activationConstraint: { distance: 6 } }));

  const loadBoard = async () => {
    setIsLoading(true);
    const results = await Promise.all(STAGES.map((stage) => opportunityService.search({ stage, pageSize: 200 })));
    const next = emptyColumns();
    results.forEach((result, i) => {
      next[STAGES[i]] = result.success && result.data ? result.data.items : [];
    });
    setColumns(next);
    setIsLoading(false);
  };

  useEffect(() => {
    loadBoard();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event;
    if (!over) return;

    const oppId = String(active.id);
    const fromStage = active.data.current?.stage as OpportunityStage | undefined;
    const toStage = over.id as OpportunityStage;
    if (!fromStage || fromStage === toStage) return;

    if (!ALLOWED_TRANSITIONS[fromStage]?.includes(toStage)) {
      setError(`Can't move a deal from ${STAGE_LABEL[fromStage]} straight to ${STAGE_LABEL[toStage]} — stages can't be skipped.`);
      return;
    }

    let reason: string | undefined;
    if (toStage === "ClosedLost") {
      const entered = window.prompt("Reason for marking this deal Closed Lost:");
      if (!entered || !entered.trim()) return;
      reason = entered.trim();
    }

    const opp = columns[fromStage].find((o) => o.id === oppId);
    if (!opp) return;

    setError(null);
    // Optimistic move — reverted below if the server rejects it.
    setColumns((prev) => ({
      ...prev,
      [fromStage]: prev[fromStage].filter((o) => o.id !== oppId),
      [toStage]: [{ ...opp, stage: toStage }, ...prev[toStage]],
    }));

    const result = await opportunityService.updateStage(oppId, toStage, reason);
    if (!result.success) {
      setError(result.message || "Couldn't update the stage.");
      setColumns((prev) => ({
        ...prev,
        [toStage]: prev[toStage].filter((o) => o.id !== oppId),
        [fromStage]: [opp, ...prev[fromStage]],
      }));
    }
  };

  return (
    <div>
      <PageHeader
        title="Pipeline Board"
        subtitle="Drag a deal card to move it through the pipeline."
        actions={
          <button type="button" className="btn btn-outline-secondary" onClick={() => navigate("/opportunities")}>
            <i className="bi bi-list-ul me-1" aria-hidden="true" />
            List View
          </button>
        }
      />

      {error && (
        <div className="alert alert-danger alert-dismissible" role="alert">
          {error}
          <button type="button" className="btn-close" onClick={() => setError(null)} aria-label="Close" />
        </div>
      )}

      {isLoading ? (
        <div className="text-muted">Loading...</div>
      ) : (
        <DndContext sensors={sensors} onDragEnd={handleDragEnd}>
          <div className="d-flex gap-3 overflow-auto pb-3">
            {STAGES.map((stage) => (
              <StageColumn key={stage} stage={stage} items={columns[stage]} />
            ))}
          </div>
        </DndContext>
      )}
    </div>
  );
}
