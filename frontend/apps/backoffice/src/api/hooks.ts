import { useMutation, useQuery, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { apiClient, cleanParams } from "@broker/ui-kit";
import type { CountryDto, PagedResult } from "@broker/ui-kit";
import type {
  AuditLogDto, AuditParams,
  ClientListItemDto, ClientDto, CreateClientRequest, UpdateClientRequest, ClientsParams,
  AccountListItemDto, AccountDto, CreateAccountRequest, UpdateAccountRequest, AccountsParams,
  ClearerDto, TradePlatformDto,
  AccountHolderInput, ClientAccountDto, ClientAccountInput,
  ExchangeDto, CurrencyDto,
  InstrumentListItemDto, InstrumentDto, CreateInstrumentRequest, UpdateInstrumentRequest, InstrumentsParams,
  TradeOrderListItemDto, TradeOrderDto, CreateTradeOrderRequest, UpdateTradeOrderRequest, TradeOrdersParams,
  NonTradeOrderListItemDto, NonTradeOrderDto, CreateNonTradeOrderRequest, UpdateNonTradeOrderRequest, NonTradeOrdersParams,
  TradeTransactionListItemDto, TradeTransactionDto, CreateTradeTransactionRequest, UpdateTradeTransactionRequest, TradeTransactionsParams,
  NonTradeTransactionListItemDto, NonTradeTransactionDto, CreateNonTradeTransactionRequest, UpdateNonTradeTransactionRequest, NonTradeTransactionsParams,
  DashboardStatsDto,
  CreateClearerRequest, UpdateClearerRequest,
  CreateTradePlatformRequest, UpdateTradePlatformRequest,
  CreateExchangeRequest, UpdateExchangeRequest,
  CreateCurrencyRequest, UpdateCurrencyRequest,
} from "./types";

// Audit
const AUTH_ENTITY_TYPES = new Set(["User", "Role"]);

export const useAuditLogs = (params: AuditParams) => {
  const basePath = params.entityType && AUTH_ENTITY_TYPES.has(params.entityType)
    ? "/auth/audit"
    : "/audit";
  return useQuery({
    queryKey: ["audit", params],
    queryFn: () =>
      apiClient.get<PagedResult<AuditLogDto>>(basePath, { params }).then((r) => r.data),
    placeholderData: keepPreviousData,
  });
};

export const useAuditLog = (id: string, entityType?: string) => {
  const basePath = entityType && AUTH_ENTITY_TYPES.has(entityType)
    ? "/auth/audit"
    : "/audit";
  return useQuery({
    queryKey: ["audit", id],
    queryFn: () => apiClient.get<AuditLogDto>(`${basePath}/${id}`).then((r) => r.data),
    enabled: !!id,
  });
};

// Countries
export const useCountries = () =>
  useQuery({
    queryKey: ["countries"],
    queryFn: () =>
      apiClient.get<CountryDto[]>("/countries").then((r) => r.data),
    staleTime: 10 * 60 * 1000,
  });

// Clearers
export const useClearers = () =>
  useQuery({
    queryKey: ["clearers"],
    queryFn: () =>
      apiClient.get<ClearerDto[]>("/clearers").then((r) => r.data),
    staleTime: 10 * 60 * 1000,
  });

// Trade Platforms
export const useTradePlatforms = () =>
  useQuery({
    queryKey: ["trade-platforms"],
    queryFn: () =>
      apiClient.get<TradePlatformDto[]>("/trade-platforms").then((r) => r.data),
    staleTime: 10 * 60 * 1000,
  });


// Accounts
export const useAccounts = (params: AccountsParams) =>
  useQuery({
    queryKey: ["accounts", params],
    queryFn: () =>
      apiClient.get<PagedResult<AccountListItemDto>>("/accounts", { params: cleanParams(params as Record<string, unknown>) }).then((r) => r.data),
    placeholderData: keepPreviousData,
  });

export const useAccount = (id: string) =>
  useQuery({
    queryKey: ["accounts", id],
    queryFn: () => apiClient.get<AccountDto>(`/accounts/${id}`).then((r) => r.data),
    enabled: !!id,
  });

export const useCreateAccount = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateAccountRequest) =>
      apiClient.post<AccountDto>("/accounts", data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["accounts"] }),
    meta: { successMessage: "Account created" },
  });
};

