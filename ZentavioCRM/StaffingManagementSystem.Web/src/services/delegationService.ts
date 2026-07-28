/**
 * Out-of-office delegation API — thin wrapper around ZentavioCRM.Api's UserDelegationsController.
 * Self-service only: every delegation is created FROM the current authenticated user.
 */
import { apiClient } from "@/services/apiClient";
import { callApi } from "@/services/apiHelpers";

export interface UserDelegation {
  id: string;
  delegatorUserId: string;
  delegatorUserName: string;
  delegateUserId: string;
  delegateUserName: string;
  startDateUtc: string;
  endDateUtc: string;
  notes: string | null;
  isActive: boolean;
  createdAtUtc: string;
}

export interface SaveUserDelegationRequest {
  delegateUserId: string;
  startDateUtc: string;
  endDateUtc: string;
  notes: string | null;
}

const getMine = () => callApi<UserDelegation[]>(apiClient.get("/api/user-delegations/mine"));

const create = (request: SaveUserDelegationRequest) =>
  callApi<UserDelegation>(apiClient.post("/api/user-delegations", request));

const remove = (id: string) => callApi<boolean>(apiClient.delete(`/api/user-delegations/${id}`));

export const delegationService = { getMine, create, remove };
