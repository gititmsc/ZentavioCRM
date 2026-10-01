/**
 * Lead auto-assignment API — thin wrapper around ZentavioCRM.Api's LeadAssignmentController.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";

export interface LeadAssignmentSettings {
  autoAssignEnabled: boolean;
  updatedAtUtc: string | null;
}

export interface LeadAssignmentRuleUser {
  id: string;
  fullName: string;
}

export interface LeadAssignmentRule {
  id: string;
  territoryId: string | null;
  territoryName: string | null;
  isActive: boolean;
  eligibleUsers: LeadAssignmentRuleUser[];
  lastAssignedUserId: string | null;
  lastAssignedUserName: string | null;
  lastAssignedAtUtc: string | null;
  createdAtUtc: string;
}

export interface SaveLeadAssignmentRuleRequest {
  territoryId: string | null;
  isActive: boolean;
  eligibleUserIds: string[];
}

const getSettings = () => callApi<LeadAssignmentSettings>(apiClient.get("/api/lead-assignment/settings"));

const updateSettings = (request: LeadAssignmentSettings) =>
  callApi<LeadAssignmentSettings>(apiClient.put("/api/lead-assignment/settings", request));

const getRules = () => callApi<LeadAssignmentRule[]>(apiClient.get("/api/lead-assignment/rules"));

const createRule = (request: SaveLeadAssignmentRuleRequest) =>
  callApi<LeadAssignmentRule>(apiClient.post("/api/lead-assignment/rules", request));

const updateRule = (id: string, request: SaveLeadAssignmentRuleRequest) =>
  callApi<LeadAssignmentRule>(apiClient.put(`/api/lead-assignment/rules/${id}`, request));

const removeRule = (id: string) => callApi<boolean>(apiClient.delete(`/api/lead-assignment/rules/${id}`));

export const leadAssignmentService = { getSettings, updateSettings, getRules, createRule, updateRule, removeRule };
