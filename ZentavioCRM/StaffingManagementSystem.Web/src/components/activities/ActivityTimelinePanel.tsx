import { useEffect, useState } from "react";
import {
  activityService,
  type Activity,
  type ActivityRecurrenceRule,
  type ActivityType,
  type RelatedEntityType,
} from "@/services/activityService";
import type { ManagedUser } from "@/services/userService";
import { DocumentsPanel } from "@/components/documents/DocumentsPanel";

const ACTIVITY_TYPES: ActivityType[] = ["Call", "Email", "Meeting", "Task", "Note", "Visit", "WhatsApp", "Sms"];

const RECURRENCE_OPTIONS: { value: ActivityRecurrenceRule | ""; label: string }[] = [
  { value: "", label: "Does not repeat" },
  { value: "Daily", label: "Daily" },
  { value: "Weekly", label: "Weekly" },
  { value: "Monthly", label: "Monthly" },
];

/**
 * Shared "Timeline" card — generic activity log for any CRM record (Lead, Opportunity, ...).
 * Includes a quick-add bar (type + subject) with an optional "more options" section for due date,
 * assignee, recurrence, and description — those fields exist on the backend (and feed the existing
 * due-date reminder poll) but previously had no way to be set from the UI at all.
 */
export function ActivityTimelinePanel({
  relatedToType,
  relatedToId,
  users,
}: {
  relatedToType: RelatedEntityType;
  relatedToId: string;
  users: ManagedUser[];
}) {
  const [timeline, setTimeline] = useState<Activity[]>([]);
  const [type, setType] = useState<ActivityType>("Note");
  const [subject, setSubject] = useState("");
  const [showMore, setShowMore] = useState(false);
  const [description, setDescription] = useState("");
  const [dueAtUtc, setDueAtUtc] = useState("");
  const [assignedToUserId, setAssignedToUserId] = useState("");
  const [recurrenceRule, setRecurrenceRule] = useState<ActivityRecurrenceRule | "">("");
  const [recurrenceCount, setRecurrenceCount] = useState(4);
  const [expandedAttachments, setExpandedAttachments] = useState<Record<string, boolean>>({});

  const load = async () => {
    const result = await activityService.getTimeline(relatedToType, relatedToId);
    if (result.success && result.data) setTimeline(result.data);
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [relatedToType, relatedToId]);

  const resetComposer = () => {
    setSubject("");
    setDescription("");
    setDueAtUtc("");
    setAssignedToUserId("");
    setRecurrenceRule("");
    setRecurrenceCount(4);
    setShowMore(false);
  };

  const handleAdd = async () => {
    if (!subject.trim()) return;

    const result = await activityService.create(relatedToType, relatedToId, {
      type,
      subject: subject.trim(),
      description: description.trim() || null,
      dueAtUtc: dueAtUtc || null,
      assignedToUserId: assignedToUserId || null,
      recurrenceRule: recurrenceRule || null,
      recurrenceCount: recurrenceRule ? recurrenceCount : null,
    });

    if (result.success) {
      resetComposer();
      load();
    }
  };

  const toggleAttachments = (activityId: string) => {
    setExpandedAttachments((prev) => ({ ...prev, [activityId]: !prev[activityId] }));
  };

  return (
    <div className="card shadow-sm border-0">
      <div className="card-header bg-white fw-semibold">Timeline</div>
      <div className="card-body">
        <div className="d-flex gap-2 mb-2">
          <select
            className="form-select"
            style={{ maxWidth: 140 }}
            value={type}
            onChange={(e) => setType(e.target.value as ActivityType)}
          >
            {ACTIVITY_TYPES.map((t) => (
              <option key={t} value={t}>
                {t}
              </option>
            ))}
          </select>
          <input
            className="form-control"
            placeholder="Log a call, note, or task..."
            value={subject}
            onChange={(e) => setSubject(e.target.value)}
            onKeyDown={(e) => {
              if (e.key === "Enter") {
                e.preventDefault();
                handleAdd();
              }
            }}
          />
          <button
            type="button"
            className={`btn ${showMore ? "btn-secondary" : "btn-outline-secondary"}`}
            title="Due date, assignee, repeat..."
            onClick={() => setShowMore((s) => !s)}
          >
            <i className="bi bi-three-dots" aria-hidden="true" />
          </button>
          <button type="button" className="btn btn-outline-primary" onClick={handleAdd}>
            Log
          </button>
        </div>

        {showMore && (
          <div className="row g-2 mb-3 align-items-end border rounded p-2 mx-0">
            <div className="col-md-3">
              <label className="form-label small">Due Date</label>
              <input
                type="date"
                className="form-control form-control-sm"
                value={dueAtUtc}
                onChange={(e) => setDueAtUtc(e.target.value)}
              />
            </div>
            <div className="col-md-3">
              <label className="form-label small">Assign To</label>
              <select
                className="form-select form-select-sm"
                value={assignedToUserId}
                onChange={(e) => setAssignedToUserId(e.target.value)}
              >
                <option value="">Unassigned</option>
                {users.map((user) => (
                  <option key={user.id} value={user.id}>
                    {user.fullName}
                  </option>
                ))}
              </select>
            </div>
            <div className="col-md-3">
              <label className="form-label small">Repeat</label>
              <select
                className="form-select form-select-sm"
                value={recurrenceRule}
                onChange={(e) => setRecurrenceRule(e.target.value as ActivityRecurrenceRule | "")}
                disabled={!dueAtUtc}
                title={!dueAtUtc ? "Set a due date to enable repeating" : undefined}
              >
                {RECURRENCE_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </div>
            {recurrenceRule && (
              <div className="col-md-3">
                <label className="form-label small">Occurrences</label>
                <input
                  type="number"
                  min={2}
                  max={52}
                  className="form-control form-control-sm"
                  value={recurrenceCount}
                  onChange={(e) => setRecurrenceCount(Number(e.target.value) || 2)}
                />
              </div>
            )}
            <div className="col-12">
              <label className="form-label small">Description</label>
              <textarea
                className="form-control form-control-sm"
                rows={2}
                value={description}
                onChange={(e) => setDescription(e.target.value)}
              />
            </div>
          </div>
        )}

        {timeline.length === 0 && <div className="text-muted">No activity logged yet.</div>}

        <ul className="list-unstyled mb-0">
          {timeline.map((activity) => {
            const isOverdue =
              !!activity.dueAtUtc && !activity.completedAtUtc && new Date(activity.dueAtUtc) < new Date();

            return (
              <li key={activity.id} className="border-bottom py-2">
                <div className="d-flex justify-content-between">
                  <span className="fw-semibold">
                    <i className="bi bi-clock-history me-2 text-muted" aria-hidden="true" />
                    {activity.type}: {activity.subject}
                    {activity.recurrenceRule && (
                      <i
                        className="bi bi-arrow-repeat ms-2 text-muted"
                        title={`Repeats ${activity.recurrenceRule}`}
                        aria-hidden="true"
                      />
                    )}
                  </span>
                  <span className="text-muted small">{new Date(activity.createdAtUtc).toLocaleString()}</span>
                </div>
                {activity.description && <div className="text-muted small ms-4">{activity.description}</div>}
                {activity.dueAtUtc && (
                  <div className={`small ms-4 ${isOverdue ? "text-danger" : "text-muted"}`}>
                    Due {new Date(activity.dueAtUtc).toLocaleDateString()}
                    {activity.assignedToUserName ? ` — ${activity.assignedToUserName}` : ""}
                  </div>
                )}
                <div className="ms-4 mt-1">
                  <button
                    type="button"
                    className="btn btn-link btn-sm p-0 text-decoration-none"
                    onClick={() => toggleAttachments(activity.id)}
                  >
                    <i className="bi bi-paperclip me-1" aria-hidden="true" />
                    Attachments
                  </button>
                  {expandedAttachments[activity.id] && (
                    <div className="mt-2">
                      <DocumentsPanel entityType="Activity" entityId={activity.id} />
                    </div>
                  )}
                </div>
              </li>
            );
          })}
        </ul>
      </div>
    </div>
  );
}
