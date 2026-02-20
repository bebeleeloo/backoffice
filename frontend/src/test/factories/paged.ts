import type { PagedResult } from "@/api/types";

export function buildPagedResult<T>(items: T[], page = 1, pageSize = 25): PagedResult<T> {
  return {
    items,
    totalCount: items.length,
    page,
    pageSize,
    totalPages: Math.ceil(items.length / pageSize) || 1,
  };
}
