import { useState, useCallback, useMemo, type ReactNode } from "react";
import { Button, IconButton, Chip, Paper } from "@mui/material";
import { type GridColDef, type GridPaginationModel, type GridSortModel } from "@mui/x-data-grid";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import VisibilityIcon from "@mui/icons-material/Visibility";
import { useTradeOrders, useDeleteTradeOrder, useAccounts, useInstruments } from "../api/hooks";
import type { TradeOrderListItemDto, OrderStatus, TradeSide, TradeOrderType, TimeInForce } from "../api/types";
import { useHasPermission } from "../auth/usePermission";
import { CreateTradeOrderDialog, EditTradeOrderDialog } from "./TradeOrderDialogs";
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

const STATUS_OPTIONS: { value: OrderStatus; label: string }[] = [
  { value: "New", label: "New" },
  { value: "PendingApproval", label: "Pending Approval" },
  { value: "Approved", label: "Approved" },
  { value: "Rejected", label: "Rejected" },
  { value: "InProgress", label: "In Progress" },
  { value: "PartiallyFilled", label: "Partially Filled" },
  { value: "Filled", label: "Filled" },
  { value: "Completed", label: "Completed" },
  { value: "Cancelled", label: "Cancelled" },
  { value: "Failed", label: "Failed" },
];

const SIDE_OPTIONS: { value: TradeSide; label: string }[] = [
  { value: "Buy", label: "Buy" },
  { value: "Sell", label: "Sell" },
  { value: "ShortSell", label: "Short Sell" },
  { value: "BuyToCover", label: "Buy to Cover" },
];

const ORDER_TYPE_OPTIONS: { value: TradeOrderType; label: string }[] = [
  { value: "Market", label: "Market" },
  { value: "Limit", label: "Limit" },
  { value: "Stop", label: "Stop" },
  { value: "StopLimit", label: "Stop Limit" },
];

const TIF_OPTIONS: { value: TimeInForce; label: string }[] = [
  { value: "Day", label: "Day" },
  { value: "GTC", label: "GTC" },
  { value: "IOC", label: "IOC" },
  { value: "FOK", label: "FOK" },
  { value: "GTD", label: "GTD" },
];

const STATUS_COLORS: Record<string, "default" | "primary" | "secondary" | "error" | "info" | "success" | "warning"> = {
  New: "info",
  PendingApproval: "warning",
  Approved: "success",
  Rejected: "error",
  InProgress: "primary",
  PartiallyFilled: "warning",
  Filled: "success",
  Completed: "success",
  Cancelled: "default",
  Failed: "error",
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
    status: getAllOrUndefined(sp, "status") as OrderStatus[] | undefined,
    side: getAllOrUndefined(sp, "side") as TradeSide[] | undefined,
    orderType: getAllOrUndefined(sp, "orderType") as TradeOrderType[] | undefined,
    timeInForce: getAllOrUndefined(sp, "timeInForce") as TimeInForce[] | undefined,
    accountId: getAllOrUndefined(sp, "accountId"),
    instrumentId: getAllOrUndefined(sp, "instrumentId"),
    orderNumber: sp.get("orderNumber") || undefined,
    externalId: sp.get("externalId") || undefined,
    orderDateFrom: sp.get("orderDateFrom") || undefined,
    orderDateTo: sp.get("orderDateTo") || undefined,
    createdFrom: sp.get("createdFrom") || undefined,
    createdTo: sp.get("createdTo") || undefined,
    executedFrom: sp.get("executedFrom") || undefined,
    executedTo: sp.get("executedTo") || undefined,
    quantityMin: sp.get("quantityMin") || undefined,
    quantityMax: sp.get("quantityMax") || undefined,
    priceMin: sp.get("priceMin") || undefined,
    priceMax: sp.get("priceMax") || undefined,
    executedQuantityMin: sp.get("executedQuantityMin") || undefined,
    executedQuantityMax: sp.get("executedQuantityMax") || undefined,
    averagePriceMin: sp.get("averagePriceMin") || undefined,
    averagePriceMax: sp.get("averagePriceMax") || undefined,
    commissionMin: sp.get("commissionMin") || undefined,
    commissionMax: sp.get("commissionMax") || undefined,
  };
}

