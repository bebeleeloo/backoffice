import { faker } from "@faker-js/faker";
import type { ClientListItemDto } from "@/api/types";

export function buildClientListItemDto(
  overrides: Partial<ClientListItemDto> = {},
): ClientListItemDto {
  return {
    id: faker.string.uuid(),
    clientType: faker.helpers.arrayElement(["Individual", "Corporate"]),
    displayName: faker.person.fullName(),
    email: faker.internet.email(),
    status: faker.helpers.arrayElement(["Active", "Blocked", "PendingKyc"]),
    kycStatus: faker.helpers.arrayElement(["NotStarted", "InProgress", "Approved", "Rejected"]),
    residenceCountryIso2: "US",
    residenceCountryFlagEmoji: "\u{1F1FA}\u{1F1F8}",
    createdAt: faker.date.past().toISOString(),
    rowVersion: faker.string.alphanumeric(8),
    phone: faker.phone.number(),
    externalId: faker.string.alphanumeric(10),
    pepStatus: false,
    riskLevel: faker.helpers.arrayElement(["Low", "Medium", "High"]),
    residenceCountryName: "United States",
    citizenshipCountryIso2: "US",
    citizenshipCountryFlagEmoji: "\u{1F1FA}\u{1F1F8}",
    citizenshipCountryName: "United States",
    ...overrides,
  };
}
