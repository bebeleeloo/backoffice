import React, {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import {
  Box,
  TextField,
  Select,
  MenuItem,
  Checkbox,
  ListItemText,
  FormControl,
  Popover,
  IconButton,
  InputAdornment,
  Tooltip,
  type SelectChangeEvent,
} from "@mui/material";
import ClearIcon from "@mui/icons-material/Clear";
import CalendarTodayIcon from "@mui/icons-material/CalendarToday";
import { useTheme } from "@mui/material/styles";
import {
  GridColumnHeaders,
  useGridApiContext,
  useGridSelector,
  gridVisibleColumnDefinitionsSelector,
} from "@mui/x-data-grid";
import { DatePicker } from "@mui/x-date-pickers/DatePicker";
import dayjs, { type Dayjs } from "dayjs";
import { useDebounce } from "../../hooks/useDebounce";
import type { CountryDto } from "../../api/types";

/* ─── Context: field → render function map ─── */

const FilterCtx = createContext<Map<string, () => ReactNode>>(new Map());

export function FilterRowProvider({
  children,
  filterDefs,
}: {
  children: ReactNode;
  filterDefs: Map<string, () => ReactNode>;
}) {
  return <FilterCtx.Provider value={filterDefs}>{children}</FilterCtx.Provider>;
}

/* ─── Slot: original headers + filter row below ─── */

export const CustomColumnHeaders = React.forwardRef(
  function CustomColumnHeaders(props: object, ref: React.Ref<HTMLDivElement>) {
    const filterDefs = useContext(FilterCtx);
    const headerProps = props as React.ComponentProps<typeof GridColumnHeaders>;

    return (
      <>
        {/* eslint-disable-next-line @typescript-eslint/no-explicit-any */}
        <GridColumnHeaders ref={ref as any} {...headerProps} />
        {filterDefs.size > 0 && <HeaderFilterRow />}
      </>
    );
  },
);

/* ─── Filter row synced with grid columns ─── */

function HeaderFilterRow() {
  const filterDefs = useContext(FilterCtx);
  const apiRef = useGridApiContext();
  const theme = useTheme();
  const visibleColumns = useGridSelector(
    apiRef,
    gridVisibleColumnDefinitionsSelector,
  );
  const containerRef = useRef<HTMLDivElement>(null);
  const innerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const root = containerRef.current?.closest(
      ".MuiDataGrid-root",
    ) as HTMLElement | null;
    if (!root || !innerRef.current) return;
    const scroller = root.querySelector(
      ".MuiDataGrid-virtualScroller",
    ) as HTMLElement | null;
    if (!scroller) return;

    const sync = () => {
      if (innerRef.current)
        innerRef.current.style.transform = `translateX(-${scroller.scrollLeft}px)`;
    };
    scroller.addEventListener("scroll", sync);
    sync();
    return () => scroller.removeEventListener("scroll", sync);
  }, []);

  return (
    <div
      ref={containerRef}
      style={{
        overflow: "hidden",
        width: "100%",
        height: 36,
        borderTop: `1px solid ${theme.palette.divider}`,
        backgroundColor: theme.palette.background.paper,
      }}
    >
      <div
        ref={innerRef}
        style={{ display: "flex", alignItems: "center", height: 36, minHeight: 36, maxHeight: 36 }}
      >
        {visibleColumns.map((col) => {
          const render = filterDefs.get(col.field);
          return (
            <div
              key={col.field}
              style={{
                width: col.computedWidth || col.width || 100,
                minWidth: col.computedWidth || col.width || 100,
                flexShrink: 0,
                padding: "2px 10px",
                boxSizing: "border-box",
              }}
            >
              {render ? render() : <div style={{ height: 28 }} />}
            </div>
          );
        })}
      </div>
    </div>
  );
}

/* ─── Shared compact styles ─── */

const inputSx = {
  "& .MuiOutlinedInput-root": { height: 28, fontSize: "0.8rem" },
  "& .MuiOutlinedInput-input": { py: "2px", px: 1 },
};
const selectSx = {
  height: 28,
  fontSize: "0.8rem",
  "& .MuiSelect-select": { py: "2px", px: 1 },
};
const compactDateFieldSx = {
  "& .MuiOutlinedInput-root": { height: 32, fontSize: "0.8rem" },
  "& .MuiOutlinedInput-input": { py: "4px", px: 1 },
  "& .MuiInputLabel-root": { fontSize: "0.8rem" },
};
const NoActionBar = () => null;

/* ─── Text filter (debounced) ─── */

