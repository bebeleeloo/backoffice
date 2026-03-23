import { useState, useCallback, useMemo, type ReactNode } from "react";
import { Button, IconButton, Chip, Paper, Tooltip } from "@mui/material";
import { type GridColDef, type GridPaginationModel, type GridSortModel, type GridSortDirection } from "@mui/x-data-grid";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import VisibilityIcon from "@mui/icons-material/Visibility";
import HistoryIcon from "@mui/icons-material/History";
import FilterListOffIcon from "@mui/icons-material/FilterListOff";
import { useAccounts, useDeleteAccount } from "../api/hooks";
import type { AccountListItemDto, AccountStatus, AccountType, MarginType, Tariff } from "../api/types";
import { CompactMultiSelect, ConfirmDialog, DateRangePopover, EntityHistoryDialog, ExportButton, FilteredDataGrid, GlobalSearchBar, InlineTextFilter, PageContainer, apiClient, useConfirm, useHasPermission } from "@broker/ui-kit";
import type { ExcelColumn } from "@broker/ui-kit";
import { CreateAccountDialog, EditAccountDialog } from "./AccountDialogs";
import { useSearchParams, useNavigate } from "react-router-dom";
import type { PagedResult } from "../api/types";

const STATUS_OPTIONS: { value: AccountStatus; label: string }[] = [
  { value: "Active", label: "Active" },
  { value: "Blocked", label: "Blocked" },
  { value: "Closed", label: "Closed" },
  { value: "Suspended", label: "Suspended" },
];

const ACCOUNT_TYPE_OPTIONS: { value: AccountType; label: string }[] = [
  { value: "Individual", label: "Individual" },
  { value: "Corporate", label: "Corporate" },
  { value: "Joint", label: "Joint" },
  { value: "Trust", label: "Trust" },
  { value: "IRA", label: "IRA" },
];

const MARGIN_TYPE_OPTIONS: { value: MarginType; label: string }[] = [
  { value: "Cash", label: "Cash" },
  { value: "MarginX1", label: "Margin X1" },
  { value: "MarginX2", label: "Margin X2" },
  { value: "MarginX4", label: "Margin X4" },
  { value: "DayTrader", label: "Day Trader" },
];

const TARIFF_OPTIONS: { value: Tariff; label: string }[] = [
  { value: "Basic", label: "Basic" },
  { value: "Standard", label: "Standard" },
  { value: "Premium", label: "Premium" },
  { value: "VIP", label: "VIP" },
];

const STATUS_COLORS: Record<AccountStatus, "success" | "error" | "default" | "warning"> = {
  Active: "success",
  Blocked: "error",
  Closed: "default",
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
    number: sp.get("number") || undefined,
    status: getAllOrUndefined(sp, "status") as AccountStatus[] | undefined,
    accountType: getAllOrUndefined(sp, "accountType") as AccountType[] | undefined,
    marginType: getAllOrUndefined(sp, "marginType") as MarginType[] | undefined,
    tariff: getAllOrUndefined(sp, "tariff") as Tariff[] | undefined,
    clearerName: sp.get("clearerName") || undefined,
    tradePlatformName: sp.get("tradePlatformName") || undefined,
    externalId: sp.get("externalId") || undefined,
    createdFrom: sp.get("createdFrom") || undefined,
    createdTo: sp.get("createdTo") || undefined,
  };
}