export function TradeOrdersPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  const params = readParams(searchParams);

  const [createOpen, setCreateOpen] = useState(false);
  const [editOrder, setEditOrder] = useState<{ id: string } | null>(null);

  const canCreate = useHasPermission("orders.create");
  const canUpdate = useHasPermission("orders.update");
  const canDelete = useHasPermission("orders.delete");

  const { data, isLoading } = useTradeOrders(params);
  const { data: accountsData } = useAccounts({ page: 1, pageSize: 200 });
  const { data: instrumentsData } = useInstruments({ page: 1, pageSize: 200 });

  const accountOptions = useMemo(() =>
    (accountsData?.items ?? []).map((a) => ({ value: a.id, label: a.number })),
  [accountsData]);

  const instrumentOptions = useMemo(() =>
    (instrumentsData?.items ?? []).map((i) => ({ value: i.id, label: `${i.symbol} — ${i.name}` })),
  [instrumentsData]);
  const deleteTradeOrder = useDeleteTradeOrder();
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

  const handlePagination = (model: GridPaginationModel) => {
    setParam({ page: String(model.page + 1), pageSize: String(model.pageSize) });
  };

  const handleSort = (model: GridSortModel) => {
    const s = model[0];
    setParam({ sort: s ? `${s.field} ${s.sort}` : undefined, page: "1" });
  };

  const handleDelete = async (id: string) => {
    const ok = await confirm({ title: "Delete Trade Order", message: "Are you sure you want to delete this trade order?" });
    if (!ok) return;
    try { await deleteTradeOrder.mutateAsync(id); } catch { /* handled by MutationCache */ }
  };

  const columns: GridColDef<TradeOrderListItemDto>[] = [
    { field: "orderNumber", headerName: "Order #", flex: 1, minWidth: 130 },
    { field: "accountNumber", headerName: "Account", flex: 1, minWidth: 130 },
    {
      field: "status", headerName: "Status", width: 140,
      renderCell: ({ value }) => (
        <Chip label={value} color={STATUS_COLORS[value as string] ?? "default"} size="small" />
      ),
    },
    {
      field: "orderDate", headerName: "Order Date", width: 120,
      renderCell: ({ value }) => new Date(value as string).toLocaleDateString(),
    },
    { field: "instrumentSymbol", headerName: "Instrument", flex: 1, minWidth: 120 },
    {
      field: "side", headerName: "Side", width: 120,
      renderCell: ({ value }) => (
        <Chip label={value} color={SIDE_COLORS[value as string] ?? "default"} size="small" />
      ),
    },
    { field: "orderType", headerName: "Type", width: 110 },
    { field: "timeInForce", headerName: "TIF", width: 80 },
    {
      field: "quantity", headerName: "Qty", width: 100, type: "number",
      renderCell: ({ value }) => (value as number).toLocaleString(),
    },
    {
      field: "price", headerName: "Price", width: 100, type: "number",
      renderCell: ({ value }) => value != null ? (value as number).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : "\u2014",
    },
    {
      field: "executedQuantity", headerName: "Exec Qty", width: 100, type: "number",
      renderCell: ({ value }) => (value as number).toLocaleString(),
    },
    {
      field: "averagePrice", headerName: "Avg Price", width: 110, type: "number",
      renderCell: ({ value }) => value != null ? (value as number).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : "\u2014",
    },
    {
      field: "commission", headerName: "Commission", width: 110, type: "number",
      renderCell: ({ value }) => value != null ? (value as number).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : "\u2014",
    },
    {
      field: "executedAt", headerName: "Executed", width: 170,
      renderCell: ({ value }) => value ? new Date(value as string).toLocaleString() : "\u2014",
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
          <IconButton size="small" onClick={() => navigate(`/trade-orders/${row.id}`)} data-testid={`action-view-${row.id}`}>
            <VisibilityIcon fontSize="small" />
          </IconButton>
          {canUpdate && (
            <IconButton size="small" onClick={() => setEditOrder({ id: row.id })} data-testid={`action-edit-${row.id}`}>
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
    m.set("orderDate", () => (
      <DateRangePopover
        fromValue={params.orderDateFrom ?? ""}
        toValue={params.orderDateTo ?? ""}
        onFromChange={(v) => setFilterParam("orderDateFrom", v || undefined)}
        onToChange={(v) => setFilterParam("orderDateTo", v || undefined)}
      />
    ));
    m.set("instrumentSymbol", () => (
      <CompactMultiSelect
        options={instrumentOptions}
        value={params.instrumentId ?? []}
        onChange={(v) => setMultiFilterParam("instrumentId", v)}
      />
    ));
    m.set("side", () => (
      <CompactMultiSelect
        options={SIDE_OPTIONS}
        value={params.side ?? []}
        onChange={(v) => setMultiFilterParam("side", v)}
      />
    ));
    m.set("orderType", () => (
      <CompactMultiSelect
        options={ORDER_TYPE_OPTIONS}
        value={params.orderType ?? []}
        onChange={(v) => setMultiFilterParam("orderType", v)}
      />
    ));
    m.set("timeInForce", () => (
      <CompactMultiSelect
        options={TIF_OPTIONS}
        value={params.timeInForce ?? []}
        onChange={(v) => setMultiFilterParam("timeInForce", v)}
      />
    ));
    m.set("executedAt", () => (
      <DateRangePopover
        fromValue={params.executedFrom ?? ""}
        toValue={params.executedTo ?? ""}
        onFromChange={(v) => setFilterParam("executedFrom", v || undefined)}
        onToChange={(v) => setFilterParam("executedTo", v || undefined)}
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
    m.set("executedQuantity", () => (
      <NumericRangePopover
        minValue={params.executedQuantityMin ?? ""}
        maxValue={params.executedQuantityMax ?? ""}
        onMinChange={(v) => setFilterParam("executedQuantityMin", v || undefined)}
        onMaxChange={(v) => setFilterParam("executedQuantityMax", v || undefined)}
      />
    ));
    m.set("averagePrice", () => (
      <NumericRangePopover
        minValue={params.averagePriceMin ?? ""}
        maxValue={params.averagePriceMax ?? ""}
        onMinChange={(v) => setFilterParam("averagePriceMin", v || undefined)}
        onMaxChange={(v) => setFilterParam("averagePriceMax", v || undefined)}
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
  }, [params.orderNumber, params.accountId, params.status, params.orderDateFrom, params.orderDateTo,
      params.instrumentId,
      params.side, params.orderType, params.timeInForce, params.externalId,
      params.createdFrom, params.createdTo, params.executedFrom, params.executedTo,
      params.quantityMin, params.quantityMax, params.priceMin, params.priceMax,
      params.executedQuantityMin, params.executedQuantityMax,
      params.averagePriceMin, params.averagePriceMax,
      params.commissionMin, params.commissionMax,
      accountOptions, instrumentOptions,
      setFilterParam, setMultiFilterParam]);

  const exportColumns: ExcelColumn<TradeOrderListItemDto>[] = useMemo(() => [
    { header: "Order #", value: (r) => r.orderNumber },
    { header: "Account", value: (r) => r.accountNumber },
    { header: "Status", value: (r) => r.status },
    { header: "Order Date", value: (r) => r.orderDate ? new Date(r.orderDate).toLocaleDateString() : "" },
    { header: "Instrument", value: (r) => r.instrumentSymbol },
    { header: "Side", value: (r) => r.side },
    { header: "Type", value: (r) => r.orderType },
    { header: "TIF", value: (r) => r.timeInForce },
    { header: "Quantity", value: (r) => r.quantity },
    { header: "Price", value: (r) => r.price != null ? r.price : "" },
    { header: "Exec Qty", value: (r) => r.executedQuantity },
    { header: "Avg Price", value: (r) => r.averagePrice != null ? r.averagePrice : "" },
    { header: "Commission", value: (r) => r.commission != null ? r.commission : "" },
    { header: "Executed", value: (r) => r.executedAt ? new Date(r.executedAt).toLocaleString() : "" },
    { header: "External ID", value: (r) => r.externalId },
    { header: "Created", value: (r) => r.createdAt ? new Date(r.createdAt).toLocaleString() : "" },
  ], []);

  const fetchAllTradeOrders = useCallback(async () => {
    const { page: _, pageSize: __, ...filters } = params;
    const resp = await apiClient.get<PagedResult<TradeOrderListItemDto>>("/trade-orders", {
      params: { ...filters, page: 1, pageSize: 10000 },
    });
    return resp.data.items;
  }, [params]);

  return (
    <PageContainer
      variant="list"
      title="Trade Orders"
      actions={
        <>
          <ExportButton fetchData={fetchAllTradeOrders} columns={exportColumns} filename="trade-orders" />
          {canCreate && (
            <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
              Create Trade Order
            </Button>
          )}
        </>
      }
      subheaderLeft={
        <GlobalSearchBar
          value={params.q ?? ""}
          onChange={(v) => setFilterParam("q", v || undefined)}
          placeholder="Search trade orders..."
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
          onRowClick={(p) => navigate(`/trade-orders/${p.row.id}`)}
          initialState={{
            columns: {
              columnVisibilityModel: {
                externalId: false,
                executedAt: false,
                commission: false,
              },
            },
          }}
          sx={{ height: "100%", border: "none", "& .MuiDataGrid-row": { cursor: "pointer" } }}
        />
      </Paper>

      <CreateTradeOrderDialog open={createOpen} onClose={() => setCreateOpen(false)} />
      <EditTradeOrderDialog onClose={() => setEditOrder(null)} order={editOrder} />
      <ConfirmDialog {...confirmDialogProps} isLoading={deleteTradeOrder.isPending} />
    </PageContainer>
  );
}
