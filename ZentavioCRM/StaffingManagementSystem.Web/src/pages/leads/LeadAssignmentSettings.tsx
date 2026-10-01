import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  leadAssignmentService,
  type LeadAssignmentRule,
  type SaveLeadAssignmentRuleRequest,
} from "@/services/leadAssignmentService";
import { territoryService, type Territory } from "@/services/territoryService";
import { userService, type ManagedUser } from "@/services/userService";
import { PageHeader } from "@/components/layout/PageHeader";
import { FormSection } from "@/components/form/FormSection";

const EMPTY_FORM: SaveLeadAssignmentRuleRequest = { territoryId: null, isActive: true, eligibleUserIds: [] };

export default function LeadAssignmentSettingsPage() {
  const navigate = useNavigate();
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [autoAssignEnabled, setAutoAssignEnabled] = useState(false);
  const [isSavingSettings, setIsSavingSettings] = useState(false);
  const [settingsMessage, setSettingsMessage] = useState<string | null>(null);

  const [rules, setRules] = useState<LeadAssignmentRule[]>([]);
  const [territories, setTerritories] = useState<Territory[]>([]);
  const [users, setUsers] = useState<ManagedUser[]>([]);

  const [editingRuleId, setEditingRuleId] = useState<string | "new" | null>(null);
  const [form, setForm] = useState<SaveLeadAssignmentRuleRequest>(EMPTY_FORM);
  const [formError, setFormError] = useState<string | null>(null);
  const [isSavingRule, setIsSavingRule] = useState(false);

  const loadAll = async () => {
    setIsLoading(true);
    const [settingsResult, rulesResult, territoriesResult, usersResult] = await Promise.all([
      leadAssignmentService.getSettings(),
      leadAssignmentService.getRules(),
      territoryService.getAll(),
      userService.getAll(),
    ]);

    if (settingsResult.success && settingsResult.data) setAutoAssignEnabled(settingsResult.data.autoAssignEnabled);
    if (rulesResult.success && rulesResult.data) setRules(rulesResult.data);
    if (territoriesResult.success && territoriesResult.data) setTerritories(territoriesResult.data);
    if (usersResult.success && usersResult.data) setUsers(usersResult.data.filter((u) => u.isActive));

    if (!settingsResult.success || !rulesResult.success) {
      setError(settingsResult.message || rulesResult.message || "Unable to load lead assignment settings.");
    }
    setIsLoading(false);
  };

  useEffect(() => {
    loadAll();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const handleSaveSettings = async () => {
    setIsSavingSettings(true);
    setSettingsMessage(null);
    const result = await leadAssignmentService.updateSettings({ autoAssignEnabled, updatedAtUtc: null });
    setIsSavingSettings(false);
    if (!result.success) {
      setError(result.message || "Unable to save settings.");
      return;
    }
    setSettingsMessage("Saved.");
  };

  const startCreate = () => {
    setEditingRuleId("new");
    setForm(EMPTY_FORM);
    setFormError(null);
  };

  const startEdit = (rule: LeadAssignmentRule) => {
    setEditingRuleId(rule.id);
    setForm({
      territoryId: rule.territoryId,
      isActive: rule.isActive,
      eligibleUserIds: rule.eligibleUsers.map((u) => u.id),
    });
    setFormError(null);
  };

  const cancelEdit = () => {
    setEditingRuleId(null);
    setForm(EMPTY_FORM);
    setFormError(null);
  };

  const toggleUser = (userId: string) => {
    setForm((f) => ({
      ...f,
      eligibleUserIds: f.eligibleUserIds.includes(userId)
        ? f.eligibleUserIds.filter((id) => id !== userId)
        : [...f.eligibleUserIds, userId],
    }));
  };

  const saveRule = async () => {
    setIsSavingRule(true);
    setFormError(null);

    const result =
      editingRuleId === "new"
        ? await leadAssignmentService.createRule(form)
        : await leadAssignmentService.updateRule(editingRuleId!, form);

    setIsSavingRule(false);
    if (!result.success) {
      setFormError(result.message || "Unable to save this rule.");
      return;
    }
    cancelEdit();
    loadAll();
  };

  const deleteRule = async (rule: LeadAssignmentRule) => {
    const label = rule.territoryName ?? "the tenant-wide fallback rule";
    if (!window.confirm(`Delete the assignment rule for ${label}?`)) return;
    const result = await leadAssignmentService.removeRule(rule.id);
    if (!result.success) {
      setError(result.message || "Unable to delete this rule.");
      return;
    }
    loadAll();
  };

  const usedTerritoryIds = new Set(
    rules.filter((r) => r.id !== editingRuleId).map((r) => r.territoryId).filter((id): id is string => id !== null)
  );

  if (isLoading) {
    return <div className="text-muted">Loading...</div>;
  }

  return (
    <div>
      <PageHeader
        title="Lead Assignment"
        subtitle="Automatically route new leads to a user via round-robin, by territory."
        backTo="/leads"
        backLabel="Back to Leads"
      />

      {error && <div className="alert alert-danger">{error}</div>}

      <FormSection icon="bi-signpost-split" title="Auto-Assignment" description="When on, a new lead created without an explicit owner is routed through the rules below instead of being left unassigned.">
        <div className="form-check form-switch mb-2">
          <input
            className="form-check-input"
            type="checkbox"
            role="switch"
            id="autoAssignEnabled"
            checked={autoAssignEnabled}
            onChange={(e) => setAutoAssignEnabled(e.target.checked)}
          />
          <label className="form-check-label" htmlFor="autoAssignEnabled">
            Auto-assign new leads
          </label>
        </div>
        <button type="button" className="btn btn-primary btn-sm" onClick={handleSaveSettings} disabled={isSavingSettings}>
          {isSavingSettings ? "Saving..." : "Save"}
        </button>
        {settingsMessage && <span className="text-success small ms-2">{settingsMessage}</span>}
      </FormSection>

      <FormSection
        icon="bi-people"
        title="Routing Rules"
        description="One rule per territory, plus an optional tenant-wide fallback rule (no territory) for leads with no territory or no matching territory-specific rule. Each rule round-robins across its eligible users."
      >
        {rules.length === 0 && editingRuleId !== "new" && (
          <p className="text-muted small">No rules configured yet — auto-assignment has nothing to route through until you add one.</p>
        )}

        {rules.length > 0 && (
          <div className="table-responsive mb-3">
            <table className="table table-sm align-middle">
              <thead>
                <tr>
                  <th>Territory</th>
                  <th>Active</th>
                  <th>Eligible users</th>
                  <th>Last assigned</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {rules.map((rule) => (
                  <tr key={rule.id}>
                    <td>{rule.territoryName ?? <span className="fst-italic">Tenant-wide fallback</span>}</td>
                    <td>
                      <span className={`badge ${rule.isActive ? "text-bg-success" : "text-bg-secondary"}`}>
                        {rule.isActive ? "Active" : "Inactive"}
                      </span>
                    </td>
                    <td className="small">{rule.eligibleUsers.map((u) => u.fullName).join(", ") || <span className="text-muted">None</span>}</td>
                    <td className="small text-muted">
                      {rule.lastAssignedUserName ? `${rule.lastAssignedUserName} (${new Date(rule.lastAssignedAtUtc!).toLocaleString()})` : "—"}
                    </td>
                    <td className="text-end">
                      <button type="button" className="btn btn-sm btn-outline-secondary me-1" onClick={() => startEdit(rule)}>
                        Edit
                      </button>
                      <button type="button" className="btn btn-sm btn-outline-danger" onClick={() => deleteRule(rule)}>
                        Delete
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {editingRuleId === null && (
          <button type="button" className="btn btn-outline-primary btn-sm" onClick={startCreate}>
            <i className="bi bi-plus-lg me-1" aria-hidden="true" />
            Add Rule
          </button>
        )}

        {editingRuleId !== null && (
          <div className="card card-body bg-body-tertiary">
            <h6>{editingRuleId === "new" ? "New Rule" : "Edit Rule"}</h6>

            {formError && <div className="alert alert-danger py-2">{formError}</div>}

            <div className="row g-3 mb-2">
              <div className="col-md-5">
                <label className="form-label">Territory</label>
                <select
                  className="form-select"
                  value={form.territoryId ?? ""}
                  onChange={(e) => setForm((f) => ({ ...f, territoryId: e.target.value || null }))}
                >
                  <option value="">Tenant-wide fallback (no territory)</option>
                  {territories.map((t) => (
                    <option key={t.id} value={t.id} disabled={usedTerritoryIds.has(t.id)}>
                      {t.name}
                      {usedTerritoryIds.has(t.id) ? " (already has a rule)" : ""}
                    </option>
                  ))}
                </select>
              </div>
              <div className="col-md-3 d-flex align-items-end">
                <div className="form-check form-switch">
                  <input
                    className="form-check-input"
                    type="checkbox"
                    role="switch"
                    id="ruleIsActive"
                    checked={form.isActive}
                    onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))}
                  />
                  <label className="form-check-label" htmlFor="ruleIsActive">
                    Active
                  </label>
                </div>
              </div>
            </div>

            <label className="form-label">Eligible users (round-robin pool)</label>
            <div className="d-flex flex-wrap gap-2 mb-3" style={{ maxHeight: 220, overflowY: "auto" }}>
              {users.map((u) => {
                const selected = form.eligibleUserIds.includes(u.id);
                return (
                  <button
                    key={u.id}
                    type="button"
                    className={`btn btn-sm ${selected ? "btn-primary" : "btn-outline-secondary"}`}
                    onClick={() => toggleUser(u.id)}
                  >
                    {selected && <i className="bi bi-check-lg me-1" aria-hidden="true" />}
                    {u.fullName}
                  </button>
                );
              })}
              {users.length === 0 && <span className="text-muted small">No active users found.</span>}
            </div>

            <div className="d-flex gap-2">
              <button type="button" className="btn btn-primary btn-sm" onClick={saveRule} disabled={isSavingRule}>
                {isSavingRule ? "Saving..." : "Save Rule"}
              </button>
              <button type="button" className="btn btn-outline-secondary btn-sm" onClick={cancelEdit}>
                Cancel
              </button>
            </div>
          </div>
        )}
      </FormSection>

      <div className="mt-3">
        <button type="button" className="btn btn-outline-secondary" onClick={() => navigate("/leads")}>
          Back to Leads
        </button>
      </div>
    </div>
  );
}
