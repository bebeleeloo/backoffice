import { useState, useCallback, useMemo, type ReactNode } from "react";
import { Button, IconButton, Chip, TextField, InputAdornment, Paper } from "@mui/material";
import { type GridColDef, type GridPaginationModel, type GridSortModel } from "@mui/x-data-grid";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import VisibilityIcon from "@mui/icons-material/Visibility";
import SearchIcon from "@mui/icons-material/Search";
import { useInstruments, useDeleteInstrument } from "../api/hooks";
import type {
  InstrumentListItemDto, InstrumentType, AssetClass, InstrumentStatus, Sector,
} from "../api/types";
import { useHasPermission } from "../auth/usePermission";
import { CreateInstrumentDialog, EditInstrumentDialog } from "./InstrumentDialogs";
import { useSearchParams, useNavigate } from "react-router-dom";
import { PageContainer } from "../components/PageContainer";
import { FilteredDataGrid, InlineTextFilter, CompactMultiSelect } from "../components/grid";

const STATUS_OPTIONS: { value: InstrumentStatus; label: string }[] = [
  { value: "Active", label: "Active" },
  { value: "Inactive", label: "Inactive" },
  { value: "Delisted", label: "Delisted" },
  { value: "Suspended", label: "Suspended" },
];

const TYPE_OPTIONS: { value: InstrumentType; label: string }[] = [
  { value: "Stock", label: "Stock" },
  { value: "Bond", label: "Bond" },
  { value: "ETF", label: "ETF" },
  { value: "Option", label: "Option" },
  { value: "Future", label: "Future" },
  { value: "Forex", label: "Forex" },
  { value: "CFD", label: "CFD" },
  { value: "MutualFund", label: "Mutual Fund" },
  { value: "Warrant", label: "Warrant" },
  { value: "Index", label: "Index" },
];

const ASSET_CLASS_OPTIONS: { value: AssetClass; label: string }[] = [
  { value: "Equities", label: "Equities" },
  { value: "FixedIncome", label: "Fixed Income" },
  { value: "Derivatives", label: "Derivatives" },
  { value: "ForeignExchange", label: "FX" },
  { value: "Commodities", label: "Commodities" },
  { value: "Funds", label: "Funds" },
];

const SECTOR_OPTIONS: { value: Sector; label: string }[] = [
  { value: "Technology", label: "Technology" },
  { value: "Healthcare", label: "Healthcare" },
  { value: "Finance", label: "Finance" },
  { value: "Energy", label: "Energy" },
  { value: "ConsumerDiscretionary", label: "Consumer Disc." },
  { value: "ConsumerStaples", label: "Consumer Staples" },
  { value: "Industrials", label: "Industrials" },
  { value: "Materials", label: "Materials" },
  { value: "RealEstate", label: "Real Estate" },
  { value: "Utilities", label: "Utilities" },
  { value: "Communication", label: "Communication" },
  { value: "Other", label: "Other" },
];

const STATUS_COLORS: Record<InstrumentStatus, "success" | "error" | "default" | "warning"> = {
  Active: "success",
  Inactive: "default",
  Delisted: "error",
  Suspended: "warning",
};

function getAllOrUndefined(sp: URLSearchParams, key: string) {
  const vals = sp.getAll(key);
  return vals.length > 0 ? vals : undefined;
}

function readParams(sp: URLSearchParams) {
  return {
    page: Number(sp.get("page") || "1"),
    pageSize: Number(sp.get("pageSize") || "25"),
    sort: sp.get("sort") || undefined,
    q: sp.get("q") || undefined,
    symbol: sp.get("symbol") || undefined,
    name: sp.get("name") || undefined,
    type: getAllOrUndefined(sp, "type") as InstrumentType[] | undefined,
    assetClass: getAllOrUndefined(sp, "assetClass") as AssetClass[] | undefined,
    status: getAllOrUndefined(sp, "status") as InstrumentStatus[] | undefined,
    sector: getAllOrUndefined(sp, "sector") as Sector[] | undefined,
    exchangeName: sp.get("exchangeName") || undefined,
    currencyCode: sp.get("currencyCode") || undefined,
  };
}

