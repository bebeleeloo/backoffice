import { useState, useCallback, useMemo, type ReactNode } from "react";
import { Button, IconButton, Chip, Paper, Tooltip } from "@mui/material";
import { type GridColDef, type GridPaginationModel, type GridSortModel } from "@mui/x-data-grid";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import VisibilityIcon from "@mui/icons-material/Visibility";
import FilterListOffIcon from "@mui/icons-material/FilterListOff";
import { useTradeTransactions, useDeleteTradeTransaction, useAccounts, useInstruments } from "../api/hooks";
import type { TradeTransactionListItemDto, TransactionStatus, TradeSide } from "../api/types";
import { useHasPermission } from "../auth/usePermission";
import { CreateTradeTransactionDialog, EditTradeTransactionDialog } from "./TradeTransactionDialogs";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { useConfirm } from "../hooks/useConfirm";
import { useSearchParams, useNavigate } from "react-router-dom";
import { ExportButton } from "../components/ExportButton";
import type { ExcelColumn } from "../utils/exportToExcel";
import { apiClient } from "../api/client";
import type { PagedResult } from "../api/types";
import { PageContainer } from "../components/PageContainer";
import { GlobalSearchBar } from "../components/GlobalSearchBar";
import { FilteredDataGrid, InlineTextFilter, CompactMultiSelect, DateRangePopover, NumericRangePopover } from "../components/grid";
import { EntityHistoryDialog } from "../components/EntityHistoryDialog";

const STATUS_OPTIONS: { value: TransactionStatus; label: string }[] = [
  { value: "Pending", label: "Pending" },
  { value: "Settled", label: "Settled" },
  { value: "Failed", label: "Failed" },
  { value: "Cancelled", label: "Cancelled" },
];

const SIDE_OPTIONS: { value: TradeSide; label: string }[] = [
  { value: "Buy", label: "Buy" },
  { value: "Sell", label: "Sell" },
  { value: "ShortSell", label: "Short Sell" },
  { value: "BuyToCover", label: "Buy to Cover" },
];

const STATUS_COLORS: Record<string, "default" | "primary" | "secondary" | "error" | "info" | "success" | "warning"> = {
  Pending: "warning",
  Settled: "success",
  Failed: "error",
  Cancelled: "default",
};

const SIDE_COLORS: Record<string, "default" | "primary" | "secondary" | "error" | "info" | "success" | "warning"> = {
  Buy: "success",
  Sell: "error",
  ShortSell: "warning",
  BuyToCover: "info",
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
    status: getAllOrUndefined(sp, "status") as TransactionStatus[] | undefined,
    side: getAllOrUndefined(sp, "side") as TradeSide[] | undefined,
    accountId: getAllOrUndefined(sp, "accountId"),
    instrumentId: getAllOrUndefined(sp, "instrumentId"),
    transactionNumber: sp.get("transactionNumber") || undefined,
    orderNumber: sp.get("orderNumber") || undefined,
    externalId: sp.get("externalId") || undefined,
    transactionDateFrom: sp.get("transactionDateFrom") || undefined,
    transactionDateTo: sp.get("transactionDateTo") || undefined,
    createdFrom: sp.get("createdFrom") || undefined,
    createdTo: sp.get("createdTo") || undefined,
    settlementDateFrom: sp.get("settlementDateFrom") || undefined,
    settlementDateTo: sp.get("settlementDateTo") || undefined,
    quantityMin: sp.get("quantityMin") || undefined,
    quantityMax: sp.get("quantityMax") || undefined,
    priceMin: sp.get("priceMin") || undefined,
    priceMax: sp.get("priceMax") || undefined,
    commissionMin: sp.get("commissionMin") || undefined,
    commissionMax: sp.get("commissionMax") || undefined,
  };
}

