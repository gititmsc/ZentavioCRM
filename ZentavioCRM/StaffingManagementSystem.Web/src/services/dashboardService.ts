/**
 * Dashboard API — thin wrapper around ZentavioCRM.Api's DashboardController.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";
import type { OpportunityStage } from "@/services/opportunityService";

export interface StageBreakdownItem {
  stage: OpportunityStage;
  count: number;
  value: number;
}

export interface SalesDashboardSummary {
  openLeadsCount: number;
  activeCustomersCount: number;
  convertedLeadsThisMonthCount: number;
  pipelineValue: number;
  openOpportunitiesCount: number;
  winRatePercentage: number;
  stageBreakdown: StageBreakdownItem[];
}

export interface UsageSummary {
  userCount: number;
  maxUsers: number;
  recordCount: number;
  maxRecords: number;
  databaseSizeMB: number;
  maxStorageMB: number;
}

const getSalesSummary = () => callApi<SalesDashboardSummary>(apiClient.get("/api/dashboard/sales-summary"));

/** Self-service usage vs. plan limits for the current tenant — powers the in-app usage banner. */
const getUsage = () => callApi<UsageSummary>(apiClient.get("/api/dashboard/usage"));

export const dashboardService = { getSalesSummary, getUsage };
