import { faker } from "@faker-js/faker";
import type { RoleDto } from "@/api/types";

export function buildRoleDto(overrides: Partial<RoleDto> = {}): RoleDto {
  return {
    id: faker.string.uuid(),
    name: faker.helpers.arrayElement(["Admin", "Manager", "Viewer"]),
    description: faker.lorem.sentence(),
    isSystem: false,
    permissions: ["users.read", "roles.read"],
    createdAt: faker.date.past().toISOString(),
    rowVersion: faker.string.alphanumeric(8),
    ...overrides,
  };
}
