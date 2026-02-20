import { useState, useCallback, useMemo, type ReactNode } from "react";
import {
  Box, Chip, TextField, InputAdornment, IconButton, Paper,
  Dialog, DialogTitle, DialogContent, DialogActions, Button,
} from "@mui/material";
import { type GridColDef, type GridPaginationModel, type GridSortModel } from "@mui/x-data-grid";
import SearchIcon from "@mui/icons-material/Search";
import VisibilityIcon from "@mui/icons-material/Visibility";
import { useAuditLogs, useAuditLog } from "../api/hooks";
import type { AuditLogDto } from "../api/types";
import { useSearchParams } from "react-router-dom";
import dayjs from "dayjs";
import { PageContainer } from "../components/PageContainer";
import {
  FilteredDataGrid,
  InlineTextFilter,
  InlineBooleanFilter,
  DateRangePopover,
} from "../components/grid";

function readParams(sp: URLSearchParams) {
  return {
    page: Number(sp.get("page") || "1"),
    pageSize: Number(sp.get("pageSize") || "25"),
    sort: sp.get("sort") || undefined,
    q: sp.get("q") || undefined,
    from: sp.get("from") || undefined,
    to: sp.get("to") || undefined,
    action: sp.get("action") || undefined,
    entityType: sp.get("entityType") || undefined,
    isSuccess: sp.get("isSuccess") !== null ? sp.get("isSuccess") === "true" : undefined,
    userName: sp.get("userName") || undefined,
    method: sp.get("method") || undefined,
    path: sp.get("path") || undefined,
    statusCode: sp.get("statusCode") ? Number(sp.get("statusCode")) : undefined,
  };
}

export function AuditPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const params = readParams(searchParams);

  const [search, setSearch] = useState(params.q ?? "");
  const [detailId, setDetailId] = useState<string | null>(null);

  const { data, isLoading } = useAuditLogs(params);
  const detail = useAuditLog(detailId ?? "");

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

  const columns: GridColDef<AuditLogDto>[] = [
    {
      field: "createdAt", headerName: "Time", width: 170,
      valueFormatter: (value: string) => dayjs(value).format("YYYY-MM-DD HH:mm:ss"),
    },
    { field: "userName", headerName: "User", width: 120 },
    { field: "action", headerName: "Action", width: 160 },
    { field: "entityType", headerName: "Entity", width: 100 },
    { field: "method", headerName: "Method", width: 80 },
    { field: "path", headerName: "Path", flex: 1, minWidth: 200 },
    { field: "statusCode", headerName: "Status", width: 80 },
    {
      field: "isSuccess", headerName: "Result", width: 90,
      renderCell: ({ value }) => (
        <Chip label={value ? "OK" : "Fail"} color={value ? "success" : "error"} size="small" />
      ),
    },
    {
      field: "actions", headerName: "", width: 60, sortable: false, filterable: false,
      renderCell: ({ row }) => (
        <IconButton size="small" onClick={() => setDetailId(row.id)} data-testid={`action-view-${row.id}`}>
          <VisibilityIcon fontSize="small" />
        </IconButton>
      ),
    },
  ];

  const filterDefs = useMemo(() => {
    const m = new Map<string, () => ReactNode>();
    m.set("createdAt", () => (
      <DateRangePopover
        fromValue={params.from ?? ""}
        toValue={params.to ?? ""}
        onFromChange={(v) => setFilterParam("from", v || undefined)}
        onToChange={(v) => setFilterParam("to", v || undefined)}
      />
    ));
    m.set("userName", () => (
      <InlineTextFilter
        value={params.userName ?? ""}
        onChange={(v) => setFilterParam("userName", v || undefined)}
        placeholder="User..."
      />
    ));
    m.set("action", () => (
      <InlineTextFilter
        value={params.action ?? ""}
        onChange={(v) => setFilterParam("action", v || undefined)}
        placeholder="Action..."
      />
    ));
    m.set("entityType", () => (
      <InlineTextFilter
        value={params.entityType ?? ""}
        onChange={(v) => setFilterParam("entityType", v || undefined)}
        placeholder="Entity..."
      />
    ));
    m.set("method", () => (
      <InlineTextFilter
        value={params.method ?? ""}
        onChange={(v) => setFilterParam("method", v || undefined)}
        placeholder="Method..."
      />
    ));
    m.set("path", () => (
      <InlineTextFilter
        value={params.path ?? ""}
        onChange={(v) => setFilterParam("path", v || undefined)}
        placeholder="Path..."
      />
    ));
    m.set("statusCode", () => (
      <InlineTextFilter
        value={params.statusCode !== undefined ? String(params.statusCode) : ""}
        onChange={(v) => setFilterParam("statusCode", v || undefined)}
        placeholder="Status..."
      />
    ));
    m.set("isSuccess", () => (
      <InlineBooleanFilter
        value={params.isSuccess}
        onChange={(v) =>
          setFilterParam("isSuccess", v === undefined ? undefined : String(v))
        }
      />
    ));
    return m;
  }, [params.from, params.to, params.userName, params.action, params.entityType, params.method, params.path, params.statusCode, params.isSuccess, setFilterParam]);

  return (
    <PageContainer
      variant="list"
      title="Audit Log"
      subheaderLeft={
        <TextField
          fullWidth
          placeholder="Search..."
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
          sx={{ height: "100%", border: "none" }}
        />
      </Paper>

      <Dialog open={!!detailId} onClose={() => setDetailId(null)} maxWidth="md" fullWidth>
        <DialogTitle>Audit Log Detail</DialogTitle>
        <DialogContent>
          {detail.data && (
            <Box sx={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 2 }}>
              <TextField label="User" value={detail.data.userName ?? "-"} slotProps={{ input: { readOnly: true } }} size="small" />
              <TextField label="Action" value={detail.data.action} slotProps={{ input: { readOnly: true } }} size="small" />
              <TextField label="Entity Type" value={detail.data.entityType ?? "-"} slotProps={{ input: { readOnly: true } }} size="small" />
              <TextField label="Entity ID" value={detail.data.entityId ?? "-"} slotProps={{ input: { readOnly: true } }} size="small" />
              <TextField label="Method" value={detail.data.method} slotProps={{ input: { readOnly: true } }} size="small" />
              <TextField label="Path" value={detail.data.path} slotProps={{ input: { readOnly: true } }} size="small" />
              <TextField label="Status Code" value={detail.data.statusCode} slotProps={{ input: { readOnly: true } }} size="small" />
              <TextField label="IP Address" value={detail.data.ipAddress ?? "-"} slotProps={{ input: { readOnly: true } }} size="small" />
              <TextField label="Correlation ID" value={detail.data.correlationId ?? "-"} slotProps={{ input: { readOnly: true } }} size="small" fullWidth sx={{ gridColumn: "1 / -1" }} />
              {detail.data.beforeJson && (
                <TextField label="Before" value={detail.data.beforeJson} slotProps={{ input: { readOnly: true } }} size="small" multiline rows={6} fullWidth sx={{ gridColumn: "1 / -1" }} />
              )}
              {detail.data.afterJson && (
                <TextField label="After" value={detail.data.afterJson} slotProps={{ input: { readOnly: true } }} size="small" multiline rows={6} fullWidth sx={{ gridColumn: "1 / -1" }} />
              )}
            </Box>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDetailId(null)}>Close</Button>
        </DialogActions>
      </Dialog>
    </PageContainer>
  );
}
