import { AxiosError } from "axios";
import type { ApiResponse } from "@/services/authService";

/** Runs an API call and normalizes both success and failure into an ApiResponse, so callers
 * never need their own try/catch around every request — mirrors the tenant app's apiHelpers.ts. */
export async function callApi<T>(request: () => Promise<{ data: ApiResponse<T> }>): Promise<ApiResponse<T>> {
  try {
    const response = await request();
    return response.data;
  } catch (error) {
    const axiosError = error as AxiosError<ApiResponse<T>>;
    const apiMessage = axiosError.response?.data?.message;
    return {
      success: false,
      message: apiMessage ?? "Unable to reach the server. Please try again.",
      errors: axiosError.response?.data?.errors,
    };
  }
}
