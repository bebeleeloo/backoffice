import { useState, useCallback, useMemo, type ReactNode } from "react";
import { Button, IconButton, Chip, TextField, InputAdornment, Paper } from "@mui/material";
import { type GridColDef, type GridPaginationModel, type GridSortModel } from "@mui/x-data-grid";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import VisibilityIcon from "@mui/icons-material/Visibility";
import SearchIcon from "@mui/icons-material/Search";
import { useAccounts, useDeleteAccount } from "../api/hooks";
import type { AccountListItemDto, AccountStatus, AccountType, MarginType, Tariff } from "../api/types";
import { useHasPermission } from "../auth/usePermission";
import { CreateAccountDialog, EditAccountDialog } from "./AccountDialogs";
import { useSearchParams, useNavigate } from "react-router-dom";
import { PageContainer } from "../components/PageContainer";
import { FilteredDataGrid, InlineTextFilter, CompactMultiSelect } from "../components/grid";

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
  };
}

export function AccountsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  const params = readParams(searchParams);

  const [createOpen, setCreateOpen] = useState(false);
  const [editAccount, setEditAccount] = useState<AccountListItemDto | null>(null);
  const [search, setSearch] = useState(params.q ?? "");

  const canCreate = useHasPermission("accounts.create");
  const canUpdate = useHasPermission("accounts.update");
  const canDelete = useHasPermission("accounts.delete");

  const { data, isLoading } = useAccounts(params);
  const deleteAccount = useDeleteAccount();

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
    if (!confirm("Delete this account?")) return;
    await deleteAccount.mutateAsync(id);
  };

  const columns: GridColDef<AccountListItemDto>[] = [
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
      field: "actions", headerName: "", width: 120, sortable: false, filterable: false, disableColumnMenu: true,
      renderCell: ({ row }) => (
        <div onClick={(e) => e.stopPropagation()}>
          <IconButton size="small" onClick={() => navigate(`/accounts/${row.id}`)} data-testid={`action-view-${row.id}`}>
            <VisibilityIcon fontSize="small" />
          </IconButton>
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
  ];

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
    return m;
  }, [params.number, params.status, params.accountType, params.marginType, params.tariff,
      params.clearerName, params.tradePlatformName, params.externalId,
      setFilterParam, setMultiFilterParam]);

  return (
    <PageContainer
      variant="list"
      title="Accounts"
      actions={
        canCreate ? (
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
            Create Account
          </Button>
        ) : undefined
      }
      subheaderLeft={
        <TextField
          fullWidth
          placeholder="Search accounts..."
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
    </PageContainer>
  );
}