export function InlineTextFilter({
  value,
  onChange,
  placeholder,
}: {
  value: string;
  onChange: (v: string) => void;
  placeholder?: string;
}) {
  const [local, setLocal] = useState(value);
  const debounced = useDebounce(local, 300);

  useEffect(() => {
    if (debounced !== value) onChange(debounced);
  }, [debounced]); // eslint-disable-line react-hooks/exhaustive-deps

  useEffect(() => {
    setLocal(value);
  }, [value]);

  return (
    <TextField
      variant="outlined"
      size="small"
      fullWidth
      placeholder={placeholder ?? "Filter..."}
      value={local}
      onChange={(e) => setLocal(e.target.value)}
      sx={inputSx}
    />
  );
}

/* ─── Multi-enum (compact text, no chips) ─── */

export function CompactMultiSelect<T extends string>({
  options,
  value,
  onChange,
}: {
  options: { value: T; label: string }[];
  value: T[];
  onChange: (v: T[]) => void;
}) {
  const theme = useTheme();
  const handleChange = (e: SelectChangeEvent<string[]>) => {
    const v = e.target.value;
    onChange((typeof v === "string" ? v.split(",") : v) as T[]);
  };

  return (
    <FormControl size="small" fullWidth>
      <Select
        multiple
        value={value as string[]}
        onChange={handleChange}
        displayEmpty
        renderValue={(selected) => {
          const sel = selected as string[];
          if (sel.length === 0)
            return <span style={{ color: theme.palette.text.secondary }}>All</span>;
          if (sel.length <= 2)
            return sel
              .map((v) => options.find((o) => o.value === v)?.label ?? v)
              .join(", ");
          return `${sel.length} selected`;
        }}
        sx={selectSx}
      >
        {options.map((o) => (
          <MenuItem key={o.value} value={o.value} dense>
            <Checkbox
              checked={value.includes(o.value)}
              size="small"
              sx={{ p: 0.25 }}
            />
            <ListItemText
              primary={o.label}
              primaryTypographyProps={{ fontSize: "0.85rem" }}
              sx={{ ml: 0.5 }}
            />
          </MenuItem>
        ))}
      </Select>
    </FormControl>
  );
}

/* ─── Country multi-select (flags in dropdown, compact display) ─── */

export function CompactCountrySelect({
  countries,
  value,
  onChange,
}: {
  countries: CountryDto[];
  value: string[];
  onChange: (ids: string[]) => void;
}) {
  const theme = useTheme();
  return (
    <FormControl size="small" fullWidth>
      <Select
        multiple
        value={value}
        onChange={(e) => {
          const v = e.target.value;
          onChange(typeof v === "string" ? v.split(",") : v);
        }}
        displayEmpty
        renderValue={(selected) => {
          const sel = selected as string[];
          if (sel.length === 0)
            return <span style={{ color: theme.palette.text.secondary }}>All</span>;
          if (sel.length <= 2)
            return sel
              .map((id) => {
                const c = countries.find((c) => c.id === id);
                return c ? `${c.flagEmoji}${c.iso2}` : id;
              })
              .join(", ");
          return `${sel.length} countries`;
        }}
        sx={selectSx}
        MenuProps={{ PaperProps: { sx: { maxHeight: 300 } } }}
      >
        {countries.map((c) => (
          <MenuItem key={c.id} value={c.id} dense>
            <Checkbox
              checked={value.includes(c.id)}
              size="small"
              sx={{ p: 0.25 }}
            />
            <ListItemText
              primary={`${c.flagEmoji} ${c.name}`}
              primaryTypographyProps={{ fontSize: "0.85rem" }}
              sx={{ ml: 0.5 }}
            />
          </MenuItem>
        ))}
      </Select>
    </FormControl>
  );
}

/* ─── Date range popover ─── */

const toDayjs = (v: string) => (v ? dayjs(v) : null);
const fromDayjs = (d: Dayjs | null) => (d?.isValid() ? d.format("YYYY-MM-DD") : "");

const datePickerSharedProps = {
  closeOnSelect: true,
  reduceAnimations: true,
  showDaysOutsideCurrentMonth: true,
  fixedWeekNumber: 6,
} as const;

/**
 * Context passes clear state into ClearableDateTextField.
 * Using context is the canonical way to inject data into MUI slot components
 * when slotProps typing doesn't allow custom props.
 */
const DateClearCtx = createContext<{ hasValue: boolean; onClear: () => void }>({
  hasValue: false,
  onClear: () => {},
});

/** Module-level TextField slot: merges clear button before DatePicker's calendar icon. */
const ClearableDateTextField = React.forwardRef<
  HTMLDivElement,
  React.ComponentProps<typeof TextField>
