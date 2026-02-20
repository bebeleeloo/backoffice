import { useState, useCallback, useEffect, useMemo, type ReactNode } from "react";
import {
  Button, IconButton, Chip, Paper, Tooltip, TextField, InputAdornment,
} from "@mui/material";
import {
  type GridColDef,
  type GridPaginationModel,
  type GridSortModel,
} from "@mui/x-data-grid";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import VisibilityIcon from "@mui/icons-material/Visibility";
import FilterListOffIcon from "@mui/icons-material/FilterListOff";
import SearchIcon from "@mui/icons-material/Search";
import { useDebounce } from "../hooks/useDebounce";
import { useClients, useDeleteClient, useCountries } from "../api/hooks";
import type {
  ClientListItemDto,
  ClientType,
  ClientStatus,
  KycStatus,
  RiskLevel,
} from "../api/types";
import { useHasPermission } from "../auth/usePermission";
import { CreateClientDialog, EditClientDialog } from "./ClientDialogs";
import { useSearchParams, useNavigate } from "react-router-dom";
import { PageContainer } from "../components/PageContainer";
import {
  FilteredDataGrid,
  InlineTextFilter,
  CompactMultiSelect,
  CompactCountrySelect,
  DateRangePopover,
  InlineBooleanFilter,
} from "../components/grid";

/* ── Chip color maps ── */

const STATUS_COLORS: Record<string, "success" | "error" | "warning" | "default"> = {
  Active: "success", Blocked: "error", PendingKyc: "warning",
};
const KYC_COLORS: Record<string, "success" | "error" | "warning" | "info" | "default"> = {
  Approved: "success", Rejected: "error", InProgress: "info", NotStarted: "default",
};
const RISK_COLORS: Record<string, "success" | "warning" | "error" | "default"> = {
  Low: "success", Medium: "warning", High: "error",
};

/* ── Filter options ── */

const STATUS_OPTIONS: { value: ClientStatus; label: string }[] = [
  { value: "Active", label: "Active" },
  { value: "Blocked", label: "Blocked" },
  { value: "PendingKyc", label: "Pending KYC" },
];
const TYPE_OPTIONS: { value: ClientType; label: string }[] = [
  { value: "Individual", label: "Individual" },
  { value: "Corporate", label: "Corporate" },
];
const KYC_OPTIONS: { value: KycStatus; label: string }[] = [
  { value: "NotStarted", label: "Not Started" },
  { value: "InProgress", label: "In Progress" },
  { value: "Approved", label: "Approved" },
  { value: "Rejected", label: "Rejected" },
];
const RISK_OPTIONS: { value: RiskLevel; label: string }[] = [
  { value: "Low", label: "Low" },
  { value: "Medium", label: "Medium" },
  { value: "High", label: "High" },
];

/* ── URL helpers ── */

function getAllOrUndefined(sp: URLSearchParams, key: string): string[] | undefined {
  const vals = sp.getAll(key);
  return vals.length > 0 ? vals : undefined;
}

function readParams(sp: URLSearchParams) {
  return {
    page: Number(sp.get("page") || "1"),
    pageSize: Number(sp.get("pageSize") || "25"),
    sort: sp.get("sort") || undefined,
    q: sp.get("q") || undefined,
    name: sp.get("name") || undefined,
    email: sp.get("email") || undefined,
    phone: sp.get("phone") || undefined,
    externalId: sp.get("externalId") || undefined,
    residenceCountryName: sp.get("residenceCountryName") || undefined,
    citizenshipCountryName: sp.get("citizenshipCountryName") || undefined,
    status: getAllOrUndefined(sp, "status") as ClientStatus[] | undefined,
    clientType: getAllOrUndefined(sp, "clientType") as ClientType[] | undefined,
    kycStatus: getAllOrUndefined(sp, "kycStatus") as KycStatus[] | undefined,
    riskLevel: getAllOrUndefined(sp, "riskLevel") as RiskLevel[] | undefined,
    residenceCountryIds: getAllOrUndefined(sp, "residenceCountryIds"),
    citizenshipCountryIds: getAllOrUndefined(sp, "citizenshipCountryIds"),
    residenceCountryId: sp.get("residenceCountryId") || undefined,
    createdFrom: sp.get("createdFrom") || undefined,
    createdTo: sp.get("createdTo") || undefined,
    pepStatus:
      sp.get("pepStatus") === "true"
        ? true
        : sp.get("pepStatus") === "false"
          ? false
          : undefined,
  };
}

/* ── Global search bar ── */

