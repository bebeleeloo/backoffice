import { useState, useCallback, useMemo, type ReactNode } from "react";
import { Button, IconButton, Chip, TextField, InputAdornment, Paper } from "@mui/material";
import { type GridColDef, type GridPaginationModel, type GridSortModel } from "@mui/x-data-grid";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import HistoryIcon from "@mui/icons-material/History";
import SecurityIcon from "@mui/icons-material/Security";
import SearchIcon from "@mui/icons-material/Search";
import { useRoles, useDeleteRole } from "../api/hooks";
import type { RoleDto } from "../api/types";
import { useHasPermission } from "../auth/usePermission";
import { CreateRoleDialog, EditRoleDialog, RolePermissionsDialog } from "./RoleDialogs";
import { EntityHistoryDialog } from "../components/EntityHistoryDialog";
import { useSearchParams } from "react-router-dom";
import { PageContainer } from "../components/PageContainer";
import { FilteredDataGrid, InlineTextFilter, InlineBooleanFilter } from "../components/grid";

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
  const params = readParams(searchParams);

  const [createOpen, setCreateOpen] = useState(false);
  const [editRole, setEditRole] = useState<RoleDto | null>(null);
  const [permsRole, setPermsRole] = useState<RoleDto | null>(null);
  const [historyRoleId, setHistoryRoleId] = useState<string | null>(null);
  const [search, setSearch] = useState(params.q ?? "");

  const canCreate = useHasPermission("roles.create");
  const canUpdate = useHasPermission("roles.update");
  const canDelete = useHasPermission("roles.delete");
  const canAudit = useHasPermission("audit.read");

  const { data, isLoading } = useRoles(params);
  const deleteRole = useDeleteRole();

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

  const handleDelete = async (id: string) => {
    if (!confirm("Delete this role?")) return;
    await deleteRole.mutateAsync(id);
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
      field: "actions", headerName: "", width: 170, sortable: false, filterable: false,
      renderCell: ({ row }) => (
        <>
          {canAudit && (
            <IconButton size="small" onClick={() => setHistoryRoleId(row.id)} title="History">
              <HistoryIcon fontSize="small" />
            </IconButton>
          )}
          {canUpdate && (
            <IconButton size="small" onClick={() => setPermsRole(row)} title="Permissions" data-testid={`action-perms-${row.id}`}>
              <SecurityIcon fontSize="small" />
            </IconButton>
          )}
          {canUpdate && (
            <IconButton size="small" onClick={() => setEditRole(row)} data-testid={`action-edit-${row.id}`}>
              <EditIcon fontSize="small" />
            </IconButton>
          )}
          {canDelete && !row.isSystem && (
            <IconButton size="small" onClick={() => handleDelete(row.id)} color="error" data-testid={`action-delete-${row.id}`}>
              <DeleteIcon fontSize="small" />
            </IconButton>
          )}
        </>
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

  return (
    <PageContainer
      variant="list"
      title="Roles"
      actions={
        canCreate ? (
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setCreateOpen(true)}>
            Create Role
          </Button>
        ) : undefined
      }
      subheaderLeft={
        <TextField
          fullWidth
          placeholder="Search roles..."
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

      <CreateRoleDialog open={createOpen} onClose={() => setCreateOpen(false)} />
      <EditRoleDialog open={!!editRole} onClose={() => setEditRole(null)} role={editRole} />
      <RolePermissionsDialog open={!!permsRole} onClose={() => setPermsRole(null)} role={permsRole} />
      <EntityHistoryDialog entityType="Role" entityId={historyRoleId ?? ""} open={!!historyRoleId} onClose={() => setHistoryRoleId(null)} />
    </PageContainer>
  );
}
