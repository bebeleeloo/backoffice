import { createTheme, type Theme } from "@mui/material/styles";
import type {} from "@mui/x-data-grid/themeAugmentation";

/* ── Sidebar color tokens (used by MainLayout) ── */
export const SIDEBAR_COLORS = {
  bg: "#0F172A",
  bgHover: "#1E293B",
  bgActiveAlpha: "rgba(13, 148, 136, 0.15)",
  text: "#CBD5E1",
  textActive: "#FFFFFF",
  textMuted: "#64748B",
  divider: "#1E293B",
  activeIndicator: "#0D9488",
  logo: "#F0FDFA",
} as const;

/* ── Gradient presets for dashboard stat cards ── */
export const STAT_GRADIENTS = [
  "linear-gradient(135deg, #0D9488 0%, #059669 100%)",
  "linear-gradient(135deg, #0891B2 0%, #0D9488 100%)",
  "linear-gradient(135deg, #059669 0%, #10B981 100%)",
  "linear-gradient(135deg, #0F766E 0%, #0891B2 100%)",
] as const;

export function createAppTheme(mode: "light" | "dark"): Theme {
  return createTheme({
    palette: {
      mode,
      primary: {
        main: "#0D9488",
        light: "#14B8A6",
        dark: "#0F766E",
        contrastText: "#FFFFFF",
      },
      secondary: {
        main: "#059669",
        light: "#10B981",
        dark: "#047857",
        contrastText: "#FFFFFF",
      },
      ...(mode === "light"
        ? { background: { default: "#F8FAFC", paper: "#FFFFFF" } }
        : { background: { default: "#0F172A", paper: "#1E293B" } }),
    },
    typography: {
      fontFamily: "'Inter', 'Roboto', 'Helvetica', 'Arial', sans-serif",
    },
    shape: {
      borderRadius: 8,
    },
    components: {
      MuiButton: {
        styleOverrides: {
          containedPrimary: {
            background: "linear-gradient(135deg, #0D9488 0%, #059669 100%)",
            boxShadow: "0 2px 8px rgba(13, 148, 136, 0.25)",
            "&:hover": {
              background: "linear-gradient(135deg, #0F766E 0%, #047857 100%)",
              boxShadow: "0 4px 12px rgba(13, 148, 136, 0.35)",
            },
          },
        },
      },
      MuiCard: {
        styleOverrides: {
          root: {
            borderRadius: 12,
            ...(mode === "light"
              ? { boxShadow: "0 1px 3px rgba(0,0,0,0.08), 0 1px 2px rgba(0,0,0,0.06)" }
              : { boxShadow: "0 1px 3px rgba(0,0,0,0.3)" }),
          },
        },
      },
      MuiChip: {
        styleOverrides: {
          root: {
            fontWeight: 500,
          },
        },
      },
      MuiDataGrid: {
        defaultProps: {
          density: "compact",
          disableRowSelectionOnClick: true,
        },
        styleOverrides: {
          root: {
            borderRadius: 12,
            border: mode === "light" ? "1px solid #E2E8F0" : "1px solid #334155",
          },
          columnHeaders: {
            backgroundColor: mode === "light" ? "#F1F5F9" : "#1E293B",
          },
        },
      },
    },
  });
}

/** Scoped theme for list/journal pages — compact controls without affecting dialogs/forms. */
export function createAppListTheme(baseTheme: Theme): Theme {
  return createTheme(baseTheme, {
    components: {
      MuiTextField: {
        defaultProps: {
          size: "small" as const,
        },
      },
      MuiButton: {
        defaultProps: {
          size: "small" as const,
        },
        variants: [
          {
            props: { size: "small" as const },
            style: {
              minHeight: 32,
              paddingTop: 4,
              paddingBottom: 4,
              fontSize: "0.8125rem",
            },
          },
        ],
      },
      MuiIconButton: {
        defaultProps: {
          size: "small" as const,
        },
        variants: [
          {
            props: { size: "small" as const },
            style: {
              padding: 6,
            },
          },
        ],
      },
      MuiChip: {
        defaultProps: {
          size: "small" as const,
        },
        styleOverrides: {
          sizeSmall: {
            height: 24,
            fontSize: "0.8125rem",
          },
          labelSmall: {
            paddingLeft: 8,
            paddingRight: 8,
          },
        },
      },
      MuiOutlinedInput: {
        styleOverrides: {
          root: {
            variants: [
              {
                props: { size: "small" as const },
                style: {
                  minHeight: 32,
                },
              },
            ],
          },
          inputSizeSmall: {
            paddingTop: 4,
            paddingBottom: 4,
            paddingLeft: 8,
            paddingRight: 8,
          },
        },
      },
      MuiInputLabel: {
        styleOverrides: {
          root: {
            fontSize: "0.8125rem",
          },
        },
      },
      MuiFormHelperText: {
        styleOverrides: {
          root: {
            fontSize: "0.75rem",
          },
        },
      },
      MuiInputAdornment: {
        styleOverrides: {
          root: {
            "& .MuiSvgIcon-root": {
              fontSize: "1.125rem",
            },
          },
        },
      },
      MuiDataGrid: {
        defaultProps: {
          density: "standard" as const,
          disableRowSelectionOnClick: true,
          rowHeight: 44,
          columnHeaderHeight: 44,
        },
        styleOverrides: {
          root: {
            fontSize: "0.8125rem",
          },
          columnHeaders: {
            fontSize: "0.8125rem",
          },
          cell: {
            display: "flex",
            alignItems: "center",
            "& .MuiDataGrid-cellContent": {
              lineHeight: 1.25,
            },
          },
          columnHeader: {
            display: "flex",
            alignItems: "center",
          },
          columnHeaderTitle: {
            fontWeight: 500,
          },
        },
      },
    },
  });
}
