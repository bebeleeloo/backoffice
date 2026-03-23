import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { render } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "@mui/material/styles";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { AuthContext, ListThemeContext, createAppListTheme, createAppTheme } from "@broker/ui-kit";
import { RoleDetailsPage } from "../../pages/RoleDetailsPage";
import { ALL_PERMISSIONS, buildUserProfile } from "../factories";

function renderRoleDetails(roleId: string, permissions: string[] = ALL_PERMISSIONS) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: 0 },
      mutations: { retry: false },
    },
  });

  const theme = createAppTheme("light");
  const listTheme = createAppListTheme(theme);
  const userProfile = buildUserProfile({ permissions });

  const authValue = {
    user: userProfile,
    isAuthenticated: true,
    isLoading: false,
    permissions,
    login: async () => {},
    logout: () => {},
    refreshProfile: async () => {},
  };

  return render(
    <QueryClientProvider client={queryClient}>
      <ThemeProvider theme={theme}>
        <ListThemeContext.Provider value={listTheme}>
          <AuthContext.Provider value={authValue}>
            <MemoryRouter initialEntries={[`/roles/${roleId}`]}>
              <Routes>
                <Route path="/roles/:id" element={<RoleDetailsPage />} />
              </Routes>
            </MemoryRouter>
          </AuthContext.Provider>
        </ListThemeContext.Provider>
      </ThemeProvider>
    </QueryClientProvider>,
  );
}

describe("RoleDetailsPage", () => {
  it("renders loading state initially", () => {
    renderRoleDetails("r1");

    expect(screen.getByRole("progressbar")).toBeInTheDocument();
  });

  it("shows not-found state for missing role", async () => {
    renderRoleDetails("nonexistent");

    const notFound = await screen.findByText("Role not found.");
    expect(notFound).toBeInTheDocument();
  });

  it("shows return to roles link when role not found", async () => {
    renderRoleDetails("nonexistent");

    const link = await screen.findByText("Return to Roles list");
    expect(link).toBeInTheDocument();
    expect(link).toHaveAttribute("href", "/roles");
  });
});
