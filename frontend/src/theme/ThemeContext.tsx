import { createContext, useContext, useState, useMemo, useEffect, useSyncExternalStore, type ReactNode } from "react";
import { ThemeProvider, CssBaseline } from "@mui/material";
import { createAppTheme, createAppListTheme } from "./index";
import type { Theme } from "@mui/material/styles";

export type ThemePreference = "light" | "dark" | "system";

interface ThemeContextValue {
  preference: ThemePreference;
  setPreference: (pref: ThemePreference) => void;
  mode: "light" | "dark";
}

const ThemeContext = createContext<ThemeContextValue | null>(null);
export const ListThemeContext = createContext<Theme | null>(null);

const STORAGE_KEY = "themeMode";

function isValidPreference(value: unknown): value is ThemePreference {
  return value === "light" || value === "dark" || value === "system";
}

function getStoredPreference(): ThemePreference {
  const stored = localStorage.getItem(STORAGE_KEY);
  return isValidPreference(stored) ? stored : "light";
}

const mediaQuery = window.matchMedia("(prefers-color-scheme: dark)");

function subscribeToMediaQuery(callback: () => void) {
  mediaQuery.addEventListener("change", callback);
  return () => mediaQuery.removeEventListener("change", callback);
}

function getSystemDark() {
  return mediaQuery.matches;
}

export function AppThemeProvider({ children }: { children: ReactNode }) {
  const [preference, setPreferenceState] = useState<ThemePreference>(getStoredPreference);
  const systemDark = useSyncExternalStore(subscribeToMediaQuery, getSystemDark);

  const setPreference = (pref: ThemePreference) => {
    setPreferenceState(pref);
    localStorage.setItem(STORAGE_KEY, pref);
  };

  const mode: "light" | "dark" =
    preference === "system" ? (systemDark ? "dark" : "light") : preference;

  const theme = useMemo(() => createAppTheme(mode), [mode]);
  const listTheme = useMemo(() => createAppListTheme(theme), [theme]);

  const ctxValue = useMemo(
    () => ({ preference, setPreference, mode }),
    [preference, mode],
  );

  useEffect(() => {
    document.documentElement.setAttribute("data-theme", mode);
  }, [mode]);

  return (
    <ThemeContext.Provider value={ctxValue}>
      <ListThemeContext.Provider value={listTheme}>
        <ThemeProvider theme={theme}>
          <CssBaseline />
          {children}
        </ThemeProvider>
      </ListThemeContext.Provider>
    </ThemeContext.Provider>
  );
}

export function useThemeMode(): ThemeContextValue {
  const ctx = useContext(ThemeContext);
  if (!ctx) throw new Error("useThemeMode must be used within AppThemeProvider");
  return ctx;
}

export function useListTheme(): Theme {
  const ctx = useContext(ListThemeContext);
  if (!ctx) throw new Error("useListTheme must be used within AppThemeProvider");
  return ctx;
}