export function TradeTransactionsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  const params = readParams(searchParams);

  const [createOpen, setCreateOpen] = useState(false);
  const [editTransaction, setEditTransaction] = useState<{ id: string } | null>(null);
  const [historyEntity, setHistoryEntity] = useState<{ id: string } | null>(null);

  const canCreate = useHasPermission("transactions.create");
  const canUpdate = useHasPermission("transactions.update");
  const canDelete = useHasPermission("transactions.delete");

  const { data, isLoading } = useTradeTransactions(params);
  const { data: accountsData } = useAccounts({ page: 1, pageSize: 200 });
  const { data: instrumentsData } = useInstruments({ page: 1, pageSize: 200 });

  const accountOptions = useMemo(() =>
    (accountsData?.items ?? []).map((a) => ({ value: a.id, label: a.number })),
  [accountsData]);

  const instrumentOptions = useMemo(() =>
    (instrumentsData?.items ?? []).map((i) => ({ value: i.id, label: `${i.symbol} — ${i.name}` })),
  [instrumentsData]);

  const deleteTradeTransaction = useDeleteTradeTransaction();
  const { confirm, confirmDialogProps } = useConfirm();

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

  const clearAllFilters = useCallback(() => {
    setSearchParams(new URLSearchParams());
  }, [setSearchParams]);

  const hasActiveFilters = !!(
    params.q || params.transactionNumber || params.orderNumber || params.externalId ||
    params.status?.length || params.side?.length || params.accountId?.length || params.instrumentId?.length ||
    params.transactionDateFrom || params.transactionDateTo || params.createdFrom || params.createdTo ||
    params.settlementDateFrom || params.settlementDateTo ||
    params.quantityMin || params.quantityMax || params.priceMin || params.priceMax ||
    params.commissionMin || params.commissionMax
  );

  const handlePagination = (model: GridPaginationModel) => {
    setParam({ page: String(model.page + 1), pageSize: String(model.pageSize) });
  };

  const handleSort = (model: GridSortModel) => {
    const s = model[0];
    setParam({ sort: s ? `${s.field} ${s.sort}` : undefined, page: "1" });
  };

  const handleDelete = async (id: string) => {
    const ok = await confirm({ title: "Delete Trade Transaction", message: "Are you sure you want to delete this trade transaction?" });
    if (!ok) return;
    try { await deleteTradeTransaction.mutateAsync(id); } catch { /* handled by MutationCache */ }
  };

  const columns: GridColDef<TradeTransactionListItemDto>[] = [
    { field: "transactionNumber", headerName: "Transaction #", flex: 1, minWidth: 150 },
    { field: "orderNumber", headerName: "Order #", flex: 1, minWidth: 130 },
    { field: "accountNumber", headerName: "Account", flex: 1, minWidth: 120 },
    {
      field: "status", headerName: "Status", width: 120,
      renderCell: ({ value }) => (
        <Chip label={value} color={STATUS_COLORS[value as string] ?? "default"} size="small" />
      ),
    },
    {
      field: "transactionDate", headerName: "Txn Date", width: 120,
      renderCell: ({ value }) => new Date(value as string).toLocaleDateString(),
    },
    { field: "instrumentSymbol", headerName: "Instrument", flex: 1, minWidth: 120 },
    {
      field: "side", headerName: "Side", width: 120,
      renderCell: ({ value }) => (
        <Chip label={value} color={SIDE_COLORS[value as string] ?? "default"} size="small" />
      ),
    },
    {
      field: "quantity", headerName: "Qty", width: 100, type: "number",
      renderCell: ({ value }) => (value as number).toLocaleString(),
    },
    {
      field: "price", headerName: "Price", width: 100, type: "number",
      renderCell: ({ value }) => (value as number).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }),
    },
    {
      field: "commission", headerName: "Commission", width: 110, type: "number",
      renderCell: ({ value }) => value != null ? (value as number).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : "\u2014",
    },
    {
      field: "settlementDate", headerName: "Settlement", width: 120,
      renderCell: ({ value }) => value ? new Date(value as string).toLocaleDateString() : "\u2014",
    },
    { field: "venue", headerName: "Venue", width: 120,
      renderCell: ({ value }) => value ?? "\u2014",
    },
    { field: "externalId", headerName: "External ID", width: 120 },
    {
      field: "createdAt", headerName: "Created", width: 170,
      renderCell: ({ value }) => new Date(value as string).toLocaleString(),
    },
    {
      field: "actions", headerName: "", width: 120, sortable: false, filterable: false, disableColumnMenu: true,
      renderCell: ({ row }) => (
        <div onClick={(e) => e.stopPropagation()}>
          <IconButton size="small" onClick={() => navigate(`/trade-transactions/${row.id}`)} data-testid={`action-view-${row.id}`}>
            <VisibilityIcon fontSize="small" />
          </IconButton>
          {canUpdate && (
            <IconButton size="small" onClick={() => setEditTransaction({ id: row.id })} data-testid={`action-edit-${row.id}`}>
              <EditIcon fontSize="small" />
            </IconButton>
          )}
          {canDelete && (
            <IconButton size="small" onClick={() => handleDelete(row.id)} color="error" data-testid={`action-delete-${row.id}`}>
              <DeleteIcon fontSize="small" />
            </IconButton>
          )}
        </div>
      ),
    },
  ];

  const filterDefs = useMemo(() => {
    const m = new Map<string, () => ReactNode>();
    m.set("transactionNumber", () => (
      <InlineTextFilter
        value={params.transactionNumber ?? ""}
        onChange={(v) => setFilterParam("transactionNumber", v || undefined)}
        placeholder="Transaction #..."
      />
    ));
    m.set("orderNumber", () => (
      <InlineTextFilter
        value={params.orderNumber ?? ""}
        onChange={(v) => setFilterParam("orderNumber", v || undefined)}
        placeholder="Order #..."
      />
    ));
    m.set("accountNumber", () => (
      <CompactMultiSelect
        options={accountOptions}
        value={params.accountId ?? []}
        onChange={(v) => setMultiFilterParam("accountId", v)}
      />
    ));
    m.set("status", () => (
      <CompactMultiSelect
        options={STATUS_OPTIONS}
        value={params.status ?? []}
        onChange={(v) => setMultiFilterParam("status", v)}
      />
    ));
    m.set("side", () => (
      <CompactMultiSelect
        options={SIDE_OPTIONS}
        value={params.side ?? []}
        onChange={(v) => setMultiFilterParam("side", v)}
      />
    ));
    m.set("instrumentSymbol", () => (
      <CompactMultiSelect
        options={instrumentOptions}
        value={params.instrumentId ?? []}
        onChange={(v) => setMultiFilterParam("instrumentId", v)}
      />
    ));
    m.set("transactionDate", () => (
      <DateRangePopover
        fromValue={params.transactionDateFrom ?? ""}
        toValue={params.transactionDateTo ?? ""}
        onFromChange={(v) => setFilterParam("transactionDateFrom", v || undefined)}
        onToChange={(v) => setFilterParam("transactionDateTo", v || undefined)}
      />
    ));
    m.set("settlementDate", () => (
      <DateRangePopover
        fromValue={params.settlementDateFrom ?? ""}
        toValue={params.settlementDateTo ?? ""}
        onFromChange={(v) => setFilterParam("settlementDateFrom", v || undefined)}
        onToChange={(v) => setFilterParam("settlementDateTo", v || undefined)}
      />
    ));
    m.set("quantity", () => (
      <NumericRangePopover
        minValue={params.quantityMin ?? ""}
        maxValue={params.quantityMax ?? ""}
        onMinChange={(v) => setFilterParam("quantityMin", v || undefined)}
        onMaxChange={(v) => setFilterParam("quantityMax", v || undefined)}
      />
    ));
    m.set("price", () => (
      <NumericRangePopover
        minValue={params.priceMin ?? ""}
        maxValue={params.priceMax ?? ""}
        onMinChange={(v) => setFilterParam("priceMin", v || undefined)}
        onMaxChange={(v) => setFilterParam("priceMax", v || undefined)}
      />
    ));
    m.set("commission", () => (
      <NumericRangePopover
        minValue={params.commissionMin ?? ""}
        maxValue={params.commissionMax ?? ""}
        onMinChange={(v) => setFilterParam("commissionMin", v || undefined)}
        onMaxChange={(v) => setFilterParam("commissionMax", v || undefined)}
      />
    ));
    m.set("externalId", () => (
      <InlineTextFilter
        value={params.externalId ?? ""}
        onChange={(v) => setFilterParam("externalId", v || undefined)}
        placeholder="External ID..."
      />
    ));
    m.set("createdAt", () => (
      <DateRangePopover
        fromValue={params.createdFrom ?? ""}
        toValue={params.createdTo ?? ""}
        onFromChange={(v) => setFilterParam("createdFrom", v || undefined)}
        onToChange={(v) => setFilterParam("createdTo", v || undefined)}
      />
    ));
    return m;
  }, [params.transactionNumber, params.orderNumber, params.status, params.side,
      params.accountId, params.instrumentId,
      params.transactionDateFrom, params.transactionDateTo,
      params.settlementDateFrom, params.settlementDateTo,
      params.externalId, params.createdFrom, params.createdTo,
      params.quantityMin, params.quantityMax, params.priceMin, params.priceMax,
      params.commissionMin, params.commissionMax,
      accountOptions, instrumentOptions,
      setFilterParam, setMultiFilterParam]);

  const exportColumns: ExcelColumn<TradeTransactionListItemDto>[] = useMemo(() => [
    { header: "Transaction #", value: (r) => r.transactionNumber },
    { header: "Order #", value: (r) => r.orderNumber },
    { header: "Account", value: (r) => r.accountNumber },
    { header: "Status", value: (r) => r.status },
    { header: "Txn Date", value: (r) => r.transactionDate ? new Date(r.transactionDate).toLocaleDateString() : "" },
    { header: "Instrument", value: (r) => r.instrumentSymbol },
    { header: "Side", value: (r) => r.side },
    { header: "Quantity", value: (r) => r.quantity },
    { header: "Price", value: (r) => r.price },
    { header: "Commission", value: (r) => r.commission != null ? r.commission : "" },
    { header: "Settlement", value: (r) => r.settlementDate ? new Date(r.settlementDate).toLocaleDateString() : "" },
    { header: "Venue", value: (r) => r.venue ?? "" },
    { header: "External ID", value: (r) => r.externalId ?? "" },
    { header: "Created", value: (r) => r.createdAt ? new Date(r.createdAt).toLocaleString() : "" },
  ], []);

  const fetchAllTradeTransactions = useCallback(async () => {
    const { page: _, pageSize: __, ...filters } = params;
    const resp = await apiClient.get<PagedResult<TradeTransactionListItemDto>>("/trade-transactions", {
      params: { ...filters, page: 1, pageSize: 10000 },
    });
    return resp.data.items;
  }, [params]);

  return (
    <PageContainer
      variant="list"
      title="Trade Transactions"
      actions={
        <>
          {hasActiveFilters && (
            <Tooltip title="Clear all filters">
              <IconButton size="small" onClick={clearAllFilters}><FilterListOffIcon /></IconButton>
            </Tooltip>
          )}
          <ExportButton fetchData={fetchAllTradeTransactions} columns={exportColumns} filename="trade-transactions" />
          {canCreate && (
            <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
              Create Trade Transaction
            </Button>
          )}
        </>
      }
      subheaderLeft={
        <GlobalSearchBar
          value={params.q ?? ""}
          onChange={(v) => setFilterParam("q", v || undefined)}
          placeholder="Search trade transactions..."
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
          onRowClick={(p) => navigate(`/trade-transactions/${p.row.id}`)}
          initialState={{
            columns: {
              columnVisibilityModel: {
                externalId: false,
                venue: false,
                commission: false,
              },
            },
          }}
          sx={{ height: "100%", border: "none", "& .MuiDataGrid-row": { cursor: "pointer" } }}
        />
      </Paper>

      <CreateTradeTransactionDialog open={createOpen} onClose={() => setCreateOpen(false)} />
      <EditTradeTransactionDialog onClose={() => setEditTransaction(null)} transaction={editTransaction} />
      <ConfirmDialog {...confirmDialogProps} isLoading={deleteTradeTransaction.isPending} />
      {historyEntity && (
        <EntityHistoryDialog entityType="Transaction" entityId={historyEntity.id} open={!!historyEntity} onClose={() => setHistoryEntity(null)} />
      )}
    </PageContainer>
  );
}
