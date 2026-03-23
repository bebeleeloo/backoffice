import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithProviders } from "../renderWithProviders";
import { UsersPage } from "../../pages/UsersPage";
import { ALL_PERMISSIONS } from "../factories";

describe("UsersPage", () => {
  it("renders the page title", () => {
    renderWithProviders(<UsersPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByText("Users")).toBeInTheDocument();
  });

  it("renders global search bar", () => {
    renderWithProviders(<UsersPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByPlaceholderText(/search users/i)).toBeInTheDocument();
  });

  it("shows Create User button when user has users.create permission", () => {
    renderWithProviders(<UsersPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByRole("button", { name: /create user/i })).toBeInTheDocument();
  });

  it("hides Create User button without users.create permission", () => {
    renderWithProviders(<UsersPage />, {
      permissions: ["users.read"],
    });

    expect(screen.queryByRole("button", { name: /create user/i })).not.toBeInTheDocument();
  });

  it("shows Export button", () => {
    renderWithProviders(<UsersPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByTestId("FileDownloadIcon")).toBeInTheDocument();
  });
});
