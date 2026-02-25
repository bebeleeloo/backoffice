import { createTheme, type Theme } from "@mui/material/styles";
import type {} from "@mui/x-data-grid/themeAugmentation";

export function createAppTheme(mode: "light" | "dark"): Theme {
  return createTheme({
    palette: {
      mode,
      primary: {
        main: "#1565c0",
      },
      secondary: {
        main: "#7b1fa2",
      },
      ...(mode === "light" && {
        background: {
          default: "#f5f5f5",
        },
      }),
    },
    typography: {
      fontFamily: "'Inter', 'Roboto', 'Helvetica', 'Arial', sans-serif",
    },
    components: {
      MuiDataGrid: {
        defaultProps: {
          density: "compact",
          disableRowSelectionOnClick: true,
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
