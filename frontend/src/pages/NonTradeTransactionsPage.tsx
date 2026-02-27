import { useState, useCallback, useMemo, type ReactNode } from "react";
import { Button, IconButton, Chip, Paper, Tooltip } from "@mui/material";
import { type GridColDef, type GridPaginationModel, type GridSortModel } from "@mui/x-data-grid";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import VisibilityIcon from "@mui/icons-material/Visibility";
import FilterListOffIcon from "@mui/icons-material/FilterListOff";
import { useNonTradeTransactions, useDeleteNonTradeTransaction, useAccounts, useInstruments } from "../api/hooks";
import type { NonTradeTransactionListItemDto, TransactionStatus } from "../api/types";
import { useHasPermission } from "../auth/usePermission";
import { CreateNonTradeTransactionDialog, EditNonTradeTransactionDialog } from "./NonTradeTransactionDialogs";
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

const STATUS_OPTIONS: { value: TransactionStatus; label: string }[] = [
  { value: "Pending", label: "Pending" },
  { value: "Settled", label: "Settled" },
  { value: "Failed", label: "Failed" },
  { value: "Cancelled", label: "Cancelled" },
];

const STATUS_COLORS: Record<string, "default" | "primary" | "secondary" | "error" | "info" | "success" | "warning"> = {
  Pending: "warning",
  Settled: "success",
  Failed: "error",
  Cancelled: "default",
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
    accountId: getAllOrUndefined(sp, "accountId"),
    instrumentId: getAllOrUndefined(sp, "instrumentId"),
    transactionNumber: sp.get("transactionNumber") || undefined,
    orderNumber: sp.get("orderNumber") || undefined,
    currencyCode: sp.get("currencyCode") || undefined,
    referenceNumber: sp.get("referenceNumber") || undefined,
    externalId: sp.get("externalId") || undefined,
    transactionDateFrom: sp.get("transactionDateFrom") || undefined,
    transactionDateTo: sp.get("transactionDateTo") || undefined,
    createdFrom: sp.get("createdFrom") || undefined,
    createdTo: sp.get("createdTo") || undefined,
    processedFrom: sp.get("processedFrom") || undefined,
    processedTo: sp.get("processedTo") || undefined,
    amountMin: sp.get("amountMin") || undefined,
    amountMax: sp.get("amountMax") || undefined,
  };
}

