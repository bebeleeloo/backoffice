import { faker } from "@faker-js/faker";
import type { TradeTransactionListItemDto } from "../../api/types";

export function buildTradeTransactionListItemDto(
  overrides: Partial<TradeTransactionListItemDto> = {},
): TradeTransactionListItemDto {
  return {
    id: faker.string.uuid(),
    transactionNumber: `TT-${faker.date.recent().toISOString().slice(0, 10).replace(/-/g, "")}-${faker.string.alphanumeric(8).toUpperCase()}`,
    orderNumber: faker.datatype.boolean() ? `TO-${faker.string.alphanumeric(8).toUpperCase()}` : null,
    accountNumber: faker.datatype.boolean() ? `ACC-${faker.string.alphanumeric(6).toUpperCase()}` : null,
    status: faker.helpers.arrayElement(["Pending", "Settled", "Failed", "Cancelled"] as const),
    transactionDate: faker.date.recent().toISOString(),
    instrumentSymbol: faker.helpers.arrayElement(["AAPL", "GOOGL", "MSFT", "TSLA"]),
    instrumentName: faker.company.name(),
    side: faker.helpers.arrayElement(["Buy", "Sell", "ShortSell", "BuyToCover"] as const),
    quantity: faker.number.int({ min: 1, max: 1000 }),
    price: faker.number.float({ min: 1, max: 500, fractionDigits: 2 }),
    commission: faker.datatype.boolean() ? faker.number.float({ min: 0.5, max: 20, fractionDigits: 2 }) : null,
    settlementDate: faker.datatype.boolean() ? faker.date.soon().toISOString() : null,
    venue: faker.datatype.boolean() ? faker.helpers.arrayElement(["NYSE", "NASDAQ", "LSE"]) : null,
    externalId: faker.datatype.boolean() ? `EXT-TT-${faker.number.int({ min: 10000, max: 99999 })}` : null,
    createdAt: faker.date.recent().toISOString(),
    rowVersion: faker.string.alphanumeric(8),
    ...overrides,
  };
}
