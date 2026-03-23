import { useState, useMemo, createElement } from "react";
import {
  Autocomplete, Box, Button, Card, CardContent, Typography, IconButton,
  List, ListItem, ListItemIcon, ListItemText, Collapse,
  Dialog, DialogTitle, DialogContent, DialogActions,
  TextField, CircularProgress, Chip, Tooltip,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import ExpandLess from "@mui/icons-material/ExpandLess";
import ExpandMore from "@mui/icons-material/ExpandMore";
import HistoryIcon from "@mui/icons-material/History";
import { iconMap, FallbackIcon, ConfirmDialog, PageContainer, useConfirm, EntityHistoryDialog, useHasPermission } from "@broker/ui-kit";

const ICON_NAMES = Object.keys(iconMap);
import { useMenuRaw, useSaveMenu, usePermissions } from "../api/hooks";
import type { MenuItemConfig } from "../api/types";

interface MenuItemDialogProps {
  open: boolean;
  item: MenuItemConfig | null;
  permissionCodes: string[];
  onClose: () => void;
  onSave: (item: MenuItemConfig) => void;
}

function MenuItemDialog({ open, item, permissionCodes, onClose, onSave }: MenuItemDialogProps) {
  const [id, setId] = useState(item?.id ?? "");
  const [label, setLabel] = useState(item?.label ?? "");
  const [icon, setIcon] = useState(item?.icon ?? "");
  const [path, setPath] = useState(item?.path ?? "");
  const [permissions, setPermissions] = useState<string[]>(item?.permissions ?? []);
  const [iconFilter, setIconFilter] = useState("");

  const handleSave = () => {
    onSave({
      id: id || label.toLowerCase().replace(/\s+/g, "-"),
      label,
      icon,
      path: path || undefined,
      permissions: permissions.length > 0 ? permissions : undefined,
      children: item?.children,
    });
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>{item ? "Edit Menu Item" : "Add Menu Item"}</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "16px !important" }}>
        <TextField label="ID" value={id} onChange={(e) => setId(e.target.value)} size="small" InputLabelProps={{ shrink: true }} />
        <TextField label="Label" value={label} onChange={(e) => setLabel(e.target.value)} size="small" required InputLabelProps={{ shrink: true }} />
        <Box>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 0.5 }}>
            Icon {icon && (
              <Chip
                icon={createElement(iconMap[icon] ?? FallbackIcon, { fontSize: "small" } as never)}
                label={icon}
                size="small"
                color="primary"
                variant="outlined"
                sx={{ ml: 1 }}
              />
            )}
          </Typography>
          <TextField
            size="small"
            placeholder="Filter icons..."
            value={iconFilter}
            onChange={(e) => setIconFilter(e.target.value)}
            fullWidth
            sx={{ mb: 1 }}
          />
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 0.5, maxHeight: 180, overflow: "auto" }}>
            {ICON_NAMES
              .filter((name) => !iconFilter || name.toLowerCase().includes(iconFilter.toLowerCase()))
              .map((name) => (
                <Tooltip key={name} title={name}>
                  <IconButton
                    size="small"
                    onClick={() => setIcon(name)}
                    sx={{
                      border: 1,
                      borderColor: icon === name ? "primary.main" : "divider",
                      bgcolor: icon === name ? "primary.main" : "transparent",
                      color: icon === name ? "primary.contrastText" : "text.secondary",
                      "&:hover": { bgcolor: icon === name ? "primary.dark" : "action.hover" },
                    }}
                  >
                    {createElement(iconMap[name], { fontSize: "small" } as never)}
                  </IconButton>
                </Tooltip>
              ))}
          </Box>
        </Box>
        <TextField label="Path" value={path} onChange={(e) => setPath(e.target.value)} size="small" placeholder="/example" InputLabelProps={{ shrink: true }} />
        <Autocomplete
          multiple
          options={permissionCodes}
          value={permissions}
          onChange={(_, value) => setPermissions(value)}
          groupBy={(option) => option.split(".")[0]}
          renderInput={(params) => <TextField {...params} label="Permissions" size="small" InputLabelProps={{ shrink: true }} />}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSave} disabled={!label}>Save</Button>
      </DialogActions>
    </Dialog>
  );
}

