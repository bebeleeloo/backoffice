import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { http, HttpResponse } from "msw";
import { renderWithProviders } from "../renderWithProviders";
import { UpstreamsPage } from "@/pages/UpstreamsPage";
import { ALL_PERMISSIONS } from "../factories";
import { server } from "../msw/server";

describe("UpstreamsPage", () => {
  it("renders the page title after loading", async () => {
    server.use(
      http.get("/api/v1/config/upstreams", () => HttpResponse.json({})),
    );
    renderWithProviders(<UpstreamsPage />, { permissions: ALL_PERMISSIONS });

    expect(await screen.findByText("Upstreams")).toBeInTheDocument();
  });

  it("shows Add Upstream button", async () => {
    server.use(
      http.get("/api/v1/config/upstreams", () => HttpResponse.json({})),
    );
    renderWithProviders(<UpstreamsPage />, { permissions: ALL_PERMISSIONS });

    expect(await screen.findByRole("button", { name: /add upstream/i })).toBeInTheDocument();
  });

  it("shows History button when user has audit.read permission", async () => {
    server.use(
      http.get("/api/v1/config/upstreams", () => HttpResponse.json({})),
    );
    renderWithProviders(<UpstreamsPage />, { permissions: ALL_PERMISSIONS });

    expect(await screen.findByRole("button", { name: /history/i })).toBeInTheDocument();
  });

  it("hides History button without audit.read permission", async () => {
    server.use(
      http.get("/api/v1/config/upstreams", () => HttpResponse.json({})),
    );
    renderWithProviders(<UpstreamsPage />, { permissions: ["settings.manage"] });

    await screen.findByText("Upstreams");
    expect(screen.queryByRole("button", { name: /history/i })).not.toBeInTheDocument();
  });

  it("shows loading spinner while fetching data", () => {
    server.use(
      http.get("/api/v1/config/upstreams", () => new Promise(() => {})),
    );
    renderWithProviders(<UpstreamsPage />, { permissions: ALL_PERMISSIONS });

    expect(screen.getByRole("progressbar")).toBeInTheDocument();
  });

  it("renders no upstream cards when empty", async () => {
    server.use(
      http.get("/api/v1/config/upstreams", () => HttpResponse.json({})),
    );
    renderWithProviders(<UpstreamsPage />, { permissions: ALL_PERMISSIONS });

    await screen.findByText("Upstreams");
    expect(screen.queryByText("core-api")).not.toBeInTheDocument();
  });
});
