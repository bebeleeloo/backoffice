import { useContext } from "react";
import { NavigationContext } from "./context";

export function useAppNavigation() {
  const context = useContext(NavigationContext);
  if (!context) {
    throw new Error("useAppNavigation must be used within a NavigationProvider");
  }
  return context;
}