export function NonTradeTransactionsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  const params = readParams(searchParams);

  const [createOpen, setCreateOpen] = useState(false);
  const [editTransaction, setEditTransaction] = useState<{ id: string } | null>(null);

  const canCreate = useHasPermission("transactions.create");
  const canUpdate = useHasPermission("transactions.update");
  const canDelete = useHasPermission("transactions.delete");

  const { data, isLoading } = useNonTradeTransactions(params);
  const { data: accountsData } = useAccounts({ page: 1, pageSize: 200 });
  const { data: instrumentsData } = useInstruments({ page: 1, pageSize: 200 });

  const accountOptions = useMemo(() =>
    (accountsData?.items ?? []).map((a) => ({ value: a.id, label: a.number })),
  [accountsData]);

  const instrumentOptions = useMemo(() =>
    (instrumentsData?.items ?? []).map((i) => ({ value: i.id, label: `${i.symbol} — ${i.name}` })),
  [instrumentsData]);

  const deleteTransaction = useDeleteNonTradeTransaction();
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
    params.q || params.transactionNumber || params.orderNumber || params.currencyCode ||
    params.referenceNumber || params.externalId ||
    params.status?.length || params.accountId?.length || params.instrumentId?.length ||
    params.transactionDateFrom || params.transactionDateTo || params.createdFrom || params.createdTo ||
    params.processedFrom || params.processedTo ||
    params.amountMin || params.amountMax
  );

  const handlePagination = (model: GridPaginationModel) => {
    setParam({ page: String(model.page + 1), pageSize: String(model.pageSize) });
  };

  const handleSort = (model: GridSortModel) => {
    const s = model[0];
    setParam({ sort: s ? `${s.field} ${s.sort}` : undefined, page: "1" });
  };

  const handleDelete = async (id: string) => {
    const ok = await confirm({ title: "Delete Non-Trade Transaction", message: "Are you sure you want to delete this non-trade transaction?" });
    if (!ok) return;
    try { await deleteTransaction.mutateAsync(id); } catch { /* handled by MutationCache */ }
  };

  const columns: GridColDef<NonTradeTransactionListItemDto>[] = [
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
    {
      field: "amount", headerName: "Amount", width: 120,
      renderCell: ({ value }) => value != null ? Number(value).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : "\u2014",
    },
    { field: "currencyCode", headerName: "Currency", width: 90 },
    { field: "instrumentSymbol", headerName: "Instrument", width: 120,
      renderCell: ({ value }) => value ?? "\u2014",
    },
    { field: "referenceNumber", headerName: "Reference #", flex: 1, minWidth: 130,
      renderCell: ({ value }) => value ?? "\u2014",
    },
    {
      field: "processedAt", headerName: "Processed", width: 170,
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
          <IconButton size="small" onClick={() => navigate(`/non-trade-transactions/${row.id}`)} data-testid={`action-view-${row.id}`}>
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
    m.set("status", () => (
      <CompactMultiSelect
        options={STATUS_OPTIONS}
        value={params.status ?? []}
        onChange={(v) => setMultiFilterParam("status", v)}
      />
    ));
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
    m.set("amount", () => (
      <NumericRangePopover
        minValue={params.amountMin ?? ""}
        maxValue={params.amountMax ?? ""}
        onMinChange={(v) => setFilterParam("amountMin", v || undefined)}
        onMaxChange={(v) => setFilterParam("amountMax", v || undefined)}
      />
    ));
    m.set("currencyCode", () => (
      <InlineTextFilter
        value={params.currencyCode ?? ""}
        onChange={(v) => setFilterParam("currencyCode", v || undefined)}
        placeholder="Currency..."
      />
    ));
    m.set("referenceNumber", () => (
      <InlineTextFilter
        value={params.referenceNumber ?? ""}
        onChange={(v) => setFilterParam("referenceNumber", v || undefined)}
        placeholder="Reference..."
      />
    ));
    m.set("processedAt", () => (
      <DateRangePopover
        fromValue={params.processedFrom ?? ""}
        toValue={params.processedTo ?? ""}
        onFromChange={(v) => setFilterParam("processedFrom", v || undefined)}
        onToChange={(v) => setFilterParam("processedTo", v || undefined)}
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
  }, [params.status, params.accountId, params.instrumentId,
      params.transactionNumber, params.orderNumber,
      params.transactionDateFrom, params.transactionDateTo,
      params.amountMin, params.amountMax, params.currencyCode,
      params.referenceNumber, params.externalId,
      params.createdFrom, params.createdTo,
      params.processedFrom, params.processedTo,
      accountOptions, instrumentOptions,
      setFilterParam, setMultiFilterParam]);

  const exportColumns: ExcelColumn<NonTradeTransactionListItemDto>[] = useMemo(() => [
    { header: "Transaction #", value: (r) => r.transactionNumber },
    { header: "Order #", value: (r) => r.orderNumber },
    { header: "Account", value: (r) => r.accountNumber },
    { header: "Status", value: (r) => r.status },
    { header: "Txn Date", value: (r) => r.transactionDate ? new Date(r.transactionDate).toLocaleDateString() : "" },
    { header: "Amount", value: (r) => r.amount },
    { header: "Currency", value: (r) => r.currencyCode },
    { header: "Instrument", value: (r) => r.instrumentSymbol ?? "" },
    { header: "Reference #", value: (r) => r.referenceNumber ?? "" },
    { header: "Processed", value: (r) => r.processedAt ? new Date(r.processedAt).toLocaleString() : "" },
    { header: "External ID", value: (r) => r.externalId ?? "" },
    { header: "Created", value: (r) => r.createdAt ? new Date(r.createdAt).toLocaleString() : "" },
  ], []);

  const fetchAllTransactions = useCallback(async () => {
    const { page: _, pageSize: __, ...filters } = params;
    const resp = await apiClient.get<PagedResult<NonTradeTransactionListItemDto>>("/non-trade-transactions", {
      params: { ...filters, page: 1, pageSize: 10000 },
    });
    return resp.data.items;
  }, [params]);

  return (
    <PageContainer
      variant="list"
      title="Non-Trade Transactions"
      actions={
        <>
          {hasActiveFilters && (
            <Tooltip title="Clear all filters">
              <IconButton size="small" onClick={clearAllFilters}><FilterListOffIcon /></IconButton>
            </Tooltip>
          )}
          <ExportButton fetchData={fetchAllTransactions} columns={exportColumns} filename="non-trade-transactions" />
          {canCreate && (
            <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
              Create Transaction
            </Button>
          )}
        </>
      }
      subheaderLeft={
        <GlobalSearchBar
          value={params.q ?? ""}
          onChange={(v) => setFilterParam("q", v || undefined)}
          placeholder="Search non-trade transactions..."
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
          onRowClick={(p) => navigate(`/non-trade-transactions/${p.row.id}`)}
          initialState={{
            columns: {
              columnVisibilityModel: {
                externalId: false,
                processedAt: false,
                instrumentSymbol: false,
              },
            },
          }}
          sx={{ height: "100%", border: "none", "& .MuiDataGrid-row": { cursor: "pointer" } }}
        />
      </Paper>

      <CreateNonTradeTransactionDialog open={createOpen} onClose={() => setCreateOpen(false)} />
      <EditNonTradeTransactionDialog onClose={() => setEditTransaction(null)} transaction={editTransaction} />
      <ConfirmDialog {...confirmDialogProps} isLoading={deleteTransaction.isPending} />
    </PageContainer>
  );
}
