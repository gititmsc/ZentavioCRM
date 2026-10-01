/**
 * Lead Scoring Settings API — thin wrapper around ZentavioCRM.Api's LeadsController
 * scoring-settings endpoints. Gated by Leads.ManageScoring.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";
import type { ApiResponse } from "@/services/authService";

export interface LeadScoringSettings {
  emailPresentPoints: number;
  mobilePresentPoints: number;
  industryPresentPoints: number;
  assignedPoints: number;
  expectedValueHighThreshold: number;
  expectedValueHighPoints: number;
  expectedValueMediumThreshold: number;
  expectedValueMediumPoints: number;
  expectedValueLowPoints: number;
  sourceReferralPoints: number;
  sourceWarmChannelPoints: number;
  urgentTimelinePoints: number;
  pointsPerCompletedActivity: number;
  engagementMaxPoints: number;
  maxScore: number;
  updatedAtUtc: string | null;
}

const get = () => callApi<LeadScoringSettings>(apiClient.get("/api/leads/scoring-settings"));

const update = (request: LeadScoringSettings) =>
  callApi<LeadScoringSettings>(apiClient.put("/api/leads/scoring-settings", request));

export const leadScoringSettingsService = { get, update };

export type { ApiResponse };
