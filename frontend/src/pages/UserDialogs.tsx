import { useEffect, useState } from "react";
import {
  Dialog, DialogTitle, DialogContent, DialogActions, Button, TextField,
  FormControlLabel, Switch, Autocomplete, Chip,
} from "@mui/material";
import { useCreateUser, useUpdateUser, useRoles } from "../api/hooks";
import type { UserDto } from "../api/types";

interface CreateProps { open: boolean; onClose: () => void }

export function CreateUserDialog({ open, onClose }: CreateProps) {
  const [form, setForm] = useState({ username: "", email: "", password: "", fullName: "", isActive: true, roleIds: [] as string[] });
  const create = useCreateUser();
  const roles = useRoles({ page: 1, pageSize: 100 });

  const handleSubmit = async () => {
    await create.mutateAsync({ ...form, fullName: form.fullName || undefined });
    setForm({ username: "", email: "", password: "", fullName: "", isActive: true, roleIds: [] });
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Create User</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <TextField label="Username" value={form.username} onChange={(e) => setForm((f) => ({ ...f, username: e.target.value }))} required />
        <TextField label="Email" type="email" value={form.email} onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))} required />
        <TextField label="Password" type="password" value={form.password} onChange={(e) => setForm((f) => ({ ...f, password: e.target.value }))} required />
        <TextField label="Full Name" value={form.fullName} onChange={(e) => setForm((f) => ({ ...f, fullName: e.target.value }))} />
        <FormControlLabel control={<Switch checked={form.isActive} onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))} />} label="Active" />
        <Autocomplete
          multiple
          options={roles.data?.items ?? []}
          getOptionLabel={(o) => o.name}
          value={(roles.data?.items ?? []).filter((r) => form.roleIds.includes(r.id))}
          onChange={(_, v) => setForm((f) => ({ ...f, roleIds: v.map((r) => r.id) }))}
          renderTags={(v, getProps) => v.map((o, i) => <Chip label={o.name} {...getProps({ index: i })} key={o.id} />)}
          renderInput={(p) => <TextField {...p} label="Roles" />}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button onClick={handleSubmit} variant="contained" disabled={create.isPending}>Create</Button>
      </DialogActions>
    </Dialog>
  );
}

interface EditProps { open: boolean; onClose: () => void; user: UserDto | null }

export function EditUserDialog({ open, onClose, user }: EditProps) {
  const [form, setForm] = useState({ email: "", fullName: "", isActive: true, roleIds: [] as string[] });
  const update = useUpdateUser();
  const roles = useRoles({ page: 1, pageSize: 100 });

  useEffect(() => {
    if (user) {
      setForm({
        email: user.email, fullName: user.fullName ?? "",
        isActive: user.isActive,
        roleIds: (roles.data?.items ?? []).filter((r) => user.roles.includes(r.name)).map((r) => r.id),
      });
    }
  }, [user, roles.data]);

  const handleSubmit = async () => {
    if (!user) return;
    await update.mutateAsync({
      id: user.id, ...form,
      fullName: form.fullName || undefined,
      rowVersion: user.rowVersion,
    });
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Edit User: {user?.username}</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <TextField label="Email" type="email" value={form.email} onChange={(e) => setForm((f) => ({ ...f, email: e.target.value }))} required />
        <TextField label="Full Name" value={form.fullName} onChange={(e) => setForm((f) => ({ ...f, fullName: e.target.value }))} />
        <FormControlLabel control={<Switch checked={form.isActive} onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))} />} label="Active" />
        <Autocomplete
          multiple
          options={roles.data?.items ?? []}
          getOptionLabel={(o) => o.name}
          value={(roles.data?.items ?? []).filter((r) => form.roleIds.includes(r.id))}
          onChange={(_, v) => setForm((f) => ({ ...f, roleIds: v.map((r) => r.id) }))}
          renderTags={(v, getProps) => v.map((o, i) => <Chip label={o.name} {...getProps({ index: i })} key={o.id} />)}
          renderInput={(p) => <TextField {...p} label="Roles" />}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button onClick={handleSubmit} variant="contained" disabled={update.isPending}>Save</Button>
      </DialogActions>
    </Dialog>
  );
}
