import { useMemo, type ReactNode } from "react";
import { Box, Typography } from "@mui/material";
import SearchOffIcon from "@mui/icons-material/SearchOff";
import type { SxProps, Theme } from "@mui/material/styles";
import { DataGrid, type DataGridProps } from "@mui/x-data-grid";
import { FilterRowProvider, CustomColumnHeaders } from "./GridFilterRow";

function CustomNoRowsOverlay() {
  return (
    <Box sx={{ display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", height: "100%", gap: 1 }}>
      <SearchOffIcon sx={{ fontSize: 48, color: "text.disabled" }} />
      <Typography variant="body1" color="text.secondary">No results found</Typography>
      <Typography variant="body2" color="text.disabled">Try adjusting your search or filter criteria</Typography>
    </Box>
  );
}

const BASE_HEADER_HEIGHT = 44;
const FILTER_ROW_HEIGHT = 36;
const TOTAL = BASE_HEADER_HEIGHT + FILTER_ROW_HEIGHT; // 80

export interface FilteredDataGridProps extends DataGridProps {
  /** Map of field name → render function for inline column filters. */
  filterDefs: Map<string, () => ReactNode>;
}

/**
 * DataGrid wrapped with FilterRowProvider + CustomColumnHeaders.
 *
 * columnHeaderHeight stays at 44 — cells are normal 44px with properly centered text.
 * When filters are active, two CSS custom properties are overridden so MUI's internal
 * layout accounts for the extra 36px filter row:
 *   --DataGrid-headersTotalHeight  (controls virtualScrollerContent padding-top)
 *   --DataGrid-topContainerHeight  (controls topContainer height)
 * MUI sets these inline via style.setProperty(), hence !important is required.
 */
export function FilteredDataGrid({ filterDefs, slots, sx, ...gridProps }: FilteredDataGridProps) {
  const hasActiveFilters = useMemo(() => {
    if (!filterDefs || filterDefs.size === 0) return false;

    const visibilityModel =
      gridProps.initialState?.columns?.columnVisibilityModel as
        | Record<string, boolean>
        | undefined;

    const visibleFields = new Set(
      gridProps.columns
        .filter((c) => {
          const field = c.field;
          if (visibilityModel && visibilityModel[field] === false) return false;
          return true;
        })
        .map((c) => c.field),
    );

    for (const key of filterDefs.keys()) {
      if (visibleFields.has(key)) return true;
    }
    return false;
  }, [filterDefs, gridProps.columns, gridProps.initialState?.columns?.columnVisibilityModel]);

  const internalSx: SxProps<Theme> = hasActiveFilters
    ? {
        "--DataGrid-headersTotalHeight": `${TOTAL}px !important`,
        "--DataGrid-topContainerHeight": `${TOTAL}px !important`,
      }
    : {};

  return (
    <FilterRowProvider filterDefs={filterDefs}>
      <DataGrid
        {...gridProps}
        columnHeaderHeight={BASE_HEADER_HEIGHT}
        disableColumnFilter
        slots={{ noRowsOverlay: CustomNoRowsOverlay, ...slots, columnHeaders: CustomColumnHeaders as never }}
        sx={[internalSx, ...(Array.isArray(sx) ? sx : sx ? [sx] : [])]}
      />
    </FilterRowProvider>
  );
}
