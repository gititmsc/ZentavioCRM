/**
 * Shared helper for calling ZentavioCRM.Api endpoints that return the standard
 * ApiResponse<T> envelope, normalizing network/validation failures into the same shape
 * so pages never need to catch a raw AxiosError.
 */
import type { AxiosError } from "axios";
import type { ApiResponse } from "@/services/authService";

export async function callApi<T>(request: Promise<{ data: ApiResponse<T> }>): Promise<ApiResponse<T>> {
  try {
    const response = await request;
    return response.data;
  } catch (error) {
    const axiosError = error as AxiosError<ApiResponse<T>>;
    return {
      success: false,
      message: axiosError.response?.data?.message ?? "Unable to reach the server. Please try again.",
      errors: axiosError.response?.data?.errors,
    };
  }
}
