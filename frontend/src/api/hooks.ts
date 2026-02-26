import { useMutation, useQuery, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { apiClient } from "./client";
import type {
  AuthResponse, UserProfile, PagedResult, UserDto, RoleDto,
  PermissionDto, AuditLogDto, CreateUserRequest, UpdateUserRequest,
  CreateRoleRequest, UpdateRoleRequest, UsersParams, RolesParams, AuditParams,
  ClientListItemDto, ClientDto, CreateClientRequest, UpdateClientRequest, ClientsParams,
  CountryDto,
  AccountListItemDto, AccountDto, CreateAccountRequest, UpdateAccountRequest, AccountsParams,
  ClearerDto, TradePlatformDto,
  AccountHolderInput, ClientAccountDto, ClientAccountInput,
  ExchangeDto, CurrencyDto,
  InstrumentListItemDto, InstrumentDto, CreateInstrumentRequest, UpdateInstrumentRequest, InstrumentsParams,
  TradeOrderListItemDto, TradeOrderDto, CreateTradeOrderRequest, UpdateTradeOrderRequest, TradeOrdersParams,
  NonTradeOrderListItemDto, NonTradeOrderDto, CreateNonTradeOrderRequest, UpdateNonTradeOrderRequest, NonTradeOrdersParams,
  OperationDto, EntityChangesParams,
  GlobalOperationDto, AllEntityChangesParams,
  DashboardStatsDto,
  ChangePasswordRequest, UpdateProfileRequest,
  CreateClearerRequest, UpdateClearerRequest,
  CreateTradePlatformRequest, UpdateTradePlatformRequest,
  CreateExchangeRequest, UpdateExchangeRequest,
  CreateCurrencyRequest, UpdateCurrencyRequest,
} from "./types";

// Auth
export const useLogin = () =>
  useMutation({
    mutationFn: (creds: { username: string; password: string }) =>
      apiClient.post<AuthResponse>("/auth/login", creds).then((r) => r.data),
    meta: { skipErrorToast: true },
  });

export const useMe = (enabled: boolean) =>
  useQuery({
    queryKey: ["me"],
    queryFn: () => apiClient.get<UserProfile>("/auth/me").then((r) => r.data),
    enabled,
    retry: false,
  });

// Users
export const useUsers = (params: UsersParams) =>
  useQuery({
    queryKey: ["users", params],
    queryFn: () =>
      apiClient.get<PagedResult<UserDto>>("/users", { params }).then((r) => r.data),
    placeholderData: keepPreviousData,
  });

export const useUser = (id: string) =>
  useQuery({
    queryKey: ["users", id],
    queryFn: () => apiClient.get<UserDto>(`/users/${id}`).then((r) => r.data),
    enabled: !!id,
  });

export const useCreateUser = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateUserRequest) =>
      apiClient.post<UserDto>("/users", data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["users"] }),
    meta: { successMessage: "User created" },
  });
};

export const useUpdateUser = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateUserRequest) =>
      apiClient.put<UserDto>(`/users/${data.id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["users"] }),
    meta: { successMessage: "User updated" },
  });
};

export const useDeleteUser = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/users/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["users"] }),
    meta: { successMessage: "User deleted" },
  });
};

// Roles
export const useRoles = (params: RolesParams) =>
  useQuery({
    queryKey: ["roles", params],
    queryFn: () =>
      apiClient.get<PagedResult<RoleDto>>("/roles", { params }).then((r) => r.data),
    placeholderData: keepPreviousData,
  });

export const useRole = (id: string) =>
  useQuery({
    queryKey: ["roles", id],
    queryFn: () => apiClient.get<RoleDto>(`/roles/${id}`).then((r) => r.data),
    enabled: !!id,
  });

export const useCreateRole = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateRoleRequest) =>
      apiClient.post<RoleDto>("/roles", data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["roles"] }),
    meta: { successMessage: "Role created" },
  });
};

export const useUpdateRole = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateRoleRequest) =>
      apiClient.put<RoleDto>(`/roles/${data.id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["roles"] }),
    meta: { successMessage: "Role updated" },
  });
};

export const useDeleteRole = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/roles/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["roles"] }),
    meta: { successMessage: "Role deleted" },
  });
};

export const useSetRolePermissions = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ roleId, permissionIds }: { roleId: string; permissionIds: string[] }) =>
      apiClient.put<RoleDto>(`/roles/${roleId}/permissions`, permissionIds).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["roles"] }),
    meta: {},
  });
};

// Permissions
export const usePermissions = () =>
  useQuery({
    queryKey: ["permissions"],
    queryFn: () =>
      apiClient.get<PermissionDto[]>("/permissions").then((r) => r.data),
  });

// Audit
export const useAuditLogs = (params: AuditParams) =>
  useQuery({
    queryKey: ["audit", params],
    queryFn: () =>
      apiClient.get<PagedResult<AuditLogDto>>("/audit", { params }).then((r) => r.data),
    placeholderData: keepPreviousData,
  });

export const useAuditLog = (id: string) =>
  useQuery({
    queryKey: ["audit", id],
    queryFn: () => apiClient.get<AuditLogDto>(`/audit/${id}`).then((r) => r.data),
    enabled: !!id,
  });

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

// Helpers
function cleanParams(params: Record<string, unknown>): Record<string, unknown> {
  const result: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === "") continue;
    if (Array.isArray(value) && value.length === 0) continue;
    result[key] = value;
  }
  return result;
}

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
    meta: {},
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
    meta: {},
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

// Profile
export const useChangePassword = () =>
  useMutation({
    mutationFn: (data: ChangePasswordRequest) =>
      apiClient.post("/auth/change-password", data),
    meta: { successMessage: "Password changed successfully" },
  });

export const useUpdateProfile = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateProfileRequest) =>
      apiClient.put<UserProfile>("/auth/profile", data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["me"] }),
    meta: { successMessage: "Profile updated" },
  });
};

// Reference Data — Clearers
export const useAllClearers = () =>
  useQuery({
    queryKey: ["clearers", "all"],
    queryFn: () => apiClient.get<ClearerDto[]>("/clearers/all").then((r) => r.data),
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

// Entity Changes
export const useEntityChanges = (params: EntityChangesParams, enabled = true) =>
  useQuery({
    queryKey: ["entity-changes", params],
    queryFn: () =>
      apiClient.get<PagedResult<OperationDto>>("/entity-changes", { params }).then((r) => r.data),
    enabled: enabled && !!params.entityId,
    placeholderData: keepPreviousData,
  });

export const useAllEntityChanges = (params: AllEntityChangesParams) =>
  useQuery({
    queryKey: ["entity-changes-all", params],
    queryFn: () =>
      apiClient.get<PagedResult<GlobalOperationDto>>("/entity-changes/all", { params: cleanParams(params as Record<string, unknown>) }).then((r) => r.data),
    placeholderData: keepPreviousData,
  });
