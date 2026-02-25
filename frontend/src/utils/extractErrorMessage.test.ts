import { AxiosError, AxiosHeaders } from "axios";
import { extractErrorMessage } from "./extractErrorMessage";

function makeAxiosError(
  status: number | undefined,
  data: unknown,
  message = "Request failed",
): AxiosError {
  const headers = new AxiosHeaders();
  const error = new AxiosError(message, "ERR_BAD_REQUEST", undefined, undefined, status ? {
    status,
    statusText: "Error",
    headers: {},
    config: { headers },
    data,
  } : undefined);
  return error;
}

describe("extractErrorMessage", () => {
  it("returns joined error messages from ProblemDetails.errors", () => {
    const error = makeAxiosError(400, {
      errors: {
        Name: ["Name is required", "Name is too short"],
        Email: ["Email is invalid"],
      },
    });
    const result = extractErrorMessage(error);
    expect(result).toBe("Name is required. Name is too short. Email is invalid");
  });

  it("returns detail from ProblemDetails", () => {
    const error = makeAxiosError(400, { detail: "Entity not found" });
    expect(extractErrorMessage(error)).toBe("Entity not found");
  });

  it("returns title from ProblemDetails when no detail", () => {
    const error = makeAxiosError(400, { title: "Bad Request" });
    expect(extractErrorMessage(error)).toBe("Bad Request");
  });

  it("prefers errors over detail and title", () => {
    const error = makeAxiosError(400, {
      errors: { Field: ["Field error"] },
      detail: "Detail message",
      title: "Title message",
    });
    expect(extractErrorMessage(error)).toBe("Field error");
  });

  it("returns conflict message for 409 status", () => {
    const error = makeAxiosError(409, {});
    expect(extractErrorMessage(error)).toBe("Conflict: the record was modified by another user.");
  });

  it("returns rate limit message for 429 status", () => {
    const error = makeAxiosError(429, {});
    expect(extractErrorMessage(error)).toBe("Too many requests. Please wait and try again.");
  });

  it("returns axios message when no response data", () => {
    const error = makeAxiosError(undefined, undefined, "Network Error");
    expect(extractErrorMessage(error)).toBe("Network Error");
  });

  it("returns error.message for non-Axios Error", () => {
    const error = new Error("Something broke");
    expect(extractErrorMessage(error)).toBe("Something broke");
  });

  it("returns default message for unknown error type", () => {
    expect(extractErrorMessage("string error")).toBe("An unexpected error occurred.");
    expect(extractErrorMessage(42)).toBe("An unexpected error occurred.");
    expect(extractErrorMessage(null)).toBe("An unexpected error occurred.");
    expect(extractErrorMessage(undefined)).toBe("An unexpected error occurred.");
  });

  it("skips empty errors object and falls back to detail", () => {
    const error = makeAxiosError(400, {
      errors: {},
      detail: "Fallback detail",
    });
    expect(extractErrorMessage(error)).toBe("Fallback detail");
  });
});
