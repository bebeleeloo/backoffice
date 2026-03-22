export interface UserDto {
  id: string;
  username: string;
  email: string;
  fullName: string | null;
  isActive: boolean;
  hasPhoto: boolean;
  roles: string[];
  createdAt: string;
  rowVersion: number;
}

export interface RoleDto {
  id: string;
  name: string;
  description: string | null;
  isSystem: boolean;
  permissions: string[];
  createdAt: string;
  rowVersion: number;
}

export interface PermissionDto {
  id: string;
  code: string;
  name: string;
  description: string | null;
  group: string;
}

export interface CreateUserRequest {
  username: string;
  email: string;
  password: string;
  fullName?: string;
  isActive: boolean;
  roleIds: string[];
}

export interface UpdateUserRequest {
  id: string;
  email: string;
  fullName?: string;
  isActive: boolean;
  roleIds: string[];
  rowVersion: number;
}

export interface CreateRoleRequest {
  name: string;
  description?: string;
}

export interface UpdateRoleRequest {
  id: string;
  name: string;
  description?: string;
  rowVersion: number;
}

export interface UsersParams {
  page?: number;
  pageSize?: number;
  sort?: string;
  q?: string;
  isActive?: boolean;
  username?: string;
  email?: string;
  fullName?: string;
  role?: string;
}

export interface RolesParams {
  page?: number;
  pageSize?: number;
  sort?: string;
  q?: string;
  name?: string;
  description?: string;
  isSystem?: boolean;
  permission?: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface UpdateProfileRequest {
  fullName?: string;
  email: string;
}

export interface ResetPasswordRequest {
  newPassword: string;
}

export interface SetRolePermissionsRequest {
  roleId: string;
  permissionIds: string[];
}
