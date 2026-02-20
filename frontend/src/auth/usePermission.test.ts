import { renderHook } from "@testing-library/react";
import { type ReactNode, createElement } from "react";
import { AuthContext } from "./AuthContext";
import { useHasPermission } from "./usePermission";

function wrapper(permissions: string[]) {
  return ({ children }: { children: ReactNode }) =>
    createElement(AuthContext.Provider, {
      value: {
        user: null,
        isAuthenticated: false,
        isLoading: false,
        permissions,
        login: async () => {},
        logout: () => {},
      },
      children,
    });
}

describe("useHasPermission", () => {
  it("returns true when permission is present", () => {
    const { result } = renderHook(() => useHasPermission("users.read"), {
      wrapper: wrapper(["users.read", "roles.read"]),
    });
    expect(result.current).toBe(true);
  });

  it("returns false when permission is absent", () => {
    const { result } = renderHook(() => useHasPermission("users.delete"), {
      wrapper: wrapper(["users.read"]),
    });
    expect(result.current).toBe(false);
  });

  it("returns false for empty permissions", () => {
    const { result } = renderHook(() => useHasPermission("users.read"), {
      wrapper: wrapper([]),
    });
    expect(result.current).toBe(false);
  });
});
