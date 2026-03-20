import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithProviders } from "./renderWithProviders";
import { TradeOrdersPage } from "@/pages/TradeOrdersPage";
import { NonTradeOrdersPage } from "@/pages/NonTradeOrdersPage";
import { ALL_PERMISSIONS } from "./factories";

describe("TradeOrdersPage", () => {
  it("renders the page title", () => {
    renderWithProviders(<TradeOrdersPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByText("Trade Orders")).toBeInTheDocument();
  });

  it("renders global search bar", () => {
    renderWithProviders(<TradeOrdersPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByPlaceholderText(/search trade orders/i)).toBeInTheDocument();
  });

  it("shows Create button when user has orders.create permission", () => {
    renderWithProviders(<TradeOrdersPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByRole("button", { name: /create/i })).toBeInTheDocument();
  });

  it("hides Create button without orders.create permission", () => {
    renderWithProviders(<TradeOrdersPage />, {
      permissions: ["orders.read"],
    });

    expect(screen.queryByRole("button", { name: /create/i })).not.toBeInTheDocument();
  });

  it("shows Export button", () => {
    renderWithProviders(<TradeOrdersPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByTestId("FileDownloadIcon")).toBeInTheDocument();
  });
});

describe("NonTradeOrdersPage", () => {
  it("renders the page title", () => {
    renderWithProviders(<NonTradeOrdersPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByText("Non-Trade Orders")).toBeInTheDocument();
  });

  it("renders global search bar", () => {
    renderWithProviders(<NonTradeOrdersPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByPlaceholderText(/search non-trade orders/i)).toBeInTheDocument();
  });

  it("shows Create button when user has orders.create permission", () => {
    renderWithProviders(<NonTradeOrdersPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByRole("button", { name: /create/i })).toBeInTheDocument();
  });

  it("hides Create button without orders.create permission", () => {
    renderWithProviders(<NonTradeOrdersPage />, {
      permissions: ["orders.read"],
    });

    expect(screen.queryByRole("button", { name: /create/i })).not.toBeInTheDocument();
  });

  it("shows Export button", () => {
    renderWithProviders(<NonTradeOrdersPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByTestId("FileDownloadIcon")).toBeInTheDocument();
  });
});
