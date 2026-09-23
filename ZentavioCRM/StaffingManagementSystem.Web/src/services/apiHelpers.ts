/**
 * Shared helper for calling ZentavioCRM.Api endpoints that return the standard
 * ApiResponse<T> envelope, normalizing network/validation failures into the same shape
 * so pages never need to catch a raw AxiosError.
 */
import type { AxiosError } from "axios";
import type { ApiResponse } from "@/services/authService";

/** Shape of ASP.NET Core's automatic [ApiController] model-validation 400 response — distinct from this app's own ApiResponse<T> envelope. Its `errors` is a field-keyed dictionary, not a flat array. */
interface ValidationProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export async function callApi<T>(request: Promise<{ data: ApiResponse<T> }>): Promise<ApiResponse<T>> {
  try {
    const response = await request;
    return response.data;
  } catch (error) {
    const axiosError = error as AxiosError<ApiResponse<T> | ValidationProblemDetails>;
    const payload = axiosError.response?.data;
    const appResponse = payload as ApiResponse<T> | undefined;
    const problemDetails = payload as ValidationProblemDetails | undefined;

    // Only a genuine field-keyed dictionary counts as fieldErrors — this app's own ApiResponse<T>
    // also has an `errors` property, but it's a flat string[], which the isArray check excludes.
    const fieldErrors =
      problemDetails?.errors && !Array.isArray(problemDetails.errors) ? problemDetails.errors : undefined;
    const firstFieldErrorMessage = fieldErrors ? Object.values(fieldErrors).flat()[0] : undefined;

    const message =
      typeof appResponse?.message === "string"
        ? appResponse.message
        : firstFieldErrorMessage ??
          (typeof problemDetails?.title === "string"
            ? problemDetails.title
            : typeof problemDetails?.detail === "string"
              ? problemDetails.detail
              : Array.isArray(appResponse?.errors) && appResponse.errors.length > 0
                ? appResponse.errors[0]
                : axiosError.message === "Network Error"
                  ? "Unable to reach the server. Please try again."
                  : axiosError.message || "Unable to reach the server. Please try again.");

    return {
      success: false,
      message,
      errors: Array.isArray(appResponse?.errors) ? appResponse.errors : undefined,
      fieldErrors,
    };
  }
}
