import { faker } from "@faker-js/faker";
import type { MenuItemConfig, EntityConfig, EntityFieldConfig, UpstreamEntry, PermissionDto, EntityMetadataDto } from "@/api/types";
import type { UserProfile } from "@broker/ui-kit/src/api/types";

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

export function buildMenuItemConfig(overrides: Partial<MenuItemConfig> = {}): MenuItemConfig {
  return {
    id: faker.string.alphanumeric(8),
    label: faker.lorem.word(),
    icon: "Dashboard",
    path: `/${faker.string.alphanumeric(6)}`,
    permissions: ["clients.read"],
    ...overrides,
  };
}

export function buildEntityFieldConfig(overrides: Partial<EntityFieldConfig> = {}): EntityFieldConfig {
  return {
    name: faker.database.column(),
    roles: ["*"],
    ...overrides,
  };
}

export function buildEntityConfig(overrides: Partial<EntityConfig> = {}): EntityConfig {
  return {
    name: faker.helpers.arrayElement(["Client", "Account", "Instrument", "Order", "Transaction"]),
    fields: [
      buildEntityFieldConfig({ name: "id" }),
      buildEntityFieldConfig({ name: "name" }),
      buildEntityFieldConfig({ name: "status", roles: ["Manager", "Operator"] }),
    ],
    ...overrides,
  };
}

export function buildUpstreamEntry(overrides: Partial<UpstreamEntry> = {}): UpstreamEntry {
  return {
    address: `http://${faker.internet.domainName()}:${faker.number.int({ min: 3000, max: 9999 })}`,
    routes: [
      `/api/v1/${faker.string.alphanumeric(6)}`,
      `/api/v1/${faker.string.alphanumeric(6)}`,
    ],
    ...overrides,
  };
}

export function buildPermissionDto(overrides: Partial<PermissionDto> = {}): PermissionDto {
  const group = faker.helpers.arrayElement(["clients", "accounts", "instruments"]);
  const action = faker.helpers.arrayElement(["read", "create", "update", "delete"]);
  return {
    id: faker.string.uuid(),
    code: `${group}.${action}`,
    name: `${group} ${action}`,
    group,
    ...overrides,
  };
}

export function buildEntityMetadataDto(overrides: Partial<EntityMetadataDto> = {}): EntityMetadataDto {
  return {
    name: faker.helpers.arrayElement(["Client", "Account", "Instrument"]),
    fields: ["id", "name", "status", "createdAt", "updatedAt"],
    ...overrides,
  };
}
