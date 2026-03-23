import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithProviders } from "../renderWithProviders";
import { RolesPage } from "../../pages/RolesPage";
import { ALL_PERMISSIONS } from "../factories";

describe("RolesPage", () => {
  it("renders the page title", () => {
    renderWithProviders(<RolesPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByText("Roles")).toBeInTheDocument();
  });

  it("renders global search bar", () => {
    renderWithProviders(<RolesPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByPlaceholderText(/search roles/i)).toBeInTheDocument();
  });

  it("shows Create Role button when user has roles.create permission", () => {
    renderWithProviders(<RolesPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByRole("button", { name: /create role/i })).toBeInTheDocument();
  });

  it("hides Create Role button without roles.create permission", () => {
    renderWithProviders(<RolesPage />, {
      permissions: ["roles.read"],
    });

    expect(screen.queryByRole("button", { name: /create role/i })).not.toBeInTheDocument();
  });

  it("shows Export button", () => {
    renderWithProviders(<RolesPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByTestId("FileDownloadIcon")).toBeInTheDocument();
  });

  it("shows History button when user has audit.read permission", () => {
    renderWithProviders(<RolesPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByRole("button", { name: /history/i })).toBeInTheDocument();
  });

  it("hides History button without audit.read permission", () => {
    renderWithProviders(<RolesPage />, {
      permissions: ["roles.read", "roles.create"],
    });

    expect(screen.queryByRole("button", { name: /history/i })).not.toBeInTheDocument();
  });
});
