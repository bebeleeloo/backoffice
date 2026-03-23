import { useState, useMemo, useCallback } from "react";
import { useSearchParams } from "react-router-dom";
import {
  Button, Typography, IconButton, Tooltip, Paper,
  Checkbox, Autocomplete, TextField,
  Dialog, DialogTitle, DialogContent, DialogActions,
  Table, TableHead, TableBody, TableRow, TableCell,
} from "@mui/material";
import type { GridColDef, GridSortModel, GridSortDirection } from "@mui/x-data-grid";
import DeleteIcon from "@mui/icons-material/Delete";
import EditIcon from "@mui/icons-material/Edit";
import VisibilityIcon from "@mui/icons-material/Visibility";
import HistoryIcon from "@mui/icons-material/History";
import FilterListOffIcon from "@mui/icons-material/FilterListOff";

import { PageContainer, FilteredDataGrid, CompactMultiSelect, NumericRangePopover, GlobalSearchBar, ExportButton, EntityHistoryDialog, useHasPermission } from "@broker/ui-kit";
import type { ExcelColumn } from "@broker/ui-kit";
import { useEntitiesRaw, useSaveEntities, useEntityMetadata } from "../api/hooks";
import type { EntityConfig, EntityFieldConfig } from "../api/types";

const KNOWN_ROLES = ["Manager", "Operator", "Viewer"];

function deepCloneEntities(entities: EntityConfig[]): EntityConfig[] {
  return entities.map((e) => ({
    ...e,
    fields: e.fields.map((f) => ({ ...f, roles: [...f.roles] })),
  }));
}

function getAllOrUndefined(sp: URLSearchParams, key: string) {
  const vals = sp.getAll(key);
  return vals.length > 0 ? vals : undefined;
}

function readParams(sp: URLSearchParams) {
  return {
    sort: sp.get("sort") || undefined,
    q: sp.get("q") || undefined,
    name: getAllOrUndefined(sp, "name"),
    totalFieldsMin: sp.get("totalFieldsMin") || undefined,
    totalFieldsMax: sp.get("totalFieldsMax") || undefined,
    usedFieldsMin: sp.get("usedFieldsMin") || undefined,
    usedFieldsMax: sp.get("usedFieldsMax") || undefined,
  };
}

/* ───── View-only dialog ───── */

interface EntityFieldsViewDialogProps {
  entityName: string | null;
  open: boolean;
  onClose: () => void;
}

