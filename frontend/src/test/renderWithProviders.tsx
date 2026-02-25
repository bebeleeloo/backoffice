import { type ReactNode } from "react";
import { render, type RenderOptions } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "@mui/material/styles";
import { LocalizationProvider } from "@mui/x-date-pickers/LocalizationProvider";
import { AdapterDayjs } from "@mui/x-date-pickers/AdapterDayjs";
import { MemoryRouter, type MemoryRouterProps } from "react-router-dom";
import { AuthContext } from "@/auth/AuthContext";
import { createAppTheme } from "@/theme";
import type { UserProfile } from "@/api/types";
import { buildUserProfile } from "./factories";

interface Options extends Omit<RenderOptions, "wrapper"> {
  /** Override the authenticated user (null = unauthenticated) */
  user?: UserProfile | null;
  /** Override permissions list */
  permissions?: string[];
  /** Simulate auth loading state */
  isLoading?: boolean;
  /** MemoryRouter props (initialEntries, etc.) */
  routerProps?: MemoryRouterProps;
}

export function renderWithProviders(ui: ReactNode, options: Options = {}) {
  const {
    user: userOverride,
    permissions: permissionsOverride,
    isLoading = false,
    routerProps,
    ...renderOptions
  } = options;

  const userProfile = userOverride === null ? null : (userOverride ?? buildUserProfile());
  const permissions = permissionsOverride ?? userProfile?.permissions ?? [];

  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
        refetchOnWindowFocus: false,
        refetchOnReconnect: false,
      },
      mutations: { retry: false },
    },
  });

  const authValue = {
    user: userProfile,
    isAuthenticated: !!userProfile,
    isLoading,
    permissions,
    login: async () => {},
    logout: () => {},
    refreshProfile: async () => {},
  };

  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <ThemeProvider theme={createAppTheme("light")}>
          <LocalizationProvider dateAdapter={AdapterDayjs}>
            <AuthContext.Provider value={authValue}>
              <MemoryRouter {...routerProps}>{children}</MemoryRouter>
            </AuthContext.Provider>
          </LocalizationProvider>
        </ThemeProvider>
      </QueryClientProvider>
    );
  }

  return {
    user: userEvent.setup(),
    ...render(ui, { wrapper: Wrapper, ...renderOptions }),
  };
}
