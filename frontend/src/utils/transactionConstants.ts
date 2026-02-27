import type { TransactionStatus } from "../api/types";

export const TRANSACTION_STATUS_DESCRIPTIONS: Record<TransactionStatus, string> = {
  Pending: "Transaction created, awaiting settlement",
  Settled: "Transaction successfully settled",
  Failed: "Transaction processing failed",
  Cancelled: "Transaction was cancelled",
};
