import type { AxiosError } from "axios";

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

export function extractErrorMessage(error: unknown): string {
  if (error instanceof Error && "isAxiosError" in error) {
    const axiosError = error as AxiosError<ProblemDetails>;
    const data = axiosError.response?.data;

    if (data) {
      if (data.errors) {
        const messages = Object.values(data.errors).flat();
        if (messages.length > 0) return messages.join(". ");
      }
      if (data.detail) return data.detail;
      if (data.title) return data.title;
    }

    if (axiosError.response?.status === 409) return "Conflict: the record was modified by another user.";
    if (axiosError.response?.status === 429) return "Too many requests. Please wait and try again.";
    if (axiosError.message) return axiosError.message;
  }

  if (error instanceof Error) return error.message;
  return "An unexpected error occurred.";
}