function EntityFieldsViewDialog({ entityName, open, onClose }: EntityFieldsViewDialogProps) {
  const { data: rawEntities } = useEntitiesRaw();

  const entity = useMemo(
    () => rawEntities?.find((e) => e.name === entityName) ?? null,
    [rawEntities, entityName],
  );

  const isRoleChecked = (field: EntityFieldConfig, role: string): boolean => {
    if (field.roles.includes("*")) return true;
    return field.roles.includes(role);
  };

  if (!entity) return null;

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>{entity.name} — Fields</DialogTitle>
      <DialogContent sx={{ p: 0 }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Field</TableCell>
              <TableCell align="center">All (*)</TableCell>
              {KNOWN_ROLES.map((role) => (
                <TableCell key={role} align="center">{role}</TableCell>
              ))}
            </TableRow>
          </TableHead>
          <TableBody>
            {entity.fields.map((field) => (
              <TableRow key={field.name}>
                <TableCell>
                  <Typography variant="body2" fontFamily="monospace">{field.name}</Typography>
                </TableCell>
                <TableCell align="center">
                  <Checkbox size="small" checked={field.roles.includes("*")} disabled />
                </TableCell>
                {KNOWN_ROLES.map((role) => (
                  <TableCell key={role} align="center">
                    <Checkbox size="small" checked={isRoleChecked(field, role)} disabled />
                  </TableCell>
                ))}
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Close</Button>
      </DialogActions>
    </Dialog>
  );
}

/* ───── Edit dialog ───── */

interface EntityFieldsDialogProps {
  entityName: string | null;
  open: boolean;
  onClose: () => void;
}

function EntityFieldsDialog({ entityName, open, onClose }: EntityFieldsDialogProps) {
  const { data: rawEntities } = useEntitiesRaw();
  const { data: metadata } = useEntityMetadata();
  const saveEntities = useSaveEntities();

  const entity = useMemo(
    () => rawEntities?.find((e) => e.name === entityName) ?? null,
    [rawEntities, entityName],
  );

  const isRoleChecked = (field: EntityFieldConfig, role: string): boolean => {
    if (field.roles.includes("*")) return true;
    return field.roles.includes(role);
  };

  const getAvailableFields = (): string[] => {
    if (!entity || !metadata) return [];
    const meta = metadata.find((m) => m.name === entityName);
    if (!meta) return [];
    const existing = new Set(entity.fields.map((f) => f.name));
    return meta.fields.filter((f) => !existing.has(f));
  };

  const saveWithUpdatedEntities = async (updater: (entities: EntityConfig[]) => EntityConfig[]) => {
    if (!rawEntities) return;
    const updated = updater(deepCloneEntities(rawEntities));
    try {
      await saveEntities.mutateAsync({ entities: updated });
    } catch { /* error toast via MutationCache */ }
  };

  const handleToggleRole = (fieldIdx: number, role: string) => {
    saveWithUpdatedEntities((list) => {
      const ent = list.find((e) => e.name === entityName);
      if (!ent) return list;
      const field = ent.fields[fieldIdx];
      if (role === "*") {
        field.roles = field.roles.includes("*") ? [] : ["*"];
      } else {
        if (field.roles.includes("*")) {
          field.roles = KNOWN_ROLES.filter((r) => r !== role);
        } else if (field.roles.includes(role)) {
          field.roles = field.roles.filter((r) => r !== role);
        } else {
          field.roles = [...field.roles, role];
          if (KNOWN_ROLES.every((r) => field.roles.includes(r))) {
            field.roles = ["*"];
          }
        }
      }
      return list;
    });
  };

  const handleAddField = (fieldName: string) => {
    if (!fieldName) return;
    saveWithUpdatedEntities((list) => {
      const ent = list.find((e) => e.name === entityName);
      if (!ent) return list;
      ent.fields.push({ name: fieldName, roles: ["*"] });
      return list;
    });
  };

  const handleRemoveField = (fieldIdx: number) => {
    saveWithUpdatedEntities((list) => {
      const ent = list.find((e) => e.name === entityName);
      if (!ent) return list;
      ent.fields.splice(fieldIdx, 1);
      return list;
    });
  };

  if (!entity) return null;

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>{entity.name} — Fields</DialogTitle>
      <DialogContent sx={{ p: 0 }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Field</TableCell>
              <TableCell align="center">All (*)</TableCell>
              {KNOWN_ROLES.map((role) => (
                <TableCell key={role} align="center">{role}</TableCell>
              ))}
              <TableCell align="center" width={48} />
            </TableRow>
          </TableHead>
          <TableBody>
            {entity.fields.map((field, fieldIdx) => (
              <TableRow key={field.name}>
                <TableCell>
                  <Typography variant="body2" fontFamily="monospace">{field.name}</Typography>
                </TableCell>
                <TableCell align="center">
                  <Checkbox
                    size="small"
                    checked={field.roles.includes("*")}
                    onChange={() => handleToggleRole(fieldIdx, "*")}
                    disabled={saveEntities.isPending}
                  />
                </TableCell>
                {KNOWN_ROLES.map((role) => (
                  <TableCell key={role} align="center">
                    <Checkbox
                      size="small"
                      checked={isRoleChecked(field, role)}
                      onChange={() => handleToggleRole(fieldIdx, role)}
                      disabled={field.roles.includes("*") || saveEntities.isPending}
                    />
                  </TableCell>
                ))}
                <TableCell align="center">
                  <IconButton size="small" color="error" onClick={() => handleRemoveField(fieldIdx)} disabled={saveEntities.isPending}>
                    <DeleteIcon fontSize="small" />
                  </IconButton>
                </TableCell>
              </TableRow>
            ))}
            <TableRow>
              <TableCell colSpan={KNOWN_ROLES.length + 3}>
                <Autocomplete
                  size="small"
                  options={getAvailableFields()}
                  value={null}
                  onChange={(_e, value) => { if (value) handleAddField(value); }}
                  renderInput={(params) => (
                    <TextField {...params} placeholder="Add field..." />
                  )}
                  blurOnSelect
                  clearOnBlur
                  disabled={saveEntities.isPending}
                  sx={{ maxWidth: 400 }}
                />
              </TableCell>
            </TableRow>
          </TableBody>
        </Table>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Close</Button>
      </DialogActions>
    </Dialog>
  );
}

/* ───── Main page ───── */

interface EntityRow {
  id: string;
  name: string;
  usedFields: number;
  totalFields: number;
}

export function EntityFieldsPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const params = readParams(searchParams);

  const { data: rawEntities, isLoading } = useEntitiesRaw();
  const { data: metadata } = useEntityMetadata();
  const [historyOpen, setHistoryOpen] = useState(false);
  const [historyEntity, setHistoryEntity] = useState<string | null>(null);
  const [viewEntity, setViewEntity] = useState<string | null>(null);
  const [editEntity, setEditEntity] = useState<string | null>(null);
  const canAudit = useHasPermission("audit.read");

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
    params.q || params.name?.length ||
    params.totalFieldsMin || params.totalFieldsMax ||
    params.usedFieldsMin || params.usedFieldsMax
  );

  const sortModel: GridSortModel = useMemo(() => {
    if (!params.sort) return [];
    const [field, dir] = params.sort.split(" ");
    return [{ field, sort: dir as GridSortDirection }];
  }, [params.sort]);

  const handleSort = (model: GridSortModel) => {
    const s = model[0];
    setParam({ sort: s ? `${s.field} ${s.sort}` : undefined });
  };

  const metadataMap = useMemo(() => {
    const m = new Map<string, number>();
    metadata?.forEach((e) => m.set(e.name, e.fields.length));
    return m;
  }, [metadata]);

  const allRows: EntityRow[] = useMemo(
    () => (rawEntities ?? []).map((e) => ({
      id: e.name,
      name: e.name,
      usedFields: e.fields.length,
      totalFields: metadataMap.get(e.name) ?? e.fields.length,
    })),
    [rawEntities, metadataMap],
  );

  const entityNameOptions = useMemo(
    () => allRows.map((r) => ({ value: r.name, label: r.name })),
    [allRows],
  );

  const rows = useMemo(() => {
    let filtered = allRows;
    if (params.q) {
      const q = params.q.toLowerCase();
      filtered = filtered.filter((r) => r.name.toLowerCase().includes(q));
    }
    if (params.name?.length) {
      filtered = filtered.filter((r) => params.name!.includes(r.name));
    }
    if (params.totalFieldsMin) {
      const min = Number(params.totalFieldsMin);
      filtered = filtered.filter((r) => r.totalFields >= min);
    }
    if (params.totalFieldsMax) {
      const max = Number(params.totalFieldsMax);
      filtered = filtered.filter((r) => r.totalFields <= max);
    }
    if (params.usedFieldsMin) {
      const min = Number(params.usedFieldsMin);
      filtered = filtered.filter((r) => r.usedFields >= min);
    }
    if (params.usedFieldsMax) {
      const max = Number(params.usedFieldsMax);
      filtered = filtered.filter((r) => r.usedFields <= max);
    }
    return filtered;
  }, [allRows, params.q, params.name, params.totalFieldsMin, params.totalFieldsMax, params.usedFieldsMin, params.usedFieldsMax]);

  const columns: GridColDef<EntityRow>[] = useMemo(() => [
    {
      field: "name",
      headerName: "Entity",
      flex: 1,
      minWidth: 200,
    },
    {
      field: "totalFields",
      headerName: "Total Fields",
      width: 140,
    },
    {
      field: "usedFields",
      headerName: "Used Fields",
      width: 140,
    },
    {
      field: "actions",
      headerName: "",
      width: 120,
      sortable: false,
      filterable: false,
      disableColumnMenu: true,
      renderCell: ({ row }) => (
        <div onClick={(e) => e.stopPropagation()}>
          <IconButton size="small" onClick={() => setViewEntity(row.name)}>
            <VisibilityIcon fontSize="small" />
          </IconButton>
          <IconButton size="small" onClick={() => setEditEntity(row.name)}>
            <EditIcon fontSize="small" />
          </IconButton>
          {canAudit && (
            <IconButton size="small" onClick={() => setHistoryEntity(row.name)}>
              <HistoryIcon fontSize="small" />
            </IconButton>
          )}
        </div>
      ),
    },
  ], [canAudit]);

  const filterDefs = useMemo(() => {
    const m = new Map<string, () => React.ReactNode>();
    m.set("name", () => (
      <CompactMultiSelect
        options={entityNameOptions}
        value={params.name ?? []}
        onChange={(v) => setMultiFilterParam("name", v)}
      />
    ));
    m.set("totalFields", () => (
      <NumericRangePopover
        minValue={params.totalFieldsMin ?? ""}
        maxValue={params.totalFieldsMax ?? ""}
        onMinChange={(v) => setFilterParam("totalFieldsMin", v || undefined)}
        onMaxChange={(v) => setFilterParam("totalFieldsMax", v || undefined)}
      />
    ));
    m.set("usedFields", () => (
      <NumericRangePopover
        minValue={params.usedFieldsMin ?? ""}
        maxValue={params.usedFieldsMax ?? ""}
        onMinChange={(v) => setFilterParam("usedFieldsMin", v || undefined)}
        onMaxChange={(v) => setFilterParam("usedFieldsMax", v || undefined)}
      />
    ));
    return m;
  }, [entityNameOptions, params, setFilterParam, setMultiFilterParam]);

  const exportColumns: ExcelColumn<EntityRow>[] = useMemo(() => [
    { header: "Entity", value: (r) => r.name },
    { header: "Total Fields", value: (r) => r.totalFields },
    { header: "Used Fields", value: (r) => r.usedFields },
  ], []);

  const fetchAll = useCallback(async () => rows, [rows]);

  return (
    <PageContainer
      title="Entity Fields"
      variant="list"
      actions={
        <>
          {hasActiveFilters && (
            <Tooltip title="Clear all filters">
              <IconButton size="small" onClick={clearAllFilters}>
                <FilterListOffIcon />
              </IconButton>
            </Tooltip>
          )}
          <ExportButton fetchData={fetchAll} columns={exportColumns} filename="entity-fields" />
          {canAudit && (
            <Button variant="outlined" startIcon={<HistoryIcon />} onClick={() => setHistoryOpen(true)}>
              History
            </Button>
          )}
        </>
      }
      subheaderLeft={
        <GlobalSearchBar
          value={params.q ?? ""}
          onChange={(v) => setFilterParam("q", v || undefined)}
          placeholder="Search entities..."
        />
      }
    >
      <Paper variant="outlined" sx={{ flex: 1, minHeight: 0, overflow: "hidden" }}>
        <FilteredDataGrid
          rows={rows}
          columns={columns}
          loading={isLoading}
          filterDefs={filterDefs}
          paginationMode="client"
          sortingMode="client"
          sortModel={sortModel}
          onSortModelChange={handleSort}
          hideFooter
          onRowClick={(p) => setViewEntity(p.row.name)}
          sx={{ height: "100%", border: "none", "& .MuiDataGrid-row": { cursor: "pointer" } }}
        />
      </Paper>
      <EntityFieldsViewDialog
        entityName={viewEntity}
        open={viewEntity !== null}
        onClose={() => setViewEntity(null)}
      />
      <EntityFieldsDialog
        entityName={editEntity}
        open={editEntity !== null}
        onClose={() => setEditEntity(null)}
      />
      <EntityHistoryDialog entityType="EntitiesConfig" entityId="config" open={historyOpen} onClose={() => setHistoryOpen(false)} />
      <EntityHistoryDialog entityType="EntitiesConfig" entityId="config" open={historyEntity !== null} onClose={() => setHistoryEntity(null)} filterRelatedEntityId={historyEntity ?? undefined} />
    </PageContainer>
  );
}
