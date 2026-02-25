/**
 * Regression test: Edit dialogs must populate form when data is already cached.
 * Bug: on detail pages, useClient/useAccount/useInstrument data is already cached.
 * When the Edit dialog mounts with open=true, useState(fullData) captures the
 * cached reference, so the "prev !== current" check never fires → form stays empty.
 */
import { describe, it, expect } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useState, type ReactNode } from "react";
import { ThemeProvider } from "@mui/material/styles";
import { MemoryRouter } from "react-router-dom";
import { AuthContext } from "@/auth/AuthContext";
import { createAppTheme } from "@/theme";
import { buildUserProfile, ALL_PERMISSIONS } from "./factories";
import { EditClientDialog } from "@/pages/ClientDialogs";
import { EditAccountDialog } from "@/pages/AccountDialogs";
import { EditInstrumentDialog } from "@/pages/InstrumentDialogs";
import { render } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ClientDto, AccountDto, InstrumentDto } from "@/api/types";

/* ── test data ── */

const CLIENT: ClientDto = {
  id: "c-1", clientType: "Individual", externalId: null, status: "Active",
  email: "john@test.com", phone: "+1234", preferredLanguage: null, timeZone: null,
  residenceCountryId: null, residenceCountryIso2: null, residenceCountryName: null, residenceCountryFlagEmoji: null,
  citizenshipCountryId: null, citizenshipCountryIso2: null, citizenshipCountryName: null, citizenshipCountryFlagEmoji: null,
  pepStatus: false, riskLevel: null, kycStatus: "Approved", kycReviewedAtUtc: null,
  firstName: "John", lastName: "Doe", middleName: null, dateOfBirth: null,
  gender: null, maritalStatus: null, education: null,
  ssn: null, passportNumber: null, driverLicenseNumber: null,
  companyName: null, registrationNumber: null, taxId: null,
  createdAt: "2025-01-01T00:00:00Z", rowVersion: "AAAA",
  addresses: [], investmentProfile: null,
};

const ACCOUNT: AccountDto = {
  id: "a-1", number: "ACC-001",
  clearerId: null, clearerName: null,
  tradePlatformId: null, tradePlatformName: null,
  status: "Active", accountType: "Individual", marginType: "Cash",
  optionLevel: "Level0", tariff: "Basic", deliveryType: null,
  openedAt: null, closedAt: null, comment: null, externalId: null,
  createdAt: "2025-01-01T00:00:00Z", rowVersion: "BBBB",
  holders: [],
};

const INSTRUMENT: InstrumentDto = {
  id: "i-1", symbol: "AAPL", name: "Apple Inc.",
  isin: null, cusip: null,
  type: "Stock", assetClass: "Equities", status: "Active",
  exchangeId: null, exchangeCode: null, exchangeName: null,
  currencyId: null, currencyCode: null,
  countryId: null, countryName: null, countryFlagEmoji: null,
  sector: null, lotSize: 1, tickSize: null, marginRequirement: null,
  isMarginEligible: true,
  listingDate: null, delistingDate: null, expirationDate: null,
  issuerName: null, description: null, externalId: null,
  createdAt: "2025-01-01T00:00:00Z", rowVersion: "CCCC",
};

/* ── helpers ── */

function createPrimedQueryClient() {
  const qc = new QueryClient({
    defaultOptions: {
      queries: { retry: false, gcTime: Infinity, staleTime: Infinity },
      mutations: { retry: false },
    },
  });
  // Pre-populate cache to simulate detail page already having fetched data
  qc.setQueryData(["clients", "c-1"], CLIENT);
  qc.setQueryData(["client-accounts", "c-1"], []);
  qc.setQueryData(["accounts", "a-1"], ACCOUNT);
  qc.setQueryData(["instruments", "i-1"], INSTRUMENT);
  // Ref data
  qc.setQueryData(["countries"], []);
  qc.setQueryData(["clearers"], []);
  qc.setQueryData(["trade-platforms"], []);
  qc.setQueryData(["exchanges"], []);
  qc.setQueryData(["currencies"], []);
  qc.setQueryData(["clients", { page: 1, pageSize: 200 }], { items: [], totalCount: 0, page: 1, pageSize: 200, totalPages: 0 });
  qc.setQueryData(["accounts", { page: 1, pageSize: 200 }], { items: [], totalCount: 0, page: 1, pageSize: 200, totalPages: 0 });
  return qc;
}