export function MenuEditorPage() {
  const { data: menuItems, isLoading } = useMenuRaw();
  const { data: permissionsData } = usePermissions();
  const saveMenu = useSaveMenu();
  const permissionCodes = useMemo(() => permissionsData?.map((p) => p.code) ?? [], [permissionsData]);
  const [expanded, setExpanded] = useState<Record<string, boolean>>({});
  const [editDialog, setEditDialog] = useState<{ open: boolean; item: MenuItemConfig | null; parentId?: string }>({ open: false, item: null });
  const [historyOpen, setHistoryOpen] = useState(false);
  const [historyItemId, setHistoryItemId] = useState<string | null>(null);
  const canAudit = useHasPermission("audit.read");
  const { confirm, confirmDialogProps } = useConfirm();

  const currentItems = useMemo(() => menuItems ?? [], [menuItems]);

  const handleToggle = (id: string) => {
    setExpanded((prev) => ({ ...prev, [id]: !prev[id] }));
  };

  const handleAddItem = (parentId?: string) => {
    setEditDialog({ open: true, item: null, parentId });
  };

  const handleEditItem = (item: MenuItemConfig, parentId?: string) => {
    setEditDialog({ open: true, item, parentId });
  };

  const handleDeleteItem = async (itemId: string, parentId?: string) => {
    const confirmed = await confirm({ title: "Delete Menu Item", message: "Are you sure you want to delete this menu item?" });
    if (!confirmed) return;

    const list = structuredClone(currentItems);
    let updatedItems: MenuItemConfig[];
    if (parentId) {
      const parent = list.find((i) => i.id === parentId);
      if (parent?.children) {
        parent.children = parent.children.filter((c) => c.id !== itemId);
      }
      updatedItems = list;
    } else {
      updatedItems = list.filter((i) => i.id !== itemId);
    }
    try {
      await saveMenu.mutateAsync({ menu: updatedItems });
    } catch { /* error toast via MutationCache */ }
  };

  const handleDialogSave = async (saved: MenuItemConfig) => {
    const list = structuredClone(currentItems);
    const { parentId } = editDialog;

    if (editDialog.item) {
      if (parentId) {
        const parent = list.find((i) => i.id === parentId);
        if (parent?.children) {
          const idx = parent.children.findIndex((c) => c.id === editDialog.item!.id);
          if (idx >= 0) parent.children[idx] = saved;
        }
      } else {
        const idx = list.findIndex((i) => i.id === editDialog.item!.id);
        if (idx >= 0) list[idx] = { ...saved, children: list[idx].children };
      }
    } else {
      if (parentId) {
        const parent = list.find((i) => i.id === parentId);
        if (parent) {
          parent.children = [...(parent.children ?? []), saved];
        }
      } else {
        list.push(saved);
      }
    }

    try {
      await saveMenu.mutateAsync({ menu: list });
    } catch { /* error toast via MutationCache */ }
  };

  const renderItem = (item: MenuItemConfig, parentId?: string) => {
    const hasChildren = item.children && item.children.length > 0;
    const isExpanded = expanded[item.id] ?? false;
    const IconComponent = iconMap[item.icon] ?? FallbackIcon;

    return (
      <Box key={item.id}>
        <ListItem
          secondaryAction={
            <Box>
              {!parentId && (
                <IconButton size="small" onClick={() => handleAddItem(item.id)} title="Add child" disabled={saveMenu.isPending}>
                  <AddIcon fontSize="small" />
                </IconButton>
              )}
              {canAudit && (
                <IconButton size="small" onClick={() => setHistoryItemId(item.id)} disabled={saveMenu.isPending}>
                  <HistoryIcon fontSize="small" />
                </IconButton>
              )}
              <IconButton size="small" onClick={() => handleEditItem(item, parentId)} disabled={saveMenu.isPending}>
                <EditIcon fontSize="small" />
              </IconButton>
              <IconButton size="small" onClick={() => handleDeleteItem(item.id, parentId)} color="error" disabled={saveMenu.isPending}>
                <DeleteIcon fontSize="small" />
              </IconButton>
            </Box>
          }
          sx={{ pl: parentId ? 6 : 2 }}
        >
          {hasChildren && (
            <IconButton size="small" onClick={() => handleToggle(item.id)} sx={{ mr: 1 }}>
              {isExpanded ? <ExpandLess /> : <ExpandMore />}
            </IconButton>
          )}
          <ListItemIcon sx={{ minWidth: 36 }}>
            {createElement(IconComponent)}
          </ListItemIcon>
          <ListItemText
            primary={
              <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
                <Typography variant="body2" fontWeight={500}>{item.label}</Typography>
                {item.path && <Chip label={item.path} size="small" variant="outlined" />}
                {item.permissions?.map((p) => <Chip key={p} label={p} size="small" color="primary" variant="outlined" />)}
              </Box>
            }
            secondary={`id: ${item.id}`}
          />
        </ListItem>
        {hasChildren && (
          <Collapse in={isExpanded} timeout="auto" unmountOnExit>
            <List disablePadding>
              {item.children!.map((child) => renderItem(child, item.id))}
            </List>
          </Collapse>
        )}
      </Box>
    );
  };

  if (isLoading) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", py: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <PageContainer
      title="Menu Editor"
      actions={
        <>
          {canAudit && (
            <Button variant="outlined" startIcon={<HistoryIcon />} onClick={() => setHistoryOpen(true)}>
              History
            </Button>
          )}
          <Button variant="outlined" startIcon={<AddIcon />} onClick={() => handleAddItem()} disabled={saveMenu.isPending}>
            Add Item
          </Button>
        </>
      }
    >
      <Card>
        <CardContent sx={{ p: 0, "&:last-child": { pb: 0 } }}>
          <List>
            {currentItems.map((item) => renderItem(item))}
          </List>
        </CardContent>
      </Card>
      {editDialog.open && (
        <MenuItemDialog
          open
          item={editDialog.item}
          permissionCodes={permissionCodes}
          onClose={() => setEditDialog({ open: false, item: null })}
          onSave={handleDialogSave}
        />
      )}
      <ConfirmDialog {...confirmDialogProps} />
      <EntityHistoryDialog entityType="MenuConfig" entityId="config" open={historyOpen} onClose={() => setHistoryOpen(false)} />
      <EntityHistoryDialog entityType="MenuConfig" entityId="config" open={historyItemId !== null} onClose={() => setHistoryItemId(null)} filterRelatedEntityId={historyItemId ?? undefined} />
    </PageContainer>
  );
}
