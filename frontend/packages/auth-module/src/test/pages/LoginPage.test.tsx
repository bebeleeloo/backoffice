import { describe, it, expect, vi } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import { renderWithProviders } from "../renderWithProviders";
import { LoginPage } from "../../pages/LoginPage";

describe("LoginPage", () => {
  it("renders username and password fields", () => {
    renderWithProviders(<LoginPage />, { user: null });

    expect(screen.getByLabelText(/username/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument();
  });

  it("renders sign in button", () => {
    renderWithProviders(<LoginPage />, { user: null });

    expect(screen.getByRole("button", { name: /sign in/i })).toBeInTheDocument();
  });

  it("disables sign in button when fields are empty", () => {
    renderWithProviders(<LoginPage />, { user: null });

    expect(screen.getByRole("button", { name: /sign in/i })).toBeDisabled();
  });

  it("enables sign in button when fields are filled", async () => {
    const { user } = renderWithProviders(<LoginPage />, { user: null });

    await user.type(screen.getByLabelText(/username/i), "admin");
    await user.type(screen.getByLabelText(/password/i), "password");

    expect(screen.getByRole("button", { name: /sign in/i })).toBeEnabled();
  });

  it("shows error message on login failure", async () => {
    const loginMock = vi.fn().mockRejectedValue(new Error("Invalid credentials"));
    const { user } = renderWithProviders(<LoginPage />, {
      user: null,
      loginMock,
    });

    await user.type(screen.getByLabelText(/username/i), "admin");
    await user.type(screen.getByLabelText(/password/i), "wrong");
    await user.click(screen.getByRole("button", { name: /sign in/i }));

    await waitFor(() => {
      expect(screen.getByText(/invalid credentials/i)).toBeInTheDocument();
    });
  });

  it("renders welcome heading", () => {
    renderWithProviders(<LoginPage />, { user: null });

    expect(screen.getByText(/welcome back/i)).toBeInTheDocument();
  });
});
