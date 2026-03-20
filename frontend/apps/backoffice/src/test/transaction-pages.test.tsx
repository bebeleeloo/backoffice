import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { renderWithProviders } from "./renderWithProviders";
import { TradeTransactionsPage } from "@/pages/TradeTransactionsPage";
import { NonTradeTransactionsPage } from "@/pages/NonTradeTransactionsPage";
import { ALL_PERMISSIONS } from "./factories";

describe("TradeTransactionsPage", () => {
  it("renders the page title", () => {
    renderWithProviders(<TradeTransactionsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByText("Trade Transactions")).toBeInTheDocument();
  });

  it("renders global search bar", () => {
    renderWithProviders(<TradeTransactionsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByPlaceholderText(/search trade transactions/i)).toBeInTheDocument();
  });

  it("shows Create button when user has transactions.create permission", () => {
    renderWithProviders(<TradeTransactionsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByRole("button", { name: /create/i })).toBeInTheDocument();
  });

  it("hides Create button without transactions.create permission", () => {
    renderWithProviders(<TradeTransactionsPage />, {
      permissions: ["transactions.read"],
    });

    expect(screen.queryByRole("button", { name: /create/i })).not.toBeInTheDocument();
  });

  it("shows Export button", () => {
    renderWithProviders(<TradeTransactionsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByTestId("FileDownloadIcon")).toBeInTheDocument();
  });
});

describe("NonTradeTransactionsPage", () => {
  it("renders the page title", () => {
    renderWithProviders(<NonTradeTransactionsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByText("Non-Trade Transactions")).toBeInTheDocument();
  });

  it("renders global search bar", () => {
    renderWithProviders(<NonTradeTransactionsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByPlaceholderText(/search non-trade transactions/i)).toBeInTheDocument();
  });

  it("shows Create button when user has transactions.create permission", () => {
    renderWithProviders(<NonTradeTransactionsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByRole("button", { name: /create/i })).toBeInTheDocument();
  });

  it("hides Create button without transactions.create permission", () => {
    renderWithProviders(<NonTradeTransactionsPage />, {
      permissions: ["transactions.read"],
    });

    expect(screen.queryByRole("button", { name: /create/i })).not.toBeInTheDocument();
  });

  it("shows Export button", () => {
    renderWithProviders(<NonTradeTransactionsPage />, {
      permissions: ALL_PERMISSIONS,
    });

    expect(screen.getByTestId("FileDownloadIcon")).toBeInTheDocument();
  });
});
