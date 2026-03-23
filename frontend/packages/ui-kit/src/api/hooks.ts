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
const GATEWAY_ENTITY_TYPES = new Set(["MenuConfig", "EntitiesConfig", "UpstreamsConfig", "Config"]);

function getEntityChangesBasePath(entityType: string): string {
  if (AUTH_ENTITY_TYPES.has(entityType)) return "/auth/entity-changes";
  if (GATEWAY_ENTITY_TYPES.has(entityType)) return "/config/entity-changes";
  return "/entity-changes";
}

export const useEntityChanges = (params: EntityChangesParams, enabled = true) => {
  const basePath = getEntityChangesBasePath(params.entityType);
  return useQuery({
    queryKey: ["entity-changes", params],
    queryFn: () =>
      apiClient.get<PagedResult<OperationDto>>(basePath, { params }).then((r) => r.data),
    enabled: enabled && !!params.entityId,
    placeholderData: keepPreviousData,
  });
};

export const useAllEntityChanges = (params: AllEntityChangesParams) => {
  const isAuthEntity = params.entityType && AUTH_ENTITY_TYPES.has(params.entityType);
  const isGatewayEntity = params.entityType && GATEWAY_ENTITY_TYPES.has(params.entityType);
  const isCoreEntity = params.entityType && !AUTH_ENTITY_TYPES.has(params.entityType) && !GATEWAY_ENTITY_TYPES.has(params.entityType);

  return useQuery({
    queryKey: ["entity-changes-all", params],
    queryFn: async () => {
      const cleaned = cleanParams(params as Record<string, unknown>);

      if (isAuthEntity) {
        return apiClient.get<PagedResult<GlobalOperationDto>>("/auth/entity-changes/all", { params: cleaned }).then((r) => r.data);
      }
      if (isGatewayEntity) {
        return apiClient.get<PagedResult<GlobalOperationDto>>("/config/entity-changes/all", { params: cleaned }).then((r) => r.data);
      }
      if (isCoreEntity) {
        return apiClient.get<PagedResult<GlobalOperationDto>>("/entity-changes/all", { params: cleaned }).then((r) => r.data);
      }

      // No entityType filter — fetch from all three services and merge
      const [core, auth, gateway] = await Promise.all([
        apiClient.get<PagedResult<GlobalOperationDto>>("/entity-changes/all", { params: cleaned }).then((r) => r.data),
        apiClient.get<PagedResult<GlobalOperationDto>>("/auth/entity-changes/all", { params: cleaned }).then((r) => r.data),
        apiClient.get<PagedResult<GlobalOperationDto>>("/config/entity-changes/all", { params: cleaned }).then((r) => r.data),
      ]);

      const merged = [...core.items, ...auth.items, ...gateway.items]
        .sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime());

      const pageSize = params.pageSize || 25;
      const totalCount = core.totalCount + auth.totalCount + gateway.totalCount;
      return {
        items: merged.slice(0, pageSize),
        totalCount,
        page: params.page || 1,
        pageSize,
        totalPages: Math.ceil(totalCount / pageSize),
      };
    },
    placeholderData: keepPreviousData,
  });
};