function Wrapper({ children, qc }: { children: ReactNode; qc: QueryClient }) {
  const profile = buildUserProfile({ permissions: ALL_PERMISSIONS });
  return (
    <QueryClientProvider client={qc}>
      <ThemeProvider theme={createAppTheme("light")}>
        <AuthContext.Provider value={{
          user: profile, isAuthenticated: true, isLoading: false,
          permissions: ALL_PERMISSIONS,
          login: async () => {}, logout: () => {}, refreshProfile: async () => {},
        }}>
          <MemoryRouter>{children}</MemoryRouter>
        </AuthContext.Provider>
      </ThemeProvider>
    </QueryClientProvider>
  );
}

/* ── Harness components that simulate detail page → click Edit ── */

function ClientEditHarness({ qc }: { qc: QueryClient }) {
  const [open, setOpen] = useState(false);
  return (
    <Wrapper qc={qc}>
      <button data-testid="open-edit" onClick={() => setOpen(true)}>Edit</button>
      <EditClientDialog open={open} onClose={() => setOpen(false)} clientId="c-1" />
    </Wrapper>
  );
}

function AccountEditHarness({ qc }: { qc: QueryClient }) {
  const [open, setOpen] = useState(false);
  return (
    <Wrapper qc={qc}>
      <button data-testid="open-edit" onClick={() => setOpen(true)}>Edit</button>
      <EditAccountDialog open={open} onClose={() => setOpen(false)} account={{ id: "a-1" }} />
    </Wrapper>
  );
}

function InstrumentEditHarness({ qc }: { qc: QueryClient }) {
  const [open, setOpen] = useState(false);
  return (
    <Wrapper qc={qc}>
      <button data-testid="open-edit" onClick={() => setOpen(true)}>Edit</button>
      <EditInstrumentDialog open={open} onClose={() => setOpen(false)} instrument={{ id: "i-1", symbol: "AAPL" }} />
    </Wrapper>
  );
}

/* ── Tests ── */

describe("Edit dialogs populate form when data is pre-cached", () => {
  it("EditClientDialog shows client email after opening", async () => {
    const qc = createPrimedQueryClient();
    const user = userEvent.setup();
    render(<ClientEditHarness qc={qc} />);

    await user.click(screen.getByTestId("open-edit"));

    await waitFor(() => {
      expect(screen.getByDisplayValue("john@test.com")).toBeInTheDocument();
    });
    // Also check name fields
    expect(screen.getByDisplayValue("John")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Doe")).toBeInTheDocument();
  });

  it("EditAccountDialog shows account number after opening", async () => {
    const qc = createPrimedQueryClient();
    const user = userEvent.setup();
    render(<AccountEditHarness qc={qc} />);

    await user.click(screen.getByTestId("open-edit"));

    await waitFor(() => {
      expect(screen.getByDisplayValue("ACC-001")).toBeInTheDocument();
    });
  });

  it("EditInstrumentDialog shows instrument symbol after opening", async () => {
    const qc = createPrimedQueryClient();
    const user = userEvent.setup();
    render(<InstrumentEditHarness qc={qc} />);

    await user.click(screen.getByTestId("open-edit"));

    await waitFor(() => {
      expect(screen.getByDisplayValue("AAPL")).toBeInTheDocument();
    });
    expect(screen.getByDisplayValue("Apple Inc.")).toBeInTheDocument();
  });

  it("EditClientDialog re-populates on second open (close + re-open)", async () => {
    const qc = createPrimedQueryClient();
    const user = userEvent.setup();
    render(<ClientEditHarness qc={qc} />);

    // First open
    await user.click(screen.getByTestId("open-edit"));
    await waitFor(() => {
      expect(screen.getByDisplayValue("john@test.com")).toBeInTheDocument();
    });

    // Close via Cancel
    await user.click(screen.getByRole("button", { name: /cancel/i }));
    await waitFor(() => {
      expect(screen.queryByDisplayValue("john@test.com")).not.toBeInTheDocument();
    });

    // Second open — should still populate
    await user.click(screen.getByTestId("open-edit"));
    await waitFor(() => {
      expect(screen.getByDisplayValue("john@test.com")).toBeInTheDocument();
    });
  });
});
