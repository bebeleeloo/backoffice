import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
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
} from "./types";

// Auth
export const useLogin = () =>
  useMutation({
    mutationFn: (creds: { username: string; password: string }) =>
      apiClient.post<AuthResponse>("/auth/login", creds).then((r) => r.data),
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
  });
};

export const useUpdateUser = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateUserRequest) =>
      apiClient.put<UserDto>(`/users/${data.id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["users"] }),
  });
};

export const useDeleteUser = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/users/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["users"] }),
  });
};

// Roles
export const useRoles = (params: RolesParams) =>
  useQuery({
    queryKey: ["roles", params],
    queryFn: () =>
      apiClient.get<PagedResult<RoleDto>>("/roles", { params }).then((r) => r.data),
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
  });
};

export const useUpdateRole = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateRoleRequest) =>
      apiClient.put<RoleDto>(`/roles/${data.id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["roles"] }),
  });
};

export const useDeleteRole = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/roles/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["roles"] }),
  });
};

export const useSetRolePermissions = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ roleId, permissionIds }: { roleId: string; permissionIds: string[] }) =>
      apiClient.put<RoleDto>(`/roles/${roleId}/permissions`, permissionIds).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["roles"] }),
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
  });
};

export const useUpdateAccount = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateAccountRequest) =>
      apiClient.put<AccountDto>(`/accounts/${data.id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["accounts"] }),
  });
};

export const useDeleteAccount = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/accounts/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["accounts"] }),
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
  });
};

// Clients
export const useClients = (params: ClientsParams) =>
  useQuery({
    queryKey: ["clients", params],
    queryFn: () =>
      apiClient.get<PagedResult<ClientListItemDto>>("/clients", { params: cleanParams(params as Record<string, unknown>) }).then((r) => r.data),
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
  });
};

export const useUpdateClient = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateClientRequest) =>
      apiClient.put<ClientDto>(`/clients/${data.id}`, data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["clients"] }),
  });
};

export const useDeleteClient = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/clients/${id}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["clients"] }),
  });
};
