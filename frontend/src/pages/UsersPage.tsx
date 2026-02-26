import { useState, useCallback, useMemo, type ReactNode } from "react";
import { Button, IconButton, Chip, Paper } from "@mui/material";
import { type GridColDef, type GridPaginationModel, type GridSortModel } from "@mui/x-data-grid";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import HistoryIcon from "@mui/icons-material/History";
import { useUsers, useDeleteUser } from "../api/hooks";
import type { UserDto } from "../api/types";
import { useHasPermission } from "../auth/usePermission";
import { CreateUserDialog, EditUserDialog } from "./UserDialogs";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { useConfirm } from "../hooks/useConfirm";
import { EntityHistoryDialog } from "../components/EntityHistoryDialog";
import { useSearchParams } from "react-router-dom";
import { ExportButton } from "../components/ExportButton";
import type { ExcelColumn } from "../utils/exportToExcel";
import { apiClient } from "../api/client";
import type { PagedResult } from "../api/types";
import { PageContainer } from "../components/PageContainer";
import { FilteredDataGrid, InlineTextFilter, InlineBooleanFilter } from "../components/grid";
import { GlobalSearchBar } from "../components/GlobalSearchBar";

function readParams(sp: URLSearchParams) {
  return {
    page: Number(sp.get("page") || "1"),
    pageSize: Number(sp.get("pageSize") || "25"),
    sort: sp.get("sort") || undefined,
    q: sp.get("q") || undefined,
    username: sp.get("username") || undefined,
    email: sp.get("email") || undefined,
    fullName: sp.get("fullName") || undefined,
    role: sp.get("role") || undefined,
    isActive:
      sp.get("isActive") === "true"
        ? true
        : sp.get("isActive") === "false"
          ? false
          : undefined,
  };
}

export function UsersPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const params = readParams(searchParams);

  const [createOpen, setCreateOpen] = useState(false);
  const [editUser, setEditUser] = useState<UserDto | null>(null);
  const [historyUserId, setHistoryUserId] = useState<string | null>(null);

  const canCreate = useHasPermission("users.create");
  const canUpdate = useHasPermission("users.update");
  const canDelete = useHasPermission("users.delete");
  const canAudit = useHasPermission("audit.read");

  const { data, isLoading } = useUsers(params);
  const deleteUser = useDeleteUser();
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

  const handlePagination = (model: GridPaginationModel) => {
    setParam({ page: String(model.page + 1), pageSize: String(model.pageSize) });
  };

  const handleSort = (model: GridSortModel) => {
    const s = model[0];
    setParam({ sort: s ? `${s.field} ${s.sort}` : undefined, page: "1" });
  };

  const handleDelete = async (id: string) => {
    const ok = await confirm({ title: "Delete User", message: "Are you sure you want to delete this user?" });
    if (!ok) return;
    try { await deleteUser.mutateAsync(id); } catch { /* handled by MutationCache */ }
  };

  const columns: GridColDef<UserDto>[] = [
    { field: "username", headerName: "Username", flex: 1, minWidth: 120 },
    { field: "email", headerName: "Email", flex: 1, minWidth: 180 },
    { field: "fullName", headerName: "Full Name", flex: 1, minWidth: 150 },
    {
      field: "isActive", headerName: "Status", width: 100,
      renderCell: ({ value }) => (
        <Chip label={value ? "Active" : "Inactive"} color={value ? "success" : "default"} size="small" />
      ),
    },
    {
      field: "roles", headerName: "Roles", flex: 1, minWidth: 150, sortable: false,
      renderCell: ({ value }) => (value as string[]).map((r) => <Chip key={r} label={r} size="small" sx={{ mr: 0.5 }} />),
    },
    {
      field: "actions", headerName: "", width: 130, sortable: false, filterable: false,
      renderCell: ({ row }) => (
        <div onClick={(e) => e.stopPropagation()}>
          {canAudit && (
            <IconButton size="small" onClick={() => setHistoryUserId(row.id)}>
              <HistoryIcon fontSize="small" />
            </IconButton>
          )}
          {canUpdate && (
            <IconButton size="small" onClick={() => setEditUser(row)} data-testid={`action-edit-${row.id}`}>
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
    m.set("username", () => (
      <InlineTextFilter
        value={params.username ?? ""}
        onChange={(v) => setFilterParam("username", v || undefined)}
        placeholder="Username..."
      />
    ));
    m.set("email", () => (
      <InlineTextFilter
        value={params.email ?? ""}
        onChange={(v) => setFilterParam("email", v || undefined)}
        placeholder="Email..."
      />
    ));
    m.set("fullName", () => (
      <InlineTextFilter
        value={params.fullName ?? ""}
        onChange={(v) => setFilterParam("fullName", v || undefined)}
        placeholder="Full Name..."
      />
    ));
    m.set("isActive", () => (
      <InlineBooleanFilter
        value={params.isActive}
        onChange={(v) =>
          setFilterParam("isActive", v === undefined ? undefined : String(v))
        }
      />
    ));
    m.set("roles", () => (
      <InlineTextFilter
        value={params.role ?? ""}
        onChange={(v) => setFilterParam("role", v || undefined)}
        placeholder="Role..."
      />
    ));
    return m;
  }, [params.username, params.email, params.fullName, params.isActive, params.role, setFilterParam]);

  const exportColumns: ExcelColumn<UserDto>[] = useMemo(() => [
    { header: "Username", value: (r) => r.username },
    { header: "Email", value: (r) => r.email },
    { header: "Full Name", value: (r) => r.fullName },
    { header: "Status", value: (r) => r.isActive ? "Active" : "Inactive" },
    { header: "Roles", value: (r) => r.roles.join(", ") },
    { header: "Created", value: (r) => r.createdAt ? new Date(r.createdAt).toLocaleString() : "" },
  ], []);

  const fetchAllUsers = useCallback(async () => {
    const { page: _, pageSize: __, ...filters } = params;
    const resp = await apiClient.get<PagedResult<UserDto>>("/users", {
      params: { ...filters, page: 1, pageSize: 10000 },
    });
    return resp.data.items;
  }, [params]);

  return (
    <PageContainer
      variant="list"
      title="Users"
      actions={
        <>
          <ExportButton fetchData={fetchAllUsers} columns={exportColumns} filename="users" />
          {canCreate && (
            <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
              Create User
            </Button>
          )}
        </>
      }
      subheaderLeft={
        <GlobalSearchBar
          value={params.q ?? ""}
          onChange={(v) => setFilterParam("q", v || undefined)}
          placeholder="Search users..."
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
          onRowClick={(p) => setEditUser(p.row as UserDto)}
          sx={{ height: "100%", border: "none", "& .MuiDataGrid-row": { cursor: "pointer" } }}
        />
      </Paper>

      <CreateUserDialog open={createOpen} onClose={() => setCreateOpen(false)} />
      <EditUserDialog open={!!editUser} onClose={() => setEditUser(null)} user={editUser} />
      <EntityHistoryDialog entityType="User" entityId={historyUserId ?? ""} open={!!historyUserId} onClose={() => setHistoryUserId(null)} />
      <ConfirmDialog {...confirmDialogProps} isLoading={deleteUser.isPending} />
    </PageContainer>
  );
}
