import { useAuth } from "./useAuth";

export function useHasPermission(permission: string): boolean {
  const { permissions } = useAuth();
  return permissions.includes(permission);
}
