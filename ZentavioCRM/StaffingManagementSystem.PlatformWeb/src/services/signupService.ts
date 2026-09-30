/**
 * Public, unauthenticated tenant self-service signup.
 * Calls ZentavioCRM.Api -> POST /api/platform/signup (SignupController -> ITenantProvisioningService).
 * Always lands on the Trial plan — there is no plan-tier field to send here.
 */
import { AxiosError } from "axios";
import { apiClient } from "@/services/apiClient";
import type { ApiResponse } from "@/services/authService";

export interface SignupRequest {
  companyName: string;
  subdomain: string;
  adminFirstName: string;
  adminLastName?: string;
  adminEmail: string;
  adminPassword: string;
}

export interface SignupResult {
  subdomain: string;
}

async function signup(request: SignupRequest): Promise<ApiResponse<SignupResult>> {
  try {
    const response = await apiClient.post<ApiResponse<{ subdomain: string }>>("/api/platform/signup", request);

    if (!response.data.success || !response.data.data) {
      return {
        success: false,
        message: response.data.message || "Could not create your account.",
        errors: response.data.errors,
      };
    }

    return { success: true, message: response.data.message, data: { subdomain: response.data.data.subdomain } };
  } catch (error) {
    const axiosError = error as AxiosError<ApiResponse<unknown>>;
    const apiMessage = axiosError.response?.data?.message;

    if (axiosError.response?.status === 429) {
      return {
        success: false,
        message: "Too many signup attempts from this network. Please try again later.",
      };
    }

    return {
      success: false,
      message: apiMessage ?? "Unable to reach the server. Please try again.",
      errors: axiosError.response?.data?.errors,
    };
  }
}

export const signupService = { signup };
