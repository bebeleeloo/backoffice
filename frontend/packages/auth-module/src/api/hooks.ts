import { useMutation, useQuery, useQueryClient, keepPreviousData } from "@tanstack/react-query";
import { apiClient } from "@broker/ui-kit";
import type { AuthResponse, PagedResult, UserProfile } from "@broker/ui-kit";
import type {
  UserDto, RoleDto, PermissionDto,
  CreateUserRequest, UpdateUserRequest,
  CreateRoleRequest, UpdateRoleRequest,
  UsersParams, RolesParams,
  ChangePasswordRequest, UpdateProfileRequest,
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

export const useResetUserPassword = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, newPassword }: { id: string; newPassword: string }) =>
      apiClient.post(`/users/${id}/reset-password`, { newPassword }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["users"] }),
    meta: { successMessage: "Password reset successfully" },
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
    meta: { successMessage: "Permissions updated" },
  });
};

// Permissions
export const usePermissions = () =>
  useQuery({
    queryKey: ["permissions"],
    queryFn: () =>
      apiClient.get<PermissionDto[]>("/permissions").then((r) => r.data),
    staleTime: 10 * 60 * 1000,
  });

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

// Photo
export const useUploadMyPhoto = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => {
      const form = new FormData();
      form.append("file", file);
      return apiClient.put("/auth/photo", form);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["me"] }),
    meta: { successMessage: "Photo updated" },
  });
};

export const useDeleteMyPhoto = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => apiClient.delete("/auth/photo"),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["me"] }),
    meta: { successMessage: "Photo removed" },
  });
};

export const useUploadUserPhoto = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, file }: { id: string; file: File }) => {
      const form = new FormData();
      form.append("file", file);
      return apiClient.put(`/users/${id}/photo`, form);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ["users"] }),
    meta: { successMessage: "Photo updated" },
  });
};

export const useDeleteUserPhoto = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/users/${id}/photo`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ["users"] }),
    meta: { successMessage: "Photo removed" },
  });
};
