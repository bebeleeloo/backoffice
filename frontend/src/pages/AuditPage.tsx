import { useState, useCallback, useMemo, type ReactNode } from "react";
import { Paper, Chip } from "@mui/material";
import { type GridColDef, type GridPaginationModel, type GridSortModel } from "@mui/x-data-grid";
import { useAllEntityChanges, useUsers } from "../api/hooks";
import type { GlobalOperationDto, PagedResult } from "../api/types";
import { useSearchParams } from "react-router-dom";
import { ExportButton } from "../components/ExportButton";
import type { ExcelColumn } from "../utils/exportToExcel";
import { apiClient } from "../api/client";
import { PageContainer } from "../components/PageContainer";
import { FilteredDataGrid, InlineTextFilter, CompactMultiSelect, DateRangePopover } from "../components/grid";
import { GlobalSearchBar } from "../components/GlobalSearchBar";
import { CHANGE_TYPE_COLORS, getEntityTypeLabel } from "../components/changeHistoryUtils";
import { AuditDetailDialog } from "../components/AuditDetailDialog";

const ENTITY_TYPE_OPTIONS = [
  { value: "Client", label: "Client" },
  { value: "Account", label: "Account" },
  { value: "Instrument", label: "Instrument" },
  { value: "User", label: "User" },
  { value: "Role", label: "Role" },
];

const CHANGE_TYPE_OPTIONS = [
  { value: "Created", label: "Created" },
  { value: "Modified", label: "Modified" },
  { value: "Deleted", label: "Deleted" },
];

function getAllOrUndefined(sp: URLSearchParams, key: string) {
  const vals = sp.getAll(key);
  return vals.length > 0 ? vals : undefined;
}

function readParams(sp: URLSearchParams) {
  return {
    page: Number(sp.get("page") || "1"),
    pageSize: Number(sp.get("pageSize") || "25"),
    sort: sp.get("sort") || undefined,
    from: sp.get("from") || undefined,
    to: sp.get("to") || undefined,
    userName: getAllOrUndefined(sp, "userName"),
    entityType: sp.get("entityType") || undefined,
    changeType: sp.get("changeType") || undefined,
    q: sp.get("q") || undefined,
  };
}

function formatTimestamp(iso: string) {
  const d = new Date(iso);
  return d.toLocaleDateString(undefined, { year: "numeric", month: "short", day: "numeric" })
    + ", " + d.toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit", second: "2-digit" });
}

export function AuditPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const params = readParams(searchParams);

  const [selectedOp, setSelectedOp] = useState<GlobalOperationDto | null>(null);

  const { data, isLoading } = useAllEntityChanges(params);
  const { data: usersData } = useUsers({ page: 1, pageSize: 500 });

  const userOptions = useMemo(
    () => (usersData?.items ?? [])
      .map((u) => ({ value: u.fullName ?? u.username, label: u.fullName ?? u.username }))
      .filter((o): o is { value: string; label: string } => !!o.value),
    [usersData],
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

  const columns: GridColDef<GlobalOperationDto>[] = [
    {
      field: "timestamp", headerName: "Date & Time", width: 180,
      renderCell: ({ value }) => formatTimestamp(value as string),
    },
    {
      field: "userName", headerName: "User", flex: 1, minWidth: 140,
      renderCell: ({ value }) => value ?? "system",
    },
    {
      field: "entityType", headerName: "Entity", width: 130,
      renderCell: ({ value }) => getEntityTypeLabel(value as string) || value,
    },
    {
      field: "entityDisplayName", headerName: "Name", flex: 1, minWidth: 160,
    },
    {
      field: "changeType", headerName: "Change", width: 120,
      renderCell: ({ value }) => {
        const color = CHANGE_TYPE_COLORS[value as string] ?? "default";
        return <Chip label={value as string} color={color} size="small" />;
      },
    },
  ];

  const filterDefs = useMemo(() => {
    const m = new Map<string, () => ReactNode>();
    m.set("timestamp", () => (
      <DateRangePopover
        fromValue={params.from ?? ""}
        toValue={params.to ?? ""}
        onFromChange={(v) => setFilterParam("from", v || undefined)}
        onToChange={(v) => setFilterParam("to", v || undefined)}
      />
    ));
    m.set("userName", () => (
      <CompactMultiSelect
        options={userOptions}
        value={params.userName ?? []}
        onChange={(v) => setMultiFilterParam("userName", v)}
      />
    ));
    m.set("entityType", () => (
      <CompactMultiSelect
        options={ENTITY_TYPE_OPTIONS}
        value={params.entityType ? [params.entityType] : []}
        onChange={(v) => setFilterParam("entityType", v.length === 1 ? v[0] : undefined)}
      />
    ));
    m.set("entityDisplayName", () => (
      <InlineTextFilter
        value={params.q ?? ""}
        onChange={(v) => setFilterParam("q", v || undefined)}
        placeholder="Name..."
      />
    ));
    m.set("changeType", () => (
      <CompactMultiSelect
        options={CHANGE_TYPE_OPTIONS}
        value={params.changeType ? [params.changeType] : []}
        onChange={(v) => setFilterParam("changeType", v.length === 1 ? v[0] : undefined)}
      />
    ));
    return m;
  }, [params.from, params.to, params.userName, params.entityType, params.q, params.changeType, setFilterParam, setMultiFilterParam, userOptions]);

  const exportColumns: ExcelColumn<GlobalOperationDto>[] = useMemo(() => [
    { header: "Date & Time", value: (r) => formatTimestamp(r.timestamp) },
    { header: "User", value: (r) => r.userName ?? "system" },
    { header: "Entity", value: (r) => getEntityTypeLabel(r.entityType) || r.entityType },
    { header: "Name", value: (r) => r.entityDisplayName },
    { header: "Change", value: (r) => r.changeType },
  ], []);

  const fetchAllAudit = useCallback(async () => {
    const { page: _, pageSize: __, ...filters } = params;
    const resp = await apiClient.get<PagedResult<GlobalOperationDto>>("/entity-changes/all", {
      params: { ...filters, page: 1, pageSize: 10000 },
    });
    return resp.data.items;
  }, [params]);

  return (
    <PageContainer
      variant="list"
      title="Audit Log"
      actions={
        <ExportButton fetchData={fetchAllAudit} columns={exportColumns} filename="audit-log" />
      }
      subheaderLeft={
        <GlobalSearchBar
          placeholder="Search audit logs..."
          value={params.q ?? ""}
          onChange={(v) => setFilterParam("q", v || undefined)}
        />
      }
    >
      <Paper variant="outlined" sx={{ flex: 1, minHeight: 0, overflow: "hidden" }}>
        <FilteredDataGrid
          rows={data?.items ?? []}
          columns={columns}
          getRowId={(row) => `${row.operationId}-${row.entityType}-${row.entityId}`}
          rowCount={data?.totalCount ?? 0}
          loading={isLoading}
          paginationMode="server"
          sortingMode="server"
          paginationModel={{ page: params.page - 1, pageSize: params.pageSize }}
          onPaginationModelChange={handlePagination}
          onSortModelChange={handleSort}
          pageSizeOptions={[10, 25, 50]}
          filterDefs={filterDefs}
          onRowClick={(p) => setSelectedOp(p.row as GlobalOperationDto)}
          sx={{ height: "100%", border: "none", "& .MuiDataGrid-row": { cursor: "pointer" } }}
        />
      </Paper>

      <AuditDetailDialog
        operation={selectedOp}
        open={!!selectedOp}
        onClose={() => setSelectedOp(null)}
      />
    </PageContainer>
  );
}
