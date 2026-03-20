import { createContext } from "react";

export interface NavigationContextValue {
  navigateTo: (path: string) => void;
}

export const NavigationContext = createContext<NavigationContextValue | null>(null);
