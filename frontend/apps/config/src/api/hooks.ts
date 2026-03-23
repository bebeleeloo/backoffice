import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@broker/ui-kit";
import type { MenuItemConfig, MenuConfig, EntityConfig, EntitiesConfig, EntityMetadataDto, UpstreamsMap, UpstreamsConfig, PermissionDto } from "./types";

export function useMenuRaw() {
  return useQuery<MenuItemConfig[]>({
    queryKey: ["config", "menu", "raw"],
    queryFn: async () => {
      const { data } = await apiClient.get<MenuItemConfig[]>("/config/menu/raw");
      return data;
    },
  });
}

export function useSaveMenu() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (config: MenuConfig) => {
      const { data } = await apiClient.put("/config/menu", config);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["config", "menu"] });
    },
    meta: { successMessage: "Menu configuration saved" },
  });
}

export function useEntitiesRaw() {
  return useQuery<EntityConfig[]>({
    queryKey: ["config", "entities", "raw"],
    queryFn: async () => {
      const { data } = await apiClient.get<EntityConfig[]>("/config/entities/raw");
      return data;
    },
  });
}

export function useSaveEntities() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (config: EntitiesConfig) => {
      const { data } = await apiClient.put("/config/entities", config);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["config", "entities"] });
    },
    meta: { successMessage: "Entity configuration saved" },
  });
}

export function useEntityMetadata() {
  return useQuery<EntityMetadataDto[]>({
    queryKey: ["entity-metadata"],
    queryFn: async () => {
      const [core, auth] = await Promise.all([
        apiClient.get<EntityMetadataDto[]>("/entity-metadata").then((r) => r.data),
        apiClient.get<EntityMetadataDto[]>("/auth/entity-metadata").then((r) => r.data),
      ]);
      return [...core, ...auth];
    },
    staleTime: 10 * 60 * 1000,
  });
}

export function useUpstreams() {
  return useQuery<UpstreamsMap>({
    queryKey: ["config", "upstreams"],
    queryFn: async () => {
      const { data } = await apiClient.get<UpstreamsMap>("/config/upstreams");
      return data;
    },
  });
}

export function useSaveUpstreams() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (config: UpstreamsConfig) => {
      const { data } = await apiClient.put("/config/upstreams", config);
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["config", "upstreams"] });
    },
    meta: { successMessage: "Upstreams configuration saved" },
  });
}

export function usePermissions() {
  return useQuery<PermissionDto[]>({
    queryKey: ["permissions"],
    queryFn: async () => {
      const { data } = await apiClient.get<PermissionDto[]>("/permissions");
      return data;
    },
    staleTime: 10 * 60 * 1000,
  });
}

export function useReloadConfig() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async () => {
      const { data } = await apiClient.post("/config/reload");
      return data;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["config"] });
    },
    meta: { successMessage: "Configuration reloaded" },
  });
}

