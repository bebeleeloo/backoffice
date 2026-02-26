import { useState, useCallback, useMemo, type ReactNode } from "react";
import { Button, IconButton, Chip, Paper } from "@mui/material";
import { type GridColDef, type GridPaginationModel, type GridSortModel } from "@mui/x-data-grid";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import VisibilityIcon from "@mui/icons-material/Visibility";
import { useRoles, useDeleteRole } from "../api/hooks";
import type { RoleDto } from "../api/types";
import { useHasPermission } from "../auth/usePermission";
import { CreateRoleDialog } from "./RoleDialogs";
import { ConfirmDialog } from "../components/ConfirmDialog";
import { useConfirm } from "../hooks/useConfirm";
import { useSearchParams, useNavigate } from "react-router-dom";
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
    name: sp.get("name") || undefined,
    description: sp.get("description") || undefined,
    isSystem:
      sp.get("isSystem") === "true"
        ? true
        : sp.get("isSystem") === "false"
          ? false
          : undefined,
    permission: sp.get("permission") || undefined,
  };
}

export function RolesPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const navigate = useNavigate();
  const params = readParams(searchParams);

  const [createOpen, setCreateOpen] = useState(false);

  const canCreate = useHasPermission("roles.create");
  const canUpdate = useHasPermission("roles.update");
  const canDelete = useHasPermission("roles.delete");

  const { data, isLoading } = useRoles(params);
  const deleteRole = useDeleteRole();
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
    const ok = await confirm({ title: "Delete Role", message: "Are you sure you want to delete this role?" });
    if (!ok) return;
    try { await deleteRole.mutateAsync(id); } catch { /* handled by MutationCache */ }
  };

  const columns: GridColDef<RoleDto>[] = [
    { field: "name", headerName: "Name", flex: 1, minWidth: 150 },
    { field: "description", headerName: "Description", flex: 2, minWidth: 200 },
    {
      field: "isSystem", headerName: "Type", width: 100,
      renderCell: ({ value }) => <Chip label={value ? "System" : "Custom"} color={value ? "warning" : "default"} size="small" />,
    },
    {
      field: "permissions", headerName: "Permissions", width: 120, sortable: false,
      renderCell: ({ value }) => <Chip label={`${(value as string[]).length} perms`} size="small" variant="outlined" />,
    },
    {
      field: "actions", headerName: "", width: 120, sortable: false, filterable: false, disableColumnMenu: true,
      renderCell: ({ row }) => (
        <div onClick={(e) => e.stopPropagation()}>
          <IconButton size="small" onClick={() => navigate(`/roles/${row.id}`)}>
            <VisibilityIcon fontSize="small" />
          </IconButton>
          {canUpdate && (
            <IconButton size="small" onClick={() => navigate(`/roles/${row.id}`)} data-testid={`action-edit-${row.id}`}>
              <EditIcon fontSize="small" />
            </IconButton>
          )}
          {canDelete && !row.isSystem && (
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
    m.set("name", () => (
      <InlineTextFilter
        value={params.name ?? ""}
        onChange={(v) => setFilterParam("name", v || undefined)}
        placeholder="Name..."
      />
    ));
    m.set("description", () => (
      <InlineTextFilter
        value={params.description ?? ""}
        onChange={(v) => setFilterParam("description", v || undefined)}
        placeholder="Description..."
      />
    ));
    m.set("isSystem", () => (
      <InlineBooleanFilter
        value={params.isSystem}
        onChange={(v) =>
          setFilterParam("isSystem", v === undefined ? undefined : String(v))
        }
      />
    ));
    m.set("permissions", () => (
      <InlineTextFilter
        value={params.permission ?? ""}
        onChange={(v) => setFilterParam("permission", v || undefined)}
        placeholder="Permission..."
      />
    ));
    return m;
  }, [params.name, params.description, params.isSystem, params.permission, setFilterParam]);

  const exportColumns: ExcelColumn<RoleDto>[] = useMemo(() => [
    { header: "Name", value: (r) => r.name },
    { header: "Description", value: (r) => r.description },
    { header: "Type", value: (r) => r.isSystem ? "System" : "Custom" },
    { header: "Permissions", value: (r) => r.permissions.join(", ") },
  ], []);

  const fetchAllRoles = useCallback(async () => {
    const { page: _, pageSize: __, ...filters } = params;
    const resp = await apiClient.get<PagedResult<RoleDto>>("/roles", {
      params: { ...filters, page: 1, pageSize: 10000 },
    });
    return resp.data.items;
  }, [params]);

  return (
    <PageContainer
      variant="list"
      title="Roles"
      actions={
        <>
          <ExportButton fetchData={fetchAllRoles} columns={exportColumns} filename="roles" />
          {canCreate && (
            <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
              Create Role
            </Button>
          )}
        </>
      }
      subheaderLeft={
        <GlobalSearchBar
          placeholder="Search roles..."
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
          filterDefs={filterDefs}
          onRowClick={(p) => navigate(`/roles/${p.row.id}`)}
          sx={{ height: "100%", border: "none", "& .MuiDataGrid-row": { cursor: "pointer" } }}
        />
      </Paper>

      <CreateRoleDialog open={createOpen} onClose={() => setCreateOpen(false)} />
      <ConfirmDialog {...confirmDialogProps} isLoading={deleteRole.isPending} />
    </PageContainer>
  );
}