function GlobalSearchBar({
  value,
  onChange,
}: {
  value: string;
  onChange: (v: string) => void;
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
      placeholder="Search clients..."
      value={local}
      onChange={(e) => setLocal(e.target.value)}
      slotProps={{
        input: {
          startAdornment: (
            <InputAdornment position="start">
              <SearchIcon sx={{ fontSize: 20, color: "action.disabled" }} />
            </InputAdornment>
          ),
        },
      }}
      fullWidth
    />
  );
}

/* ── Page ── */

export function ClientsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  const params = useMemo(() => readParams(searchParams), [searchParams]);

  const [createOpen, setCreateOpen] = useState(false);
  const [editClientId, setEditClientId] = useState<string | null>(null);

  const canCreate = useHasPermission("clients.create");
  const canUpdate = useHasPermission("clients.update");
  const canDelete = useHasPermission("clients.delete");

  const { data, isLoading } = useClients(params);
  const deleteClient = useDeleteClient();
  const { data: countries = [] } = useCountries();

  /* ── Filter helpers ── */

  const setFilterParam = useCallback(
    (key: string, value: string | string[] | undefined) => {
      setSearchParams((prev) => {
        const next = new URLSearchParams(prev);
        next.delete(key);
        if (value !== undefined && value !== "") {
          if (Array.isArray(value)) value.forEach((v) => next.append(key, v));
          else next.set(key, value);
        }
        next.set("page", "1");
        return next;
      });
    },
    [setSearchParams],
  );

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

  const clearAllFilters = useCallback(() => {
    setSearchParams(new URLSearchParams());
  }, [setSearchParams]);

  const hasActiveFilters = !!(
    params.q || params.name || params.email || params.phone || params.externalId ||
    params.residenceCountryName || params.citizenshipCountryName ||
    params.status?.length || params.clientType?.length ||
    params.kycStatus?.length || params.riskLevel?.length ||
    params.residenceCountryIds?.length || params.citizenshipCountryIds?.length ||
    params.createdFrom || params.createdTo || params.pepStatus !== undefined
  );

  /* ── Pagination / sort ── */

  const handlePagination = (model: GridPaginationModel) => {
    setParam({ page: String(model.page + 1), pageSize: String(model.pageSize) });
  };
  const handleSort = (model: GridSortModel) => {
    const s = model[0];
    setParam({ sort: s ? `${s.field} ${s.sort}` : undefined, page: "1" });
  };
  const handleDelete = async (id: string) => {
    if (!confirm("Delete this client?")) return;
    await deleteClient.mutateAsync(id);
  };

  /* ── Filter definitions: field → render fn ── */

  const filterDefs = useMemo(() => {
    const m = new Map<string, () => ReactNode>();

    const text = (key: string, ph?: string) => () => (
      <InlineTextFilter
        value={(params as Record<string, unknown>)[key] as string ?? ""}
        onChange={(v) => setFilterParam(key, v || undefined)}
        placeholder={ph}
      />
    );
    const multiEnum = <T extends string>(
      key: string,
      opts: { value: T; label: string }[],
    ) => () => (
      <CompactMultiSelect
        options={opts}
        value={((params as Record<string, unknown>)[key] as T[] | undefined) ?? []}
        onChange={(v) => setFilterParam(key, v.length ? v : undefined)}
      />
    );
    const country = (key: string) => () => (
      <CompactCountrySelect
        countries={countries}
        value={((params as Record<string, unknown>)[key] as string[] | undefined) ?? []}
        onChange={(v) => setFilterParam(key, v.length ? v : undefined)}
      />
    );

    m.set("displayName", text("name", "Name..."));
    m.set("clientType", multiEnum("clientType", TYPE_OPTIONS));
    m.set("email", text("email", "Email..."));
    m.set("status", multiEnum("status", STATUS_OPTIONS));
    m.set("kycStatus", multiEnum("kycStatus", KYC_OPTIONS));
    m.set("residenceCountryIso2", country("residenceCountryIds"));
    m.set("createdAt", () => (
      <DateRangePopover
        fromValue={params.createdFrom ?? ""}
        toValue={params.createdTo ?? ""}
        onFromChange={(v) => setFilterParam("createdFrom", v || undefined)}
        onToChange={(v) => setFilterParam("createdTo", v || undefined)}
      />
    ));
    m.set("phone", text("phone", "Phone..."));
    m.set("externalId", text("externalId", "Ext ID..."));
    m.set("pepStatus", () => (
      <InlineBooleanFilter
        value={params.pepStatus}
        onChange={(v) =>
          setFilterParam("pepStatus", v === undefined ? undefined : String(v))
        }
      />
    ));
    m.set("riskLevel", multiEnum("riskLevel", RISK_OPTIONS));
    m.set("citizenshipCountryIso2", country("citizenshipCountryIds"));
    m.set("residenceCountryName", text("residenceCountryName", "Country..."));
    m.set("citizenshipCountryName", text("citizenshipCountryName", "Country..."));

    return m;
  }, [params, setFilterParam, countries]);

  /* ── Columns ── */

  const columns: GridColDef<ClientListItemDto>[] = [
    { field: "displayName", headerName: "Name", flex: 1, minWidth: 180 },
    {
      field: "clientType", headerName: "Type", width: 130,
      renderCell: ({ value }) => <Chip label={value} size="small" variant="outlined" />,
    },
    { field: "email", headerName: "Email", flex: 1, minWidth: 200 },
    {
      field: "status", headerName: "Status", width: 140,
      renderCell: ({ value }) => (
        <Chip label={value} color={STATUS_COLORS[value as string] ?? "default"} size="small" />
      ),
    },
    {
      field: "kycStatus", headerName: "KYC", width: 140,
      renderCell: ({ value }) => (
        <Chip label={value} color={KYC_COLORS[value as string] ?? "default"} size="small" />
      ),
    },
    {
      field: "residenceCountryIso2", headerName: "Res. Country", width: 150,
      renderCell: ({ row }) =>
        row.residenceCountryFlagEmoji
          ? `${row.residenceCountryFlagEmoji} ${row.residenceCountryIso2}`
          : row.residenceCountryIso2,
    },
    {
      field: "createdAt", headerName: "Created", width: 180,
      valueFormatter: (value: string) =>
        value ? new Date(value).toLocaleDateString() : "",
    },
    // Hidden by default
    { field: "phone", headerName: "Phone", width: 140 },
    { field: "externalId", headerName: "External ID", width: 140 },
    {
      field: "pepStatus", headerName: "PEP", width: 100,
      renderCell: ({ value }) => (
        <Chip label={value ? "Yes" : "No"} color={value ? "error" : "default"} size="small" />
      ),
    },
    {
      field: "riskLevel", headerName: "Risk", width: 130,
      renderCell: ({ value }) =>
        value ? <Chip label={value} color={RISK_COLORS[value as string] ?? "default"} size="small" /> : "—",
    },
    {
      field: "citizenshipCountryIso2", headerName: "Citizenship", width: 150,
      renderCell: ({ row }) =>
        row.citizenshipCountryFlagEmoji
          ? `${row.citizenshipCountryFlagEmoji} ${row.citizenshipCountryIso2}`
          : row.citizenshipCountryIso2 ?? "—",
    },
    { field: "residenceCountryName", headerName: "Res. Country Name", width: 170 },
    { field: "citizenshipCountryName", headerName: "Citizenship Name", width: 170 },
    {
      field: "actions", headerName: "", width: 120,
      sortable: false, filterable: false, disableColumnMenu: true,
      renderCell: ({ row }) => (
        <>
          <IconButton size="small" onClick={() => navigate(`/clients/${row.id}`)} data-testid={`action-view-${row.id}`}>
            <VisibilityIcon fontSize="small" />
          </IconButton>
          {canUpdate && (
            <IconButton size="small" onClick={() => setEditClientId(row.id)} data-testid={`action-edit-${row.id}`}>
              <EditIcon fontSize="small" />
            </IconButton>
          )}
          {canDelete && (
            <IconButton size="small" onClick={() => handleDelete(row.id)} color="error" data-testid={`action-delete-${row.id}`}>
              <DeleteIcon fontSize="small" />
            </IconButton>
          )}
        </>
      ),
    },
  ];

  const columnVisibilityModel: Record<string, boolean> = {
    phone: false,
    externalId: false,
    pepStatus: false,
    riskLevel: false,
    citizenshipCountryIso2: false,
    residenceCountryName: false,
    citizenshipCountryName: false,
  };

  /* ── Render ── */

  return (
    <PageContainer
      variant="list"
      title="Clients"
      actions={
        <>
          {hasActiveFilters && (
            <Tooltip title="Clear all filters">
              <IconButton size="small" onClick={clearAllFilters} data-testid="clear-all-filters">
                <FilterListOffIcon />
              </IconButton>
            </Tooltip>
          )}
          {canCreate && (
            <Button
              variant="contained"
              startIcon={<AddIcon />}
              onClick={() => setCreateOpen(true)}
            >
              Create Client
            </Button>
          )}
        </>
      }
      subheaderLeft={
        <GlobalSearchBar
          value={params.q ?? ""}
          onChange={(v) => setFilterParam("q", v || undefined)}
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
          initialState={{ columns: { columnVisibilityModel } }}
          filterDefs={filterDefs}
          sx={{ height: "100%", border: "none" }}
        />
      </Paper>

      <CreateClientDialog open={createOpen} onClose={() => setCreateOpen(false)} />
      <EditClientDialog
        open={!!editClientId}
        onClose={() => setEditClientId(null)}
        clientId={editClientId}
      />
    </PageContainer>
  );
}
