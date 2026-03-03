import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithProviders } from "./renderWithProviders";
import { ClientsPage } from "@/pages/ClientsPage";
import { AccountsPage } from "@/pages/AccountsPage";
import { InstrumentsPage } from "@/pages/InstrumentsPage";
import { UsersPage } from "@/pages/UsersPage";
import { RolesPage } from "@/pages/RolesPage";
import { ALL_PERMISSIONS } from "./factories";

describe("ClientsPage", () => {
  it("renders the page title", () => {
    renderWithProviders(<ClientsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByText("Clients")).toBeInTheDocument();
  });

  it("renders global search bar", () => {
    renderWithProviders(<ClientsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByPlaceholderText(/search clients/i)).toBeInTheDocument();
  });

  it("shows Create button when user has clients.create permission", () => {
    renderWithProviders(<ClientsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByRole("button", { name: /create/i })).toBeInTheDocument();
  });

  it("hides Create button without clients.create permission", () => {
    renderWithProviders(<ClientsPage />, {
      permissions: ["clients.read"],
    });

    expect(screen.queryByRole("button", { name: /create/i })).not.toBeInTheDocument();
  });

  it("shows Export button", () => {
    renderWithProviders(<ClientsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByTestId("FileDownloadIcon")).toBeInTheDocument();
  });
});

describe("AccountsPage", () => {
  it("renders the page title", () => {
    renderWithProviders(<AccountsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByText("Accounts")).toBeInTheDocument();
  });

  it("renders global search bar", () => {
    renderWithProviders(<AccountsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByPlaceholderText(/search accounts/i)).toBeInTheDocument();
  });

  it("shows Create button when user has accounts.create permission", () => {
    renderWithProviders(<AccountsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByRole("button", { name: /create/i })).toBeInTheDocument();
  });

  it("hides Create button without accounts.create permission", () => {
    renderWithProviders(<AccountsPage />, {
      permissions: ["accounts.read"],
    });

    expect(screen.queryByRole("button", { name: /create/i })).not.toBeInTheDocument();
  });

  it("shows Export button", () => {
    renderWithProviders(<AccountsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByTestId("FileDownloadIcon")).toBeInTheDocument();
  });
});

describe("InstrumentsPage", () => {
  it("renders the page title", () => {
    renderWithProviders(<InstrumentsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByText("Instruments")).toBeInTheDocument();
  });

  it("renders global search bar", () => {
    renderWithProviders(<InstrumentsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByPlaceholderText(/search instruments/i)).toBeInTheDocument();
  });

  it("shows Create button when user has instruments.create permission", () => {
    renderWithProviders(<InstrumentsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByRole("button", { name: /create/i })).toBeInTheDocument();
  });

  it("hides Create button without instruments.create permission", () => {
    renderWithProviders(<InstrumentsPage />, {
      permissions: ["instruments.read"],
    });

    expect(screen.queryByRole("button", { name: /create/i })).not.toBeInTheDocument();
  });

  it("shows Export button", () => {
    renderWithProviders(<InstrumentsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByTestId("FileDownloadIcon")).toBeInTheDocument();
  });
});

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

  it("shows Create button when user has users.create permission", () => {
    renderWithProviders(<UsersPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByRole("button", { name: /create/i })).toBeInTheDocument();
  });

  it("hides Create button without users.create permission", () => {
    renderWithProviders(<UsersPage />, {
      permissions: ["users.read"],
    });

    expect(screen.queryByRole("button", { name: /create/i })).not.toBeInTheDocument();
  });

  it("shows Export button", () => {
    renderWithProviders(<UsersPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByTestId("FileDownloadIcon")).toBeInTheDocument();
  });
});

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

  it("shows Create button when user has roles.create permission", () => {
    renderWithProviders(<RolesPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByRole("button", { name: /create/i })).toBeInTheDocument();
  });

  it("hides Create button without roles.create permission", () => {
    renderWithProviders(<RolesPage />, {
      permissions: ["roles.read"],
    });

    expect(screen.queryByRole("button", { name: /create/i })).not.toBeInTheDocument();
  });

  it("shows Export button", () => {
    renderWithProviders(<RolesPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByTestId("FileDownloadIcon")).toBeInTheDocument();
  });
});
