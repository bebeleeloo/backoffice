import type { OrderStatus } from "../api/types";

export const STATUS_DESCRIPTIONS: Record<OrderStatus, string> = {
  New: "Order created, awaiting processing",
  PendingApproval: "Awaiting manual approval",
  Approved: "Approved, ready for execution",
  Rejected: "Rejected, will not be processed",
  InProgress: "Currently being executed",
  PartiallyFilled: "Partially executed",
  Filled: "Fully executed",
  Completed: "Processing complete",
  Cancelled: "Cancelled before full execution",
  Failed: "Execution failed",
};
