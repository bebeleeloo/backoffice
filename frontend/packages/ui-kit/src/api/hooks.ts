import { useQuery, keepPreviousData } from "@tanstack/react-query";
import { apiClient } from "./client";
import type {
  PagedResult,
  OperationDto,
  EntityChangesParams,
  GlobalOperationDto,
  AllEntityChangesParams,
} from "./types";

export function cleanParams(params: Record<string, unknown>): Record<string, unknown> {
  const result: Record<string, unknown> = {};
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === "") continue;
    if (Array.isArray(value) && value.length === 0) continue;
    result[key] = value;
  }
  return result;
}

const AUTH_ENTITY_TYPES = new Set(["User", "Role"]);

export const useEntityChanges = (params: EntityChangesParams, enabled = true) => {
  const basePath = AUTH_ENTITY_TYPES.has(params.entityType)
    ? "/auth/entity-changes"
    : "/entity-changes";
  return useQuery({
    queryKey: ["entity-changes", params],
    queryFn: () =>
      apiClient.get<PagedResult<OperationDto>>(basePath, { params }).then((r) => r.data),
    enabled: enabled && !!params.entityId,
    placeholderData: keepPreviousData,
  });
};

export const useAllEntityChanges = (params: AllEntityChangesParams) =>
  useQuery({
    queryKey: ["entity-changes-all", params],
    queryFn: () =>
      apiClient.get<PagedResult<GlobalOperationDto>>("/entity-changes/all", { params: cleanParams(params as Record<string, unknown>) }).then((r) => r.data),
    placeholderData: keepPreviousData,
  });