export const useUpdateAccount = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateAccountRequest) =>
      apiClient.put<AccountDto>(`/accounts/${data.id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["accounts"] }),
    meta: { successMessage: "Account updated" },
  });
};

export const useDeleteAccount = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/accounts/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["accounts"] }),
    meta: { successMessage: "Account deleted" },
  });
};

export const useSetAccountHolders = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ accountId, holders }: { accountId: string; holders: AccountHolderInput[] }) =>
      apiClient.put<AccountDto>(`/accounts/${accountId}/holders`, holders).then((r) => r.data),
    onSuccess: (_data, vars) => {
      qc.invalidateQueries({ queryKey: ["accounts", vars.accountId] });
      qc.invalidateQueries({ queryKey: ["client-accounts"] });
    },
    meta: { successMessage: "Account holders updated" },
  });
};

export const useClientAccounts = (clientId: string) =>
  useQuery({
    queryKey: ["client-accounts", clientId],
    queryFn: () =>
      apiClient.get<ClientAccountDto[]>(`/clients/${clientId}/accounts`).then((r) => r.data),
    enabled: !!clientId,
  });

export const useSetClientAccounts = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ clientId, accounts }: { clientId: string; accounts: ClientAccountInput[] }) =>
      apiClient.put<ClientAccountDto[]>(`/clients/${clientId}/accounts`, accounts).then((r) => r.data),
    onSuccess: (_data, vars) => {
      qc.invalidateQueries({ queryKey: ["client-accounts", vars.clientId] });
      qc.invalidateQueries({ queryKey: ["accounts"] });
    },
    meta: { successMessage: "Client accounts updated" },
  });
};

// Clients
export const useClients = (params: ClientsParams) =>
  useQuery({
    queryKey: ["clients", params],
    queryFn: () =>
      apiClient.get<PagedResult<ClientListItemDto>>("/clients", { params: cleanParams(params as Record<string, unknown>) }).then((r) => r.data),
    placeholderData: keepPreviousData,
  });

export const useClient = (id: string) =>
  useQuery({
    queryKey: ["clients", id],
    queryFn: () => apiClient.get<ClientDto>(`/clients/${id}`).then((r) => r.data),
    enabled: !!id,
  });

export const useCreateClient = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateClientRequest) =>
      apiClient.post<ClientDto>("/clients", data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["clients"] }),
    meta: { successMessage: "Client created" },
  });
};

export const useUpdateClient = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateClientRequest) =>
      apiClient.put<ClientDto>(`/clients/${data.id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["clients"] }),
    meta: { successMessage: "Client updated" },
  });
};

export const useDeleteClient = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/clients/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["clients"] }),
    meta: { successMessage: "Client deleted" },
  });
};

// Exchanges
export const useExchanges = () =>
  useQuery({
    queryKey: ["exchanges"],
    queryFn: () =>
      apiClient.get<ExchangeDto[]>("/exchanges").then((r) => r.data),
    staleTime: 10 * 60 * 1000,
  });

// Currencies
export const useCurrencies = () =>
  useQuery({
    queryKey: ["currencies"],
    queryFn: () =>
      apiClient.get<CurrencyDto[]>("/currencies").then((r) => r.data),
    staleTime: 10 * 60 * 1000,
  });

// Instruments
export const useInstruments = (params: InstrumentsParams) =>
  useQuery({
    queryKey: ["instruments", params],
    queryFn: () =>
      apiClient.get<PagedResult<InstrumentListItemDto>>("/instruments", { params: cleanParams(params as Record<string, unknown>) }).then((r) => r.data),
    placeholderData: keepPreviousData,
  });

export const useInstrument = (id: string) =>
  useQuery({
    queryKey: ["instruments", id],
    queryFn: () => apiClient.get<InstrumentDto>(`/instruments/${id}`).then((r) => r.data),
    enabled: !!id,
  });

export const useCreateInstrument = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateInstrumentRequest) =>
      apiClient.post<InstrumentDto>("/instruments", data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["instruments"] }),
    meta: { successMessage: "Instrument created" },
  });
};

export const useUpdateInstrument = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateInstrumentRequest) =>
      apiClient.put<InstrumentDto>(`/instruments/${data.id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["instruments"] }),
    meta: { successMessage: "Instrument updated" },
  });
};

export const useDeleteInstrument = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/instruments/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["instruments"] }),
    meta: { successMessage: "Instrument deleted" },
  });
};

// Trade Orders
export const useTradeOrders = (params: TradeOrdersParams) =>
  useQuery({
    queryKey: ["trade-orders", params],
    queryFn: () =>
      apiClient.get<PagedResult<TradeOrderListItemDto>>("/trade-orders", { params: cleanParams(params as Record<string, unknown>) }).then((r) => r.data),
    placeholderData: keepPreviousData,
  });

export const useTradeOrder = (id: string) =>
  useQuery({
    queryKey: ["trade-orders", id],
    queryFn: () => apiClient.get<TradeOrderDto>(`/trade-orders/${id}`).then((r) => r.data),
    enabled: !!id,
  });

export const useCreateTradeOrder = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateTradeOrderRequest) =>
      apiClient.post<TradeOrderDto>("/trade-orders", data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["trade-orders"] }),
    meta: { successMessage: "Trade order created" },
  });
};

export const useUpdateTradeOrder = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateTradeOrderRequest) =>
      apiClient.put<TradeOrderDto>(`/trade-orders/${data.id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["trade-orders"] }),
    meta: { successMessage: "Trade order updated" },
  });
};