export function InstrumentsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  const params = readParams(searchParams);

  const [createOpen, setCreateOpen] = useState(false);
  const [editInstrument, setEditInstrument] = useState<InstrumentListItemDto | null>(null);
  const [search, setSearch] = useState(params.q ?? "");

  const canCreate = useHasPermission("instruments.create");
  const canUpdate = useHasPermission("instruments.update");
  const canDelete = useHasPermission("instruments.delete");

  const { data, isLoading } = useInstruments(params);
  const deleteInstrument = useDeleteInstrument();

  const setParam = useCallback(
    (patch: Record<string, string | undefined>) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        for (const [k, v] of Object.entries(patch)) {
          if (v === undefined || v === "") next.delete(k);
          else next.set(k, v);
        }
        return next;
      });
    },
    [setSearchParams],
  );

  const setFilterParam = useCallback(
    (key: string, value: string | undefined) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        if (value === undefined || value === "") next.delete(key);
        else next.set(key, value);
        next.set("page", "1");
        return next;
      });
    },
    [setSearchParams],
  );

  const setMultiFilterParam = useCallback(
    (key: string, values: string[]) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        next.delete(key);
        for (const v of values) next.append(key, v);
        next.set("page", "1");
        return next;
      });
    },
    [setSearchParams],
  );

  const handlePagination = (model: GridPaginationModel) => {
    setParam({ page: String(model.page + 1), pageSize: String(model.pageSize) });
  };

  const handleSort = (model: GridSortModel) => {
    const s = model[0];
    setParam({ sort: s ? `${s.field} ${s.sort}` : undefined, page: "1" });
  };

  const handleSearch = () => {
    setParam({ q: search || undefined, page: "1" });
  };

  const handleDelete = async (id: string) => {
    if (!confirm("Delete this instrument?")) return;
    await deleteInstrument.mutateAsync(id);
  };

  const columns: GridColDef<InstrumentListItemDto>[] = [
    { field: "symbol", headerName: "Symbol", width: 110 },
    { field: "name", headerName: "Name", flex: 1, minWidth: 180 },
    {
      field: "type", headerName: "Type", width: 110,
      renderCell: ({ value }) => <Chip label={value} size="small" variant="outlined" />,
    },
    { field: "assetClass", headerName: "Asset Class", width: 120 },
    {
      field: "status", headerName: "Status", width: 110,
      renderCell: ({ value }) => (
        <Chip label={value} color={STATUS_COLORS[value as InstrumentStatus] ?? "default"} size="small" />
      ),
    },
    { field: "sector", headerName: "Sector", width: 130 },
    { field: "exchangeCode", headerName: "Exchange", width: 100 },
    { field: "currencyCode", headerName: "Currency", width: 90 },
    {
      field: "countryName", headerName: "Country", width: 120,
      renderCell: ({ row }) =>
        row.countryFlagEmoji && row.countryName
          ? `${row.countryFlagEmoji} ${row.countryName}`
          : row.countryName ?? "—",
    },
    { field: "lotSize", headerName: "Lot Size", width: 90, type: "number" },
    {
      field: "isMarginEligible", headerName: "Margin", width: 80,
      renderCell: ({ value }) => (value ? "Yes" : "No"),
    },
    { field: "isin", headerName: "ISIN", width: 130 },
    { field: "cusip", headerName: "CUSIP", width: 110 },
    { field: "externalId", headerName: "External ID", width: 120 },
    {
      field: "createdAt", headerName: "Created", width: 110,
      renderCell: ({ value }) => new Date(value as string).toLocaleDateString(),
    },
    {
      field: "actions", headerName: "", width: 120, sortable: false, filterable: false, disableColumnMenu: true,
      renderCell: ({ row }) => (
        <>
          <IconButton size="small" onClick={() => navigate(`/instruments/${row.id}`)}>
            <VisibilityIcon fontSize="small" />
          </IconButton>
          {canUpdate && (
            <IconButton size="small" onClick={() => setEditInstrument(row)}>
              <EditIcon fontSize="small" />
            </IconButton>
          )}
          {canDelete && (
            <IconButton size="small" onClick={() => handleDelete(row.id)} color="error">
              <DeleteIcon fontSize="small" />
            </IconButton>
          )}
        </>
      ),
    },
  ];

  const filterDefs = useMemo(() => {
    const m = new Map<string, () => ReactNode>();
    m.set("symbol", () => (
      <InlineTextFilter
        value={params.symbol ?? ""}
        onChange={(v) => setFilterParam("symbol", v || undefined)}
        placeholder="Symbol..."
      />
    ));
    m.set("name", () => (
      <InlineTextFilter
        value={params.name ?? ""}
        onChange={(v) => setFilterParam("name", v || undefined)}
        placeholder="Name..."
      />
    ));
    m.set("type", () => (
      <CompactMultiSelect
        options={TYPE_OPTIONS}
        value={params.type ?? []}
        onChange={(v) => setMultiFilterParam("type", v)}
      />
    ));
    m.set("assetClass", () => (
      <CompactMultiSelect
        options={ASSET_CLASS_OPTIONS}
        value={params.assetClass ?? []}
        onChange={(v) => setMultiFilterParam("assetClass", v)}
      />
    ));
    m.set("status", () => (
      <CompactMultiSelect
        options={STATUS_OPTIONS}
        value={params.status ?? []}
        onChange={(v) => setMultiFilterParam("status", v)}
      />
    ));
    m.set("sector", () => (
      <CompactMultiSelect
        options={SECTOR_OPTIONS}
        value={params.sector ?? []}
        onChange={(v) => setMultiFilterParam("sector", v)}
      />
    ));
    m.set("exchangeCode", () => (
      <InlineTextFilter
        value={params.exchangeName ?? ""}
        onChange={(v) => setFilterParam("exchangeName", v || undefined)}
        placeholder="Exchange..."
      />
    ));
    m.set("currencyCode", () => (
      <InlineTextFilter
        value={params.currencyCode ?? ""}
        onChange={(v) => setFilterParam("currencyCode", v || undefined)}
        placeholder="Currency..."
      />
    ));
    return m;
  }, [params.symbol, params.name, params.type, params.assetClass, params.status,
      params.sector, params.exchangeName, params.currencyCode,
      setFilterParam, setMultiFilterParam]);

  return (
    <PageContainer
      variant="list"
      title="Instruments"
      actions={
        canCreate ? (
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
            Create Instrument
          </Button>
        ) : undefined
      }
      subheaderLeft={
        <TextField
          fullWidth
          placeholder="Search instruments..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          onKeyDown={(e) => e.key === "Enter" && handleSearch()}
          slotProps={{
            input: {
              endAdornment: (
                <InputAdornment position="end">
                  <IconButton onClick={handleSearch}><SearchIcon /></IconButton>
                </InputAdornment>
              ),
            },
          }}
        />
      }
    >
      <Paper variant="outlined" sx={{ flex: 1, minHeight: 0, overflow: "hidden" }}>
        <FilteredDataGrid
          rows={data?.items ?? []}
          columns={columns}
          rowCount={data?.totalCount ?? 0}
          loading={isLoading}
          paginationMode="server"
          sortingMode="server"
          paginationModel={{ page: params.page - 1, pageSize: params.pageSize }}
          onPaginationModelChange={handlePagination}
          onSortModelChange={handleSort}
          pageSizeOptions={[10, 25, 50]}
          filterDefs={filterDefs}
          initialState={{
            columns: {
              columnVisibilityModel: {
                isin: false,
                cusip: false,
                externalId: false,
                lotSize: false,
                isMarginEligible: false,
                countryName: false,
              },
            },
          }}
          sx={{ height: "100%", border: "none" }}
        />
      </Paper>

      <CreateInstrumentDialog open={createOpen} onClose={() => setCreateOpen(false)} />
      <EditInstrumentDialog open={!!editInstrument} onClose={() => setEditInstrument(null)} instrument={editInstrument} />
    </PageContainer>
  );
}
