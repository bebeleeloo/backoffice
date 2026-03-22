import { useState, useCallback, useMemo, createElement } from "react";
import {
  Box, Button, Card, CardContent, Typography, IconButton,
  List, ListItem, ListItemIcon, ListItemText, Collapse,
  Dialog, DialogTitle, DialogContent, DialogActions,
  TextField, CircularProgress, Chip,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import DeleteIcon from "@mui/icons-material/Delete";
import ExpandLess from "@mui/icons-material/ExpandLess";
import ExpandMore from "@mui/icons-material/ExpandMore";
import SaveIcon from "@mui/icons-material/Save";
import { iconMap, FallbackIcon, ConfirmDialog, PageContainer, useConfirm } from "@broker/ui-kit";
import { useMenuRaw, useSaveMenu } from "../api/hooks";
import type { MenuItemConfig } from "../api/types";

interface MenuItemDialogProps {
  open: boolean;
  item: MenuItemConfig | null;
  onClose: () => void;
  onSave: (item: MenuItemConfig) => void;
}

function MenuItemDialog({ open, item, onClose, onSave }: MenuItemDialogProps) {
  const [id, setId] = useState(item?.id ?? "");
  const [label, setLabel] = useState(item?.label ?? "");
  const [icon, setIcon] = useState(item?.icon ?? "");
  const [path, setPath] = useState(item?.path ?? "");
  const [permissions, setPermissions] = useState(item?.permissions?.join(", ") ?? "");

  const handleSave = () => {
    const perms = permissions.split(",").map((p) => p.trim()).filter(Boolean);
    onSave({
      id: id || label.toLowerCase().replace(/\s+/g, "-"),
      label,
      icon,
      path: path || undefined,
      permissions: perms.length > 0 ? perms : undefined,
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
        <TextField label="Icon" value={icon} onChange={(e) => setIcon(e.target.value)} size="small" helperText={`Available: ${Object.keys(iconMap).join(", ")}`} InputLabelProps={{ shrink: true }} />
        <TextField label="Path" value={path} onChange={(e) => setPath(e.target.value)} size="small" placeholder="/example" InputLabelProps={{ shrink: true }} />
        <TextField label="Permissions" value={permissions} onChange={(e) => setPermissions(e.target.value)} size="small" helperText="Comma-separated permission codes" InputLabelProps={{ shrink: true }} />
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
  const saveMenu = useSaveMenu();
  const [items, setItems] = useState<MenuItemConfig[] | null>(null);
  const [expanded, setExpanded] = useState<Record<string, boolean>>({});
  const [editDialog, setEditDialog] = useState<{ open: boolean; item: MenuItemConfig | null; parentId?: string }>({ open: false, item: null });
  const { confirm, confirmDialogProps } = useConfirm();

  const currentItems = useMemo(() => items ?? menuItems ?? [], [items, menuItems]);

  const handleToggle = (id: string) => {
    setExpanded((prev) => ({ ...prev, [id]: !prev[id] }));
  };

  const handleSave = useCallback(async () => {
    try {
      await saveMenu.mutateAsync({ menu: currentItems });
      setItems(null);
    } catch { /* error toast via MutationCache */ }
  }, [currentItems, saveMenu]);

  const handleAddItem = (parentId?: string) => {
    setEditDialog({ open: true, item: null, parentId });
  };

  const handleEditItem = (item: MenuItemConfig, parentId?: string) => {
    setEditDialog({ open: true, item, parentId });
  };

  const handleDeleteItem = async (itemId: string, parentId?: string) => {
    const confirmed = await confirm({ title: "Delete Menu Item", message: "Are you sure you want to delete this menu item?" });
    if (!confirmed) return;

    setItems((prev) => {
      const list = [...(prev ?? menuItems ?? [])];
      if (parentId) {
        const parent = list.find((i) => i.id === parentId);
        if (parent?.children) {
          parent.children = parent.children.filter((c) => c.id !== itemId);
        }
      } else {
        return list.filter((i) => i.id !== itemId);
      }
      return [...list];
    });
  };

  const handleDialogSave = (saved: MenuItemConfig) => {
    setItems((prev) => {
      const list = [...(prev ?? menuItems ?? [])];
      const { parentId } = editDialog;

      if (editDialog.item) {
        // Edit existing
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
        // Add new
        if (parentId) {
          const parent = list.find((i) => i.id === parentId);
          if (parent) {
            parent.children = [...(parent.children ?? []), saved];
          }
        } else {
          list.push(saved);
        }
      }
      return [...list];
    });
  };

  const hasChanges = items !== null;

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
                <IconButton size="small" onClick={() => handleAddItem(item.id)} title="Add child">
                  <AddIcon fontSize="small" />
                </IconButton>
              )}
              <IconButton size="small" onClick={() => handleEditItem(item, parentId)}>
                <EditIcon fontSize="small" />
              </IconButton>
              <IconButton size="small" onClick={() => handleDeleteItem(item.id, parentId)} color="error">
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
    <PageContainer title="Menu Editor">
      <Box sx={{ display: "flex", justifyContent: "flex-end", gap: 1, mb: 1.5 }}>
        <Button variant="outlined" startIcon={<AddIcon />} onClick={() => handleAddItem()}>
          Add Item
        </Button>
        <Button
          variant="contained"
          startIcon={saveMenu.isPending ? <CircularProgress size={18} color="inherit" /> : <SaveIcon />}
          onClick={handleSave}
          disabled={!hasChanges || saveMenu.isPending}
        >
          Save
        </Button>
      </Box>
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
          onClose={() => setEditDialog({ open: false, item: null })}
          onSave={handleDialogSave}
        />
      )}
      <ConfirmDialog {...confirmDialogProps} />
    </PageContainer>
  );
}
