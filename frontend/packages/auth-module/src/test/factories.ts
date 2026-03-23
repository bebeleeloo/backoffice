import { faker } from "@faker-js/faker";
import type { UserProfile, PagedResult } from "@broker/ui-kit";
import type { UserDto, RoleDto, PermissionDto } from "../api/types";

export const ALL_PERMISSIONS = [
  "users.read", "users.create", "users.update", "users.delete",
  "users.reset-password",
  "roles.read", "roles.create", "roles.update", "roles.delete",
  "clients.read", "clients.create", "clients.update", "clients.delete",
  "accounts.read", "accounts.create", "accounts.update", "accounts.delete",
  "instruments.read", "instruments.create", "instruments.update", "instruments.delete",
  "orders.read", "orders.create", "orders.update", "orders.delete",
  "transactions.read", "transactions.create", "transactions.update", "transactions.delete",
  "audit.read",
  "permissions.read",
  "settings.manage",
];

export function buildUserProfile(overrides: Partial<UserProfile> = {}): UserProfile {
  return {
    id: faker.string.uuid(),
    username: "admin",
    email: "admin@test.com",
    fullName: "Test Admin",
    hasPhoto: false,
    roles: ["Admin"],
    permissions: ALL_PERMISSIONS,
    scopes: [],
    ...overrides,
  };
}

export function buildUserDto(overrides: Partial<UserDto> = {}): UserDto {
  return {
    id: faker.string.uuid(),
    username: faker.internet.username(),
    email: faker.internet.email(),
    fullName: faker.person.fullName(),
    isActive: true,
    hasPhoto: false,
    roles: ["Admin"],
    createdAt: faker.date.past().toISOString(),
    rowVersion: faker.number.int({ min: 1, max: 999999 }),
    ...overrides,
  };
}

export function buildRoleDto(overrides: Partial<RoleDto> = {}): RoleDto {
  return {
    id: faker.string.uuid(),
    name: faker.helpers.arrayElement(["Admin", "Manager", "Viewer"]),
    description: faker.lorem.sentence(),
    isSystem: false,
    permissions: ["users.read", "roles.read"],
    createdAt: faker.date.past().toISOString(),
    rowVersion: faker.number.int({ min: 1, max: 999999 }),
    ...overrides,
  };
}

export function buildPermissionDto(overrides: Partial<PermissionDto> = {}): PermissionDto {
  return {
    id: faker.string.uuid(),
    code: "users.read",
    name: "Read Users",
    description: null,
    group: "Users",
    ...overrides,
  };
}

export function buildPagedResult<T>(items: T[], page = 1, pageSize = 25): PagedResult<T> {
  return {
    items,
    totalCount: items.length,
    page,
    pageSize,
    totalPages: Math.ceil(items.length / pageSize) || 1,
  };
}
