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

const getSalesSummary = () => callApi<SalesDashboardSummary>(apiClient.get("/api/dashboard/sales-summary"));

export const dashboardService = { getSalesSummary };
