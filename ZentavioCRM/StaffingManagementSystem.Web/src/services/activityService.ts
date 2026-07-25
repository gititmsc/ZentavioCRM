/**
 * Activities API — generic timeline (calls, emails, meetings, tasks, notes...) shared by
 * every CRM record. Wraps ZentavioCRM.Api's ActivitiesController.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";

export type ActivityType = "Call" | "Email" | "Meeting" | "Task" | "Note" | "Visit" | "WhatsApp" | "Sms";

export type RelatedEntityType = "Lead" | "Customer" | "Opportunity";

export interface Activity {
  id: string;
  type: ActivityType;
  subject: string;
  description: string | null;
  relatedToType: RelatedEntityType;
  relatedToId: string;
  dueAtUtc: string | null;
  completedAtUtc: string | null;
  assignedToUserId: string | null;
  assignedToUserName: string | null;
  createdByUserName: string | null;
  createdAtUtc: string;
}

export interface CreateActivityRequest {
  type: ActivityType;
  subject: string;
  description: string | null;
  dueAtUtc: string | null;
  assignedToUserId: string | null;
}

const getTimeline = (relatedToType: RelatedEntityType, relatedToId: string) =>
  callApi<Activity[]>(apiClient.get("/api/activities", { params: { relatedToType, relatedToId } }));

const create = (relatedToType: RelatedEntityType, relatedToId: string, request: CreateActivityRequest) =>
  callApi<Activity>(apiClient.post("/api/activities", request, { params: { relatedToType, relatedToId } }));

export const activityService = { getTimeline, create };
