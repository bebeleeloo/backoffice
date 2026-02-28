import { describe, it, expect } from "vitest";
import { extractErrorMessage } from "./extractErrorMessage";

function createAxiosError(
  message: string,
  status?: number,
  data?: Record<string, unknown>,
) {
  const error = Object.assign(new Error(message), {
    isAxiosError: true,
    response: status !== undefined ? { status, data: data ?? {} } : undefined,
  });
  return error;
}

describe("extractErrorMessage", () => {
  it("returns validation errors joined with '. ' when data.errors is present", () => {
    const error = createAxiosError("Request failed", 400, {
      errors: {
        Name: ["Name is required", "Name must be at least 2 characters"],
        Email: ["Invalid email"],
      },
    });

    const result = extractErrorMessage(error);
    expect(result).toBe(
      "Name is required. Name must be at least 2 characters. Invalid email",
    );
  });

  it("returns data.detail when present", () => {
    const error = createAxiosError("Request failed", 400, {
      detail: "Client with this email already exists.",
    });

    expect(extractErrorMessage(error)).toBe(
      "Client with this email already exists.",
    );
  });

  it("returns data.title when present and no detail", () => {
    const error = createAxiosError("Request failed", 400, {
      title: "Bad Request",
    });

    expect(extractErrorMessage(error)).toBe("Bad Request");
  });

  it("prefers errors over detail and title", () => {
    const error = createAxiosError("Request failed", 400, {
      errors: { Field: ["Field error"] },
      detail: "Some detail",
      title: "Some title",
    });

    expect(extractErrorMessage(error)).toBe("Field error");
  });

  it("prefers detail over title", () => {
    const error = createAxiosError("Request failed", 400, {
      detail: "Detailed message",
      title: "Title message",
    });

    expect(extractErrorMessage(error)).toBe("Detailed message");
  });

  it("returns conflict message for 409 status when no data content", () => {
    const error = createAxiosError("Request failed", 409);

    expect(extractErrorMessage(error)).toBe(
      "Conflict: the record was modified by another user.",
    );
  });

  it("returns rate limit message for 429 status when no data content", () => {
    const error = createAxiosError("Request failed", 429);

    expect(extractErrorMessage(error)).toBe(
      "Too many requests. Please wait and try again.",
    );
  });

  it("returns axiosError.message when response has no data", () => {
    const error = Object.assign(new Error("Network Error"), {
      isAxiosError: true,
      response: undefined,
    });

    expect(extractErrorMessage(error)).toBe("Network Error");
  });

  it("returns error.message for a regular (non-Axios) Error", () => {
    const error = new Error("Something went wrong");

    expect(extractErrorMessage(error)).toBe("Something went wrong");
  });

  it('returns "An unexpected error occurred." for unknown error types', () => {
    expect(extractErrorMessage("string error")).toBe(
      "An unexpected error occurred.",
    );
  });

  it('returns "An unexpected error occurred." for null', () => {
    expect(extractErrorMessage(null)).toBe("An unexpected error occurred.");
  });

  it('returns "An unexpected error occurred." for undefined', () => {
    expect(extractErrorMessage(undefined)).toBe(
      "An unexpected error occurred.",
    );
  });

  it("skips empty errors object and falls through to detail", () => {
    const error = createAxiosError("Request failed", 400, {
      errors: {},
      detail: "Fallback detail",
    });

    expect(extractErrorMessage(error)).toBe("Fallback detail");
  });
});
