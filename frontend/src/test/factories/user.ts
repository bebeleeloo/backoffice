import { faker } from "@faker-js/faker";
import type { UserDto, UserProfile } from "@/api/types";

export const ALL_PERMISSIONS = [
  "users.read", "users.create", "users.update", "users.delete",
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

export function buildUserDto(overrides: Partial<UserDto> = {}): UserDto {
  return {
    id: faker.string.uuid(),
    username: faker.internet.username(),
    email: faker.internet.email(),
    fullName: faker.person.fullName(),
    isActive: true,
    roles: ["Admin"],
    createdAt: faker.date.past().toISOString(),
    rowVersion: faker.string.alphanumeric(8),
    ...overrides,
  };
}

export function buildUserProfile(overrides: Partial<UserProfile> = {}): UserProfile {
  return {
    id: faker.string.uuid(),
    username: "admin",
    email: "admin@test.com",
    fullName: "Test Admin",
    roles: ["Admin"],
    permissions: ALL_PERMISSIONS,
    scopes: [],
    ...overrides,
  };
}
