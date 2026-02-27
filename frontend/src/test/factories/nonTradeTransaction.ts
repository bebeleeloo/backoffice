import { faker } from "@faker-js/faker";
import type { NonTradeTransactionListItemDto } from "../../api/types";

export function buildNonTradeTransactionListItemDto(
  overrides: Partial<NonTradeTransactionListItemDto> = {},
): NonTradeTransactionListItemDto {
  return {
    id: faker.string.uuid(),
    transactionNumber: `NTT-${faker.date.recent().toISOString().slice(0, 10).replace(/-/g, "")}-${faker.string.alphanumeric(8).toUpperCase()}`,
    orderNumber: faker.datatype.boolean() ? `NTO-${faker.string.alphanumeric(8).toUpperCase()}` : null,
    accountNumber: faker.datatype.boolean() ? `ACC-${faker.string.alphanumeric(6).toUpperCase()}` : null,
    status: faker.helpers.arrayElement(["Pending", "Settled", "Failed", "Cancelled"] as const),
    transactionDate: faker.date.recent().toISOString(),
    amount: faker.number.float({ min: 10, max: 10000, fractionDigits: 2 }),
    currencyCode: faker.helpers.arrayElement(["USD", "EUR", "GBP"]),
    instrumentSymbol: faker.datatype.boolean() ? faker.helpers.arrayElement(["AAPL", "GOOGL"]) : null,
    referenceNumber: faker.datatype.boolean() ? `REF-${faker.number.int({ min: 100000, max: 999999 })}` : null,
    processedAt: faker.datatype.boolean() ? faker.date.recent().toISOString() : null,
    externalId: faker.datatype.boolean() ? `EXT-NTT-${faker.number.int({ min: 10000, max: 99999 })}` : null,
    createdAt: faker.date.recent().toISOString(),
    rowVersion: faker.string.alphanumeric(8),
    ...overrides,
  };
}
