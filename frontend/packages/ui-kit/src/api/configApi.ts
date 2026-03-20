import { useQuery } from "@tanstack/react-query";
import { apiClient } from "./client";

export interface MenuItem {
  id: string;
  label: string;
  icon: string;
  path?: string;
  permissions?: string[];
  children?: MenuItem[];
}

export function useMenu() {
  return useQuery<MenuItem[]>({
    queryKey: ["config", "menu"],
    queryFn: async () => {
      const { data } = await apiClient.get<MenuItem[]>("/config/menu");
      return data;
    },
    staleTime: 5 * 60 * 1000,
  });
}