export const useDeleteTradeOrder = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/trade-orders/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["trade-orders"] }),
    meta: { successMessage: "Trade order deleted" },
  });
};

// Non-Trade Orders
export const useNonTradeOrders = (params: NonTradeOrdersParams) =>
  useQuery({
    queryKey: ["non-trade-orders", params],
    queryFn: () =>
      apiClient.get<PagedResult<NonTradeOrderListItemDto>>("/non-trade-orders", { params: cleanParams(params as Record<string, unknown>) }).then((r) => r.data),
    placeholderData: keepPreviousData,
  });

export const useNonTradeOrder = (id: string) =>
  useQuery({
    queryKey: ["non-trade-orders", id],
    queryFn: () => apiClient.get<NonTradeOrderDto>(`/non-trade-orders/${id}`).then((r) => r.data),
    enabled: !!id,
  });

export const useCreateNonTradeOrder = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateNonTradeOrderRequest) =>
      apiClient.post<NonTradeOrderDto>("/non-trade-orders", data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["non-trade-orders"] }),
    meta: { successMessage: "Non-trade order created" },
  });
};

export const useUpdateNonTradeOrder = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateNonTradeOrderRequest) =>
      apiClient.put<NonTradeOrderDto>(`/non-trade-orders/${data.id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["non-trade-orders"] }),
    meta: { successMessage: "Non-trade order updated" },
  });
};

export const useDeleteNonTradeOrder = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/non-trade-orders/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["non-trade-orders"] }),
    meta: { successMessage: "Non-trade order deleted" },
  });
};

// Reference Data — Clearers
export const useAllClearers = () =>
  useQuery({
    queryKey: ["clearers", "all"],
    queryFn: () => apiClient.get<ClearerDto[]>("/clearers/all").then((r) => r.data),
    staleTime: 10 * 60 * 1000,
  });

export const useCreateClearer = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateClearerRequest) =>
      apiClient.post<ClearerDto>("/clearers", data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["clearers"] }),
    meta: { successMessage: "Clearer created" },
  });
};

export const useUpdateClearer = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateClearerRequest) =>
      apiClient.put<ClearerDto>(`/clearers/${data.id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["clearers"] }),
    meta: { successMessage: "Clearer updated" },
  });
};

export const useDeleteClearer = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/clearers/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["clearers"] }),
    meta: { successMessage: "Clearer deleted" },
  });
};

// Reference Data — Trade Platforms
export const useAllTradePlatforms = () =>
  useQuery({
    queryKey: ["trade-platforms", "all"],
    queryFn: () => apiClient.get<TradePlatformDto[]>("/trade-platforms/all").then((r) => r.data),
    staleTime: 10 * 60 * 1000,
  });

export const useCreateTradePlatform = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateTradePlatformRequest) =>
      apiClient.post<TradePlatformDto>("/trade-platforms", data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["trade-platforms"] }),
    meta: { successMessage: "Trade platform created" },
  });
};

export const useUpdateTradePlatform = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateTradePlatformRequest) =>
      apiClient.put<TradePlatformDto>(`/trade-platforms/${data.id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["trade-platforms"] }),
    meta: { successMessage: "Trade platform updated" },
  });
};

export const useDeleteTradePlatform = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/trade-platforms/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["trade-platforms"] }),
    meta: { successMessage: "Trade platform deleted" },
  });
};

// Reference Data — Exchanges
export const useAllExchanges = () =>
  useQuery({
    queryKey: ["exchanges", "all"],
    queryFn: () => apiClient.get<ExchangeDto[]>("/exchanges/all").then((r) => r.data),
    staleTime: 10 * 60 * 1000,
  });

export const useCreateExchange = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateExchangeRequest) =>
      apiClient.post<ExchangeDto>("/exchanges", data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["exchanges"] }),
    meta: { successMessage: "Exchange created" },
  });
};

export const useUpdateExchange = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateExchangeRequest) =>
      apiClient.put<ExchangeDto>(`/exchanges/${data.id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["exchanges"] }),
    meta: { successMessage: "Exchange updated" },
  });
};

export const useDeleteExchange = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/exchanges/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["exchanges"] }),
    meta: { successMessage: "Exchange deleted" },
  });
};

