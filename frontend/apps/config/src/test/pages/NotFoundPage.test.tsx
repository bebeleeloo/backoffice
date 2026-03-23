import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithProviders } from "../renderWithProviders";
import { NotFoundPage } from "@/pages/NotFoundPage";

describe("NotFoundPage", () => {
  it("renders 404 message", () => {
    renderWithProviders(<NotFoundPage />);

    expect(screen.getByText("404")).toBeInTheDocument();
    expect(screen.getByText("Page not found")).toBeInTheDocument();
  });

  it("has a button to go to dashboard", () => {
    renderWithProviders(<NotFoundPage />);

    expect(screen.getByRole("button", { name: /go to dashboard/i })).toBeInTheDocument();
  });
});
