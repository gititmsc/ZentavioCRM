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

export interface FunnelStep {
  label: string;
  count: number;
}

export interface LeadSourceItem {
  source: string;
  count: number;
  convertedCount: number;
}

export interface TrendPoint {
  bucketStartUtc: string;
  wonCount: number;
  lostCount: number;
  wonRevenue: number;
}

export interface OwnerLeaderboardItem {
  userId: string | null;
  name: string;
  wonCount: number;
  lostCount: number;
  wonRevenue: number;
  leadsAssigned: number;
}

export interface TerritoryLeaderboardItem {
  territoryId: string | null;
  name: string;
  leadsCount: number;
  convertedCount: number;
  conversionRate: number;
}

export interface DashboardAnalytics {
  fromUtc: string;
  toUtc: string;
  mineOnly: boolean;
  granularity: "week" | "month";
  funnel: FunnelStep[];
  leadsBySource: LeadSourceItem[];
  trend: TrendPoint[];
  wonRevenueTotal: number;
  wonCount: number;
  lostCount: number;
  ownerLeaderboard: OwnerLeaderboardItem[];
  territoryLeaderboard: TerritoryLeaderboardItem[];
}

export interface AnalyticsParams {
  /** yyyy-MM-dd */
  from: string;
  /** yyyy-MM-dd, inclusive */
  to: string;
  mineOnly: boolean;
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

const getAnalytics = (params: AnalyticsParams) =>
  callApi<DashboardAnalytics>(apiClient.get("/api/dashboard/analytics", { params }));

export const dashboardService = { getSalesSummary, getUsage, getAnalytics };
