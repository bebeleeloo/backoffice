import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { http, HttpResponse } from "msw";

vi.mock("@broker/ui-kit", async () => {
  const actual = await vi.importActual<typeof import("@broker/ui-kit")>("@broker/ui-kit");
  const { FilteredDataGrid } = await import("../mocks/FilteredDataGrid");
  return {
    ...actual,
    FilteredDataGrid,
  };
});

import { vi } from "vitest";
import { renderWithProviders } from "../renderWithProviders";
import { EntityFieldsPage } from "@/pages/EntityFieldsPage";
import { ALL_PERMISSIONS } from "../factories";
import { server } from "../msw/server";

describe("EntityFieldsPage", () => {
  it("renders the page title", () => {
    renderWithProviders(<EntityFieldsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByText("Entity Fields")).toBeInTheDocument();
  });

  it("shows grid columns for entity name, total fields, and used fields", async () => {
    renderWithProviders(<EntityFieldsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    await screen.findByTestId("datagrid");

    expect(screen.getByTestId("col-name")).toHaveTextContent("Entity");
    expect(screen.getByTestId("col-totalFields")).toHaveTextContent("Total Fields");
    expect(screen.getByTestId("col-usedFields")).toHaveTextContent("Used Fields");
  });

  it("shows History button when user has audit.read permission", () => {
    renderWithProviders(<EntityFieldsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByRole("button", { name: /history/i })).toBeInTheDocument();
  });

  it("hides History button without audit.read permission", () => {
    renderWithProviders(<EntityFieldsPage />, {
      permissions: ["settings.manage"],
    });

    expect(screen.queryByRole("button", { name: /history/i })).not.toBeInTheDocument();
  });

  it("shows search bar", () => {
    renderWithProviders(<EntityFieldsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByPlaceholderText(/search entities/i)).toBeInTheDocument();
  });

  it("shows export button", () => {
    renderWithProviders(<EntityFieldsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByTestId("FileDownloadIcon")).toBeInTheDocument();
  });

  it("handles empty entity list", async () => {
    server.use(
      http.get("/api/v1/config/entities/raw", () => HttpResponse.json([])),
    );

    renderWithProviders(<EntityFieldsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    await screen.findByTestId("datagrid");

    expect(screen.getByTestId("datagrid-info")).toHaveTextContent("rows: 0");
  });
});