// Reference Data — Currencies
export const useAllCurrencies = () =>
  useQuery({
    queryKey: ["currencies", "all"],
    queryFn: () => apiClient.get<CurrencyDto[]>("/currencies/all").then((r) => r.data),
    staleTime: 10 * 60 * 1000,
  });

export const useCreateCurrency = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateCurrencyRequest) =>
      apiClient.post<CurrencyDto>("/currencies", data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["currencies"] }),
    meta: { successMessage: "Currency created" },
  });
};

export const useUpdateCurrency = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateCurrencyRequest) =>
      apiClient.put<CurrencyDto>(`/currencies/${data.id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["currencies"] }),
    meta: { successMessage: "Currency updated" },
  });
};

export const useDeleteCurrency = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/currencies/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["currencies"] }),
    meta: { successMessage: "Currency deleted" },
  });
};

// Dashboard
export const useDashboardStats = () =>
  useQuery({
    queryKey: ["dashboard-stats"],
    queryFn: () =>
      apiClient.get<DashboardStatsDto>("/dashboard/stats").then((r) => r.data),
  });

// Trade Transactions
export const useTradeTransactions = (params: TradeTransactionsParams) =>
  useQuery({
    queryKey: ["trade-transactions", params],
    queryFn: () =>
      apiClient.get<PagedResult<TradeTransactionListItemDto>>("/trade-transactions", { params: cleanParams(params as Record<string, unknown>) }).then((r) => r.data),
    placeholderData: keepPreviousData,
  });

export const useTradeTransaction = (id: string) =>
  useQuery({
    queryKey: ["trade-transactions", id],
    queryFn: () => apiClient.get<TradeTransactionDto>(`/trade-transactions/${id}`).then((r) => r.data),
    enabled: !!id,
  });

export const useTradeTransactionsByOrder = (orderId: string) =>
  useQuery({
    queryKey: ["trade-transactions", "by-order", orderId],
    queryFn: () => apiClient.get<TradeTransactionListItemDto[]>(`/trade-transactions/by-order/${orderId}`).then((r) => r.data),
    enabled: !!orderId,
  });

export const useCreateTradeTransaction = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateTradeTransactionRequest) =>
      apiClient.post<TradeTransactionDto>("/trade-transactions", data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["trade-transactions"] }),
    meta: { successMessage: "Trade transaction created" },
  });
};

export const useUpdateTradeTransaction = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateTradeTransactionRequest) =>
      apiClient.put<TradeTransactionDto>(`/trade-transactions/${data.id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["trade-transactions"] }),
    meta: { successMessage: "Trade transaction updated" },
  });
};

export const useDeleteTradeTransaction = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/trade-transactions/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["trade-transactions"] }),
    meta: { successMessage: "Trade transaction deleted" },
  });
};

// Non-Trade Transactions
export const useNonTradeTransactions = (params: NonTradeTransactionsParams) =>
  useQuery({
    queryKey: ["non-trade-transactions", params],
    queryFn: () =>
      apiClient.get<PagedResult<NonTradeTransactionListItemDto>>("/non-trade-transactions", { params: cleanParams(params as Record<string, unknown>) }).then((r) => r.data),
    placeholderData: keepPreviousData,
  });

export const useNonTradeTransaction = (id: string) =>
  useQuery({
    queryKey: ["non-trade-transactions", id],
    queryFn: () => apiClient.get<NonTradeTransactionDto>(`/non-trade-transactions/${id}`).then((r) => r.data),
    enabled: !!id,
  });

export const useNonTradeTransactionsByOrder = (orderId: string) =>
  useQuery({
    queryKey: ["non-trade-transactions", "by-order", orderId],
    queryFn: () => apiClient.get<NonTradeTransactionListItemDto[]>(`/non-trade-transactions/by-order/${orderId}`).then((r) => r.data),
    enabled: !!orderId,
  });

export const useCreateNonTradeTransaction = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateNonTradeTransactionRequest) =>
      apiClient.post<NonTradeTransactionDto>("/non-trade-transactions", data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["non-trade-transactions"] }),
    meta: { successMessage: "Non-trade transaction created" },
  });
};

export const useUpdateNonTradeTransaction = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateNonTradeTransactionRequest) =>
      apiClient.put<NonTradeTransactionDto>(`/non-trade-transactions/${data.id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["non-trade-transactions"] }),
    meta: { successMessage: "Non-trade transaction updated" },
  });
};

export const useDeleteNonTradeTransaction = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/non-trade-transactions/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["non-trade-transactions"] }),
    meta: { successMessage: "Non-trade transaction deleted" },
  });
};
