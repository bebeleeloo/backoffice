import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { http, HttpResponse } from "msw";
import { renderWithProviders } from "../renderWithProviders";
import { MenuEditorPage } from "@/pages/MenuEditorPage";
import { ALL_PERMISSIONS } from "../factories";
import { server } from "../msw/server";

describe("MenuEditorPage", () => {
  it("renders the page title after loading", async () => {
    server.use(
      http.get("/api/v1/config/menu/raw", () => HttpResponse.json([])),
      http.get("/api/v1/permissions", () => HttpResponse.json([])),
    );
    renderWithProviders(<MenuEditorPage />, { permissions: ALL_PERMISSIONS });

    expect(await screen.findByText("Menu Editor")).toBeInTheDocument();
  });

  it("shows Add Item button", async () => {
    server.use(
      http.get("/api/v1/config/menu/raw", () => HttpResponse.json([])),
      http.get("/api/v1/permissions", () => HttpResponse.json([])),
    );
    renderWithProviders(<MenuEditorPage />, { permissions: ALL_PERMISSIONS });

    expect(await screen.findByRole("button", { name: /add item/i })).toBeInTheDocument();
  });

  it("shows History button when user has audit.read permission", async () => {
    server.use(
      http.get("/api/v1/config/menu/raw", () => HttpResponse.json([])),
      http.get("/api/v1/permissions", () => HttpResponse.json([])),
    );
    renderWithProviders(<MenuEditorPage />, { permissions: ALL_PERMISSIONS });

    expect(await screen.findByRole("button", { name: /history/i })).toBeInTheDocument();
  });

  it("hides History button without audit.read permission", async () => {
    server.use(
      http.get("/api/v1/config/menu/raw", () => HttpResponse.json([])),
      http.get("/api/v1/permissions", () => HttpResponse.json([])),
    );
    renderWithProviders(<MenuEditorPage />, { permissions: ["settings.manage"] });

    await screen.findByText("Menu Editor");
    expect(screen.queryByRole("button", { name: /history/i })).not.toBeInTheDocument();
  });

  it("shows loading spinner while fetching data", () => {
    server.use(
      http.get("/api/v1/config/menu/raw", () => new Promise(() => {})),
    );
    renderWithProviders(<MenuEditorPage />, { permissions: ALL_PERMISSIONS });

    expect(screen.getByRole("progressbar")).toBeInTheDocument();
  });
});
