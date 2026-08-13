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
    const axiosError = error as AxiosError<
      | ApiResponse<T>
      | { message?: string; title?: string; detail?: string; errors?: string[] }
    >;
    const payload = axiosError.response?.data as
      | ApiResponse<T>
      | { message?: string; title?: string; detail?: string; errors?: string[] }
      | undefined;
    const message =
      typeof payload?.message === "string"
        ? payload.message
        : typeof (payload as { title?: string } | undefined)?.title === "string"
          ? (payload as { title?: string }).title!
          : typeof (payload as { detail?: string } | undefined)?.detail ===
              "string"
            ? (payload as { detail?: string }).detail!
            : Array.isArray(payload?.errors) && payload.errors.length > 0
              ? payload.errors[0]
              : axiosError.message === "Network Error"
                ? "Unable to reach the server. Please try again."
                : axiosError.message ||
                  "Unable to reach the server. Please try again.";

    return {
      success: false,
      message,
      errors: Array.isArray(payload?.errors) ? payload.errors : undefined,
    };
  }
}
