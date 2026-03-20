import { useCallback } from "react";
import { useNavigate } from "react-router-dom";
import { NavigationContext } from "./context";

interface NavigationProviderProps {
  internalPaths: string[];
  children: React.ReactNode;
}

export function NavigationProvider({ internalPaths, children }: NavigationProviderProps) {
  const navigate = useNavigate();

  const navigateTo = useCallback(
    (path: string) => {
      const isInternal = internalPaths.some((prefix) => {
        if (prefix === "/") return path === "/";
        return path === prefix || path.startsWith(prefix + "/");
      });

      if (isInternal) {
        navigate(path);
      } else {
        window.location.href = path;
      }
    },
    [internalPaths, navigate],
  );

  return (
    <NavigationContext.Provider value={{ navigateTo }}>
      {children}
    </NavigationContext.Provider>
  );
}