>(function ClearableDateTextField(props, ref) {
  const { hasValue, onClear } = useContext(DateClearCtx);
  return (
    <TextField
      {...props}
      ref={ref}
      InputProps={{
        ...props.InputProps,
        endAdornment: (
          <>
            {hasValue && (
              <InputAdornment position="end" sx={{ mr: -0.5 }}>
                <Tooltip title="Очистить дату">
                  <IconButton
                    size="small"
                    aria-label="Очистить дату"
                    onClick={(e) => {
                      e.stopPropagation();
                      onClear();
                    }}
                    sx={{ p: 0.25 }}
                  >
                    <ClearIcon sx={{ fontSize: 16 }} />
                  </IconButton>
                </Tooltip>
              </InputAdornment>
            )}
            {props.InputProps?.endAdornment}
          </>
        ),
      }}
    />
  );
});

const datePickerSlots = { actionBar: NoActionBar, textField: ClearableDateTextField };

/** Compact DatePicker with integrated clear button. */
function DatePickerWithClear({
  label,
  value,
  onChange,
  ariaLabel,
}: {
  label: string;
  value: string;
  onChange: (v: string) => void;
  ariaLabel: string;
}) {
  return (
    <DateClearCtx.Provider value={{ hasValue: !!value, onClear: () => onChange("") }}>
      <DatePicker
        {...datePickerSharedProps}
        label={label}
        value={toDayjs(value)}
        onChange={(d) => onChange(fromDayjs(d))}
        slots={datePickerSlots}
        slotProps={{
          textField: {
            size: "small",
            fullWidth: true,
            sx: compactDateFieldSx,
            inputProps: { "aria-label": ariaLabel },
          },
        }}
      />
    </DateClearCtx.Provider>
  );
}

export function DateRangePopover({
  fromValue,
  toValue,
  onFromChange,
  onToChange,
}: {
  fromValue: string;
  toValue: string;
  onFromChange: (v: string) => void;
  onToChange: (v: string) => void;
}) {
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);

  const display = useMemo(() => {
    if (!fromValue && !toValue) return "";
    if (fromValue && toValue) return `${fromValue} — ${toValue}`;
    if (fromValue) return `≥ ${fromValue}`;
    return `≤ ${toValue}`;
  }, [fromValue, toValue]);

  const hasValue = !!(fromValue || toValue);

  return (
    <>
      <TextField
        variant="outlined"
        size="small"
        fullWidth
        value={display}
        placeholder="All"
        onClick={(e) => setAnchorEl(e.currentTarget as HTMLElement)}
        sx={{
          ...inputSx,
          "& .MuiOutlinedInput-input": {
            py: "2px",
            px: 1,
            cursor: "pointer",
          },
        }}
        slotProps={{
          htmlInput: { readOnly: true, "aria-label": "Created date range" },
          input: {
            endAdornment: hasValue ? (
              <InputAdornment position="end">
                <Tooltip title="Очистить диапазон дат">
                  <IconButton
                    size="small"
                    aria-label="Очистить диапазон дат"
                    onClick={(e) => {
                      e.stopPropagation();
                      onFromChange("");
                      onToChange("");
                    }}
                    sx={{ p: 0, mr: -0.5 }}
                  >
                    <ClearIcon sx={{ fontSize: 14 }} />
                  </IconButton>
                </Tooltip>
              </InputAdornment>
            ) : (
              <InputAdornment position="end">
                <CalendarTodayIcon
                  sx={{ fontSize: 14, color: "action.disabled" }}
                />
              </InputAdornment>
            ),
          },
        }}
      />
      <Popover
        open={!!anchorEl}
        anchorEl={anchorEl}
        onClose={() => setAnchorEl(null)}
        anchorOrigin={{ vertical: "bottom", horizontal: "left" }}
      >
        <Box sx={{ p: 1.5, display: "flex", flexDirection: "column", gap: 1.5, minWidth: 260 }}>
          <DatePickerWithClear
            label="From"
            value={fromValue}
            onChange={onFromChange}
            ariaLabel="Filter created from"
          />
          <DatePickerWithClear
            label="To"
            value={toValue}
            onChange={onToChange}
            ariaLabel="Filter created to"
          />
        </Box>
      </Popover>
    </>
  );
}

/* ─── Boolean (All / Yes / No) ─── */

export function InlineBooleanFilter({
  value,
  onChange,
}: {
  value: boolean | undefined;
  onChange: (v: boolean | undefined) => void;
}) {
  return (
    <FormControl size="small" fullWidth>
      <Select
        value={value === undefined ? "" : value ? "true" : "false"}
        onChange={(e) => {
          const v = e.target.value;
          onChange(v === "" ? undefined : v === "true");
        }}
        displayEmpty
        sx={selectSx}
      >
        <MenuItem value="" dense>
          <em>All</em>
        </MenuItem>
        <MenuItem value="true" dense>
          Yes
        </MenuItem>
        <MenuItem value="false" dense>
          No
        </MenuItem>
      </Select>
    </FormControl>
  );
}