export function AccountsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  const params = readParams(searchParams);

  const [createOpen, setCreateOpen] = useState(false);
  const [editAccount, setEditAccount] = useState<AccountListItemDto | null>(null);
  const [historyEntityId, setHistoryEntityId] = useState<string | null>(null);

  const canCreate = useHasPermission("accounts.create");
  const canUpdate = useHasPermission("accounts.update");
  const canDelete = useHasPermission("accounts.delete");
  const canAudit = useHasPermission("audit.read");

  const { data, isLoading } = useAccounts(params);
  const deleteAccount = useDeleteAccount();
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
    params.q || params.number || params.clearerName || params.tradePlatformName || params.externalId ||
    params.status?.length || params.accountType?.length || params.marginType?.length || params.tariff?.length ||
    params.createdFrom || params.createdTo
  );

  const handlePagination = (model: GridPaginationModel) => {
    setParam({ page: String(model.page + 1), pageSize: String(model.pageSize) });
  };

  const handleSort = (model: GridSortModel) => {
    const s = model[0];
    setParam({ sort: s ? `${s.field} ${s.sort}` : undefined, page: "1" });
  };
  const sortModel: GridSortModel = useMemo(() => {
    if (!params.sort) return [];
    const [field, dir] = params.sort.split(" ");
    return [{ field, sort: dir as GridSortDirection }];
  }, [params.sort]);

  const handleDelete = async (id: string) => {
    const ok = await confirm({ title: "Delete Account", message: "Are you sure you want to delete this account?" });
    if (!ok) return;
    try { await deleteAccount.mutateAsync(id); } catch { /* handled by MutationCache */ }
  };

  const columns: GridColDef<AccountListItemDto>[] = useMemo(() => [
    { field: "number", headerName: "Number", flex: 1, minWidth: 120 },
    {
      field: "status", headerName: "Status", width: 120,
      renderCell: ({ value }) => (
        <Chip label={value} color={STATUS_COLORS[value as AccountStatus] ?? "default"} size="small" />
      ),
    },
    { field: "accountType", headerName: "Type", width: 110 },
    { field: "marginType", headerName: "Margin", width: 100 },
    {
      field: "tariff", headerName: "Tariff", width: 100,
      renderCell: ({ value }) => <Chip label={value} size="small" variant="outlined" />,
    },
    { field: "optionLevel", headerName: "Options", width: 100 },
    { field: "clearerName", headerName: "Clearer", flex: 1, minWidth: 130 },
    { field: "tradePlatformName", headerName: "Platform", flex: 1, minWidth: 130 },
    {
      field: "openedAt", headerName: "Opened", width: 110,
      renderCell: ({ value }) => value ? new Date(value as string).toLocaleDateString() : "—",
    },
    { field: "externalId", headerName: "External ID", width: 120 },
    {
      field: "createdAt", headerName: "Created", width: 170,
      renderCell: ({ value }) => new Date(value as string).toLocaleString(),
    },
    {
      field: "actions", headerName: "", width: 150, sortable: false, filterable: false, disableColumnMenu: true,
      renderCell: ({ row }) => (
        <div onClick={(e) => e.stopPropagation()}>
          <IconButton size="small" onClick={() => navigate(`/accounts/${row.id}`)} data-testid={`action-view-${row.id}`}>
            <VisibilityIcon fontSize="small" />
          </IconButton>
          {canAudit && (
            <IconButton size="small" onClick={() => setHistoryEntityId(row.id)}>
              <HistoryIcon fontSize="small" />
            </IconButton>
          )}
          {canUpdate && (
            <IconButton size="small" onClick={() => setEditAccount(row)} data-testid={`action-edit-${row.id}`}>
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
  ], [navigate, canAudit, canUpdate, canDelete, handleDelete]);

  const filterDefs = useMemo(() => {
    const m = new Map<string, () => ReactNode>();
    m.set("number", () => (
      <InlineTextFilter
        value={params.number ?? ""}
        onChange={(v) => setFilterParam("number", v || undefined)}
        placeholder="Number..."
      />
    ));
    m.set("status", () => (
      <CompactMultiSelect
        options={STATUS_OPTIONS}
        value={params.status ?? []}
        onChange={(v) => setMultiFilterParam("status", v)}
      />
    ));
    m.set("accountType", () => (
      <CompactMultiSelect
        options={ACCOUNT_TYPE_OPTIONS}
        value={params.accountType ?? []}
        onChange={(v) => setMultiFilterParam("accountType", v)}
      />
    ));
    m.set("marginType", () => (
      <CompactMultiSelect
        options={MARGIN_TYPE_OPTIONS}
        value={params.marginType ?? []}
        onChange={(v) => setMultiFilterParam("marginType", v)}
      />
    ));
    m.set("tariff", () => (
      <CompactMultiSelect
        options={TARIFF_OPTIONS}
        value={params.tariff ?? []}
        onChange={(v) => setMultiFilterParam("tariff", v)}
      />
    ));
    m.set("clearerName", () => (
      <InlineTextFilter
        value={params.clearerName ?? ""}
        onChange={(v) => setFilterParam("clearerName", v || undefined)}
        placeholder="Clearer..."
      />
    ));
    m.set("tradePlatformName", () => (
      <InlineTextFilter
        value={params.tradePlatformName ?? ""}
        onChange={(v) => setFilterParam("tradePlatformName", v || undefined)}
        placeholder="Platform..."
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
  }, [params.number, params.status, params.accountType, params.marginType, params.tariff,
      params.clearerName, params.tradePlatformName, params.externalId,
      params.createdFrom, params.createdTo,
      setFilterParam, setMultiFilterParam]);

  const exportColumns: ExcelColumn<AccountListItemDto>[] = useMemo(() => [
    { header: "Number", value: (r) => r.number },
    { header: "Status", value: (r) => r.status },
    { header: "Type", value: (r) => r.accountType },
    { header: "Margin", value: (r) => r.marginType },
    { header: "Tariff", value: (r) => r.tariff },
    { header: "Option Level", value: (r) => r.optionLevel },
    { header: "Clearer", value: (r) => r.clearerName },
    { header: "Platform", value: (r) => r.tradePlatformName },
    { header: "Opened", value: (r) => r.openedAt ? new Date(r.openedAt).toLocaleDateString() : "" },
    { header: "External ID", value: (r) => r.externalId },
    { header: "Created", value: (r) => r.createdAt ? new Date(r.createdAt).toLocaleString() : "" },
  ], []);

  const fetchAllAccounts = useCallback(async () => {
    const { page: _, pageSize: __, ...filters } = params;
    const resp = await apiClient.get<PagedResult<AccountListItemDto>>("/accounts", {
      params: { ...filters, page: 1, pageSize: 10000 },
    });
    return resp.data.items;
  }, [params]);

  return (
    <PageContainer
      variant="list"
      title="Accounts"
      actions={
        <>
          {hasActiveFilters && (
            <Tooltip title="Clear all filters">
              <IconButton size="small" aria-label="Clear all filters" onClick={clearAllFilters}><FilterListOffIcon /></IconButton>
            </Tooltip>
          )}
          {canAudit && (
            <Button variant="outlined" startIcon={<HistoryIcon />} onClick={() => navigate("/audit?entityType=Account")}>
              History
            </Button>
          )}
          <ExportButton fetchData={fetchAllAccounts} columns={exportColumns} filename="accounts" />
          {canCreate && (
            <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
              Create Account
            </Button>
          )}
        </>
      }
      subheaderLeft={
        <GlobalSearchBar
          value={params.q ?? ""}
          onChange={(v) => setFilterParam("q", v || undefined)}
          placeholder="Search accounts..."
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
          sortModel={sortModel}
          onSortModelChange={handleSort}
          pageSizeOptions={[10, 25, 50]}
          filterDefs={filterDefs}
          onRowClick={(p) => navigate(`/accounts/${p.row.id}`)}
          initialState={{
            columns: {
              columnVisibilityModel: {
                externalId: false,
                optionLevel: false,
                openedAt: false,
              },
            },
          }}
          sx={{ height: "100%", border: "none", "& .MuiDataGrid-row": { cursor: "pointer" } }}
        />
      </Paper>

      <CreateAccountDialog open={createOpen} onClose={() => setCreateOpen(false)} />
      <EditAccountDialog open={!!editAccount} onClose={() => setEditAccount(null)} account={editAccount} />
      <ConfirmDialog {...confirmDialogProps} isLoading={deleteAccount.isPending} />
      <EntityHistoryDialog entityType="Account" entityId={historyEntityId ?? ""} open={historyEntityId !== null} onClose={() => setHistoryEntityId(null)} />
    </PageContainer>
  );
}
