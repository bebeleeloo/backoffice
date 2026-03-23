import { type ReactNode } from "react";
import { render, type RenderOptions, type RenderResult } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { UserEvent } from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "@mui/material/styles";
import { MemoryRouter, type MemoryRouterProps } from "react-router-dom";
import { AuthContext, ListThemeContext, createAppListTheme, createAppTheme } from "@broker/ui-kit";
import type { UserProfile } from "@broker/ui-kit";
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
  /** Override login mock */
  loginMock?: (username: string, password: string) => Promise<void>;
}

export function renderWithProviders(ui: ReactNode, options: Options = {}): RenderResult & { user: UserEvent } {
  const {
    user: userOverride,
    permissions: permissionsOverride,
    isLoading = false,
    routerProps,
    loginMock,
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

  const theme = createAppTheme("light");
  const listTheme = createAppListTheme(theme);

  const authValue = {
    user: userProfile,
    isAuthenticated: !!userProfile,
    isLoading,
    permissions,
    login: loginMock ?? (async () => {}),
    logout: () => {},
    refreshProfile: async () => {},
  };

  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={queryClient}>
        <ThemeProvider theme={theme}>
          <ListThemeContext.Provider value={listTheme}>
            <AuthContext.Provider value={authValue}>
              <MemoryRouter {...routerProps}>{children}</MemoryRouter>
            </AuthContext.Provider>
          </ListThemeContext.Provider>
        </ThemeProvider>
      </QueryClientProvider>
    );
  }

  return {
    user: userEvent.setup(),
    ...render(ui, { wrapper: Wrapper, ...renderOptions }),
  };
}
