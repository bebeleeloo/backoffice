import { describe, it, expect, vi } from "vitest";
import { renderHook } from "@testing-library/react";

vi.mock("./useAuth", () => ({
  useAuth: () => ({ permissions: ["clients.read", "users.read"] }),
}));

import { useHasPermission } from "./usePermission";

describe("useHasPermission", () => {
  it("returns true when the permission is in the list", () => {
    const { result } = renderHook(() => useHasPermission("clients.read"));
    expect(result.current).toBe(true);
  });

  it("returns true for another permission in the list", () => {
    const { result } = renderHook(() => useHasPermission("users.read"));
    expect(result.current).toBe(true);
  });

  it("returns false when the permission is not in the list", () => {
    const { result } = renderHook(() => useHasPermission("clients.delete"));
    expect(result.current).toBe(false);
  });

  it("returns false for a completely unrelated permission", () => {
    const { result } = renderHook(() => useHasPermission("settings.manage"));
    expect(result.current).toBe(false);
  });

  it("returns false for an empty string permission", () => {
    const { result } = renderHook(() => useHasPermission(""));
    expect(result.current).toBe(false);
  });
});
