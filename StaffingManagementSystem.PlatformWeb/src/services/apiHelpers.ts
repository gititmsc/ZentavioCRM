import type { AxiosError } from "axios";

export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data?: T;
  errors?: string[];
}

/** Wraps an axios call so callers always get an ApiResponse, even on network/HTTP failure. */
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
