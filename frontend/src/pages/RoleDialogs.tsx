import { useState } from "react";
import {
  Dialog, DialogTitle, DialogContent, DialogActions, Button, TextField,
  Checkbox, FormControlLabel, Typography, Box, Divider,
} from "@mui/material";
import { useCreateRole, useUpdateRole, usePermissions, useSetRolePermissions } from "../api/hooks";
import type { RoleDto, PermissionDto } from "../api/types";

function groupPermissions(allPerms: PermissionDto[]) {
  return allPerms.reduce<Record<string, PermissionDto[]>>((acc, p) => {
    (acc[p.group] ??= []).push(p);
    return acc;
  }, {});
}

interface CreateProps { open: boolean; onClose: () => void }

export function CreateRoleDialog({ open, onClose }: CreateProps) {
  const [form, setForm] = useState({ name: "", description: "" });
  const create = useCreateRole();

  const handleSubmit = async () => {
    await create.mutateAsync({ ...form, description: form.description || undefined });
    setForm({ name: "", description: "" });
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Create Role</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <TextField label="Name" value={form.name} onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))} required />
        <TextField label="Description" value={form.description} onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))} multiline rows={2} />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button onClick={handleSubmit} variant="contained" disabled={create.isPending || !form.name}>Create</Button>
      </DialogActions>
    </Dialog>
  );
}

interface EditProps { open: boolean; onClose: () => void; role: RoleDto | null }

export function EditRoleDialog({ open, onClose, role }: EditProps) {
  const [form, setForm] = useState({ name: "", description: "" });
  const [selectedPermIds, setSelectedPermIds] = useState<Set<string>>(new Set());
  const update = useUpdateRole();
  const permissions = usePermissions();
  const setPerms = useSetRolePermissions();

  const [prevRole, setPrevRole] = useState(role);
  if (role && role !== prevRole) {
    setPrevRole(role);
    setForm({ name: role.name, description: role.description ?? "" });
  }

  const [prevPermData, setPrevPermData] = useState(permissions.data);
  if (role && permissions.data && (role !== prevRole || permissions.data !== prevPermData)) {
    setPrevPermData(permissions.data);
    const rolePermIds = permissions.data
      .filter((p) => role.permissions.includes(p.code))
      .map((p) => p.id);
    setSelectedPermIds(new Set(rolePermIds));
  }

  const togglePerm = (id: string) => {
    setSelectedPermIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const handleSubmit = async () => {
    if (!role) return;
    await update.mutateAsync({
      id: role.id, ...form,
      description: form.description || undefined,
      rowVersion: role.rowVersion,
    });
    await setPerms.mutateAsync({ roleId: role.id, permissionIds: [...selectedPermIds] });
    onClose();
  };

  const grouped = groupPermissions(permissions.data ?? []);

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Edit Role: {role?.name}</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <TextField label="Name" value={form.name} onChange={(e) => setForm((f) => ({ ...f, name: e.target.value }))} required disabled={role?.isSystem} />
        <TextField label="Description" value={form.description} onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))} multiline rows={2} />

        <Box>
          <Typography variant="subtitle2" sx={{ mt: 1, mb: 1 }}>Permissions</Typography>
          {Object.entries(grouped).map(([group, perms]) => (
            <Box key={group} sx={{ mb: 2 }}>
              <Typography variant="subtitle2" color="text.secondary" sx={{ textTransform: "capitalize" }}>{group}</Typography>
              <Divider sx={{ mb: 1 }} />
              {perms.map((p) => (
                <FormControlLabel
                  key={p.id}
                  control={
                    <Checkbox
                      checked={selectedPermIds.has(p.id)}
                      onChange={() => togglePerm(p.id)}
                      disabled={role?.isSystem}
                      size="small"
                    />
                  }
                  label={<Typography variant="body2">{p.name}</Typography>}
                />
              ))}
            </Box>
          ))}
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button onClick={handleSubmit} variant="contained" disabled={update.isPending || setPerms.isPending || !form.name}>Save</Button>
      </DialogActions>
    </Dialog>
  );
}
