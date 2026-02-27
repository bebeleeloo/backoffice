import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import { Routes, Route, MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ThemeProvider } from "@mui/material/styles";
import { render } from "@testing-library/react";
import { AuthContext } from "@/auth/AuthContext";
import { createAppTheme, createAppListTheme } from "@/theme";
import { ListThemeContext } from "@/theme/ThemeContext";
import { TradeTransactionDetailsPage } from "@/pages/TradeTransactionDetailsPage";
import { NonTradeTransactionDetailsPage } from "@/pages/NonTradeTransactionDetailsPage";
import { buildUserProfile, ALL_PERMISSIONS } from "./factories";
import type { TradeTransactionDto, NonTradeTransactionDto } from "@/api/types";

const TRADE_TRANSACTION: TradeTransactionDto = {
  id: "tt1",
  orderId: null,
  orderNumber: null,
  accountNumber: null,
  transactionNumber: "TT-20260101-ABCD1234",
  status: "Settled",
  transactionDate: "2026-01-01T00:00:00Z",
  instrumentId: "ins1",
  instrumentSymbol: "AAPL",
  instrumentName: "Apple Inc.",
  side: "Buy",
  quantity: 100,
  price: 150.50,
  commission: 5.00,
  settlementDate: "2026-01-03T00:00:00Z",
  venue: "NYSE",
  comment: null,
  externalId: null,
  createdAt: "2026-01-01T00:00:00Z",
  rowVersion: "AAAA",
};

const NON_TRADE_TRANSACTION: NonTradeTransactionDto = {
  id: "ntt1",
  orderId: null,
  orderNumber: null,
  accountNumber: null,
  transactionNumber: "NTT-20260101-ABCD1234",
  status: "Settled",
  transactionDate: "2026-01-01T00:00:00Z",
  amount: 5000.00,
  currencyId: "cur1",
  currencyCode: "USD",
  instrumentId: null,
  instrumentSymbol: null,
  instrumentName: null,
  referenceNumber: "REF-123456",
  description: "Deposit",
  processedAt: "2026-01-01T12:00:00Z",
  comment: null,
  externalId: null,
  createdAt: "2026-01-01T00:00:00Z",
  rowVersion: "BBBB",
};

function renderDetailPage(
  page: "trade" | "non-trade",
  id: string,
  permissions: string[],
  data: unknown,
) {
  const qc = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: Infinity, staleTime: Infinity },
      mutations: { retry: false },
    },
  });
  const cacheKey = page === "trade" ? "trade-transactions" : "non-trade-transactions";
  qc.setQueryData([cacheKey, id], data);

  const profile = buildUserProfile({ permissions });
  const theme = createAppTheme("light");
  const listTheme = createAppListTheme(theme);
  const path = page === "trade" ? "/trade-transactions" : "/non-trade-transactions";
  const Page = page === "trade" ? TradeTransactionDetailsPage : NonTradeTransactionDetailsPage;

  return render(
    <QueryClientProvider client={qc}>
      <ThemeProvider theme={theme}>
        <ListThemeContext.Provider value={listTheme}>
          <AuthContext.Provider value={{
            user: profile, isAuthenticated: true, isLoading: false,
            permissions,
            login: async () => {}, logout: () => {}, refreshProfile: async () => {},
          }}>
            <MemoryRouter initialEntries={[`${path}/${id}`]}>
              <Routes>
                <Route path={`${path}/:id`} element={<Page />} />
              </Routes>
            </MemoryRouter>
          </AuthContext.Provider>
        </ListThemeContext.Provider>
      </ThemeProvider>
    </QueryClientProvider>,
  );
}

describe("TradeTransactionDetailsPage", () => {
  it("renders trade transaction number in title", () => {
    renderDetailPage("trade", "tt1", ALL_PERMISSIONS, TRADE_TRANSACTION);

    expect(screen.getByRole("heading", { level: 1 })).toHaveTextContent("TT-20260101-ABCD1234");
  });

  it("shows status and side chips", () => {
    renderDetailPage("trade", "tt1", ALL_PERMISSIONS, TRADE_TRANSACTION);

    expect(screen.getByText("Settled")).toBeInTheDocument();
    expect(screen.getByText("Buy")).toBeInTheDocument();
  });

  it("shows instrument link", () => {
    renderDetailPage("trade", "tt1", ALL_PERMISSIONS, TRADE_TRANSACTION);

    expect(screen.getByText(/AAPL/)).toBeInTheDocument();
  });

  it("shows Edit button with transactions.update permission", () => {
    renderDetailPage("trade", "tt1", ALL_PERMISSIONS, TRADE_TRANSACTION);

    expect(screen.getByRole("button", { name: /edit/i })).toBeInTheDocument();
  });

  it("hides Edit button without transactions.update permission", () => {
    renderDetailPage("trade", "tt1", ["transactions.read"], TRADE_TRANSACTION);

    expect(screen.queryByRole("button", { name: /edit/i })).not.toBeInTheDocument();
  });

  it("shows History button with audit.read permission", () => {
    renderDetailPage("trade", "tt1", ALL_PERMISSIONS, TRADE_TRANSACTION);

    expect(screen.getByRole("button", { name: /history/i })).toBeInTheDocument();
  });

  it("hides History button without audit.read permission", () => {
    renderDetailPage("trade", "tt1", ["transactions.read", "transactions.update"], TRADE_TRANSACTION);

    expect(screen.queryByRole("button", { name: /history/i })).not.toBeInTheDocument();
  });
});

describe("NonTradeTransactionDetailsPage", () => {
  it("renders non-trade transaction number in title", () => {
    renderDetailPage("non-trade", "ntt1", ALL_PERMISSIONS, NON_TRADE_TRANSACTION);

    expect(screen.getByRole("heading", { level: 1 })).toHaveTextContent("NTT-20260101-ABCD1234");
  });

  it("shows status and currency", () => {
    renderDetailPage("non-trade", "ntt1", ALL_PERMISSIONS, NON_TRADE_TRANSACTION);

    expect(screen.getByText("Settled")).toBeInTheDocument();
    expect(screen.getByText("USD")).toBeInTheDocument();
  });

  it("shows Edit button with transactions.update permission", () => {
    renderDetailPage("non-trade", "ntt1", ALL_PERMISSIONS, NON_TRADE_TRANSACTION);

    expect(screen.getByRole("button", { name: /edit/i })).toBeInTheDocument();
  });

  it("hides Edit button without transactions.update permission", () => {
    renderDetailPage("non-trade", "ntt1", ["transactions.read"], NON_TRADE_TRANSACTION);

    expect(screen.queryByRole("button", { name: /edit/i })).not.toBeInTheDocument();
  });
});
