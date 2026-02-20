import { faker } from "@faker-js/faker";
import type { AuditLogDto } from "@/api/types";

export function buildAuditLogDto(overrides: Partial<AuditLogDto> = {}): AuditLogDto {
  return {
    id: faker.string.uuid(),
    userId: faker.string.uuid(),
    userName: faker.internet.username(),
    action: faker.helpers.arrayElement(["Create", "Update", "Delete"]),
    entityType: faker.helpers.arrayElement(["User", "Role", "Client"]),
    entityId: faker.string.uuid(),
    beforeJson: null,
    afterJson: null,
    correlationId: faker.string.uuid(),
    ipAddress: faker.internet.ipv4(),
    path: "/api/v1/users",
    method: faker.helpers.arrayElement(["GET", "POST", "PUT", "DELETE"]),
    statusCode: 200,
    isSuccess: true,
    createdAt: faker.date.past().toISOString(),
    ...overrides,
  };
}
