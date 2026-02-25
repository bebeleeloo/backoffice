import { useState } from "react";
import {
  Dialog, DialogTitle, DialogContent, DialogActions, Button, TextField,
  FormControlLabel, Switch, Autocomplete, Chip,
} from "@mui/material";
import { useCreateUser, useUpdateUser, useRoles } from "../api/hooks";
import { validateEmail, validateRequired, type FieldErrors } from "../utils/validateFields";
import type { UserDto } from "../api/types";

interface CreateProps { open: boolean; onClose: () => void }

export function CreateUserDialog({ open, onClose }: CreateProps) {
  const [form, setForm] = useState({ username: "", email: "", password: "", fullName: "", isActive: true, roleIds: [] as string[] });
  const [errors, setErrors] = useState<FieldErrors>({});
  const create = useCreateUser();
  const roles = useRoles({ page: 1, pageSize: 100 });

  const updateField = (field: string, value: string) => {
    setForm((f) => ({ ...f, [field]: value }));
    setErrors((prev) => ({ ...prev, [field]: undefined }));
  };

  const handleSubmit = async () => {
    const errs: FieldErrors = {
      username: validateRequired(form.username),
      email: validateEmail(form.email),
      password: validateRequired(form.password),
    };
    if (Object.values(errs).some(Boolean)) { setErrors(errs); return; }
    try {
      await create.mutateAsync({ ...form, fullName: form.fullName || undefined });
      setForm({ username: "", email: "", password: "", fullName: "", isActive: true, roleIds: [] });
      setErrors({});
      onClose();
    } catch { /* handled by MutationCache */ }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Create User</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <TextField label="Username" value={form.username} onChange={(e) => updateField("username", e.target.value)} required error={!!errors.username} helperText={errors.username} />
        <TextField label="Email" type="email" value={form.email} onChange={(e) => updateField("email", e.target.value)} required error={!!errors.email} helperText={errors.email} />
        <TextField label="Password" type="password" value={form.password} onChange={(e) => updateField("password", e.target.value)} required error={!!errors.password} helperText={errors.password} />
        <TextField label="Full Name" value={form.fullName} onChange={(e) => setForm((f) => ({ ...f, fullName: e.target.value }))} />
        <FormControlLabel control={<Switch checked={form.isActive} onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))} />} label="Active" />
        <Autocomplete
          multiple
          disableCloseOnSelect={false}
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
  const [errors, setErrors] = useState<FieldErrors>({});
  const update = useUpdateUser();
  const roles = useRoles({ page: 1, pageSize: 100 });

  const [prevUser, setPrevUser] = useState(user);
  const [prevRolesData, setPrevRolesData] = useState(roles.data);
  if (user && (user !== prevUser || roles.data !== prevRolesData)) {
    setPrevUser(user);
    setPrevRolesData(roles.data);
    setForm({
      email: user.email, fullName: user.fullName ?? "",
      isActive: user.isActive,
      roleIds: (roles.data?.items ?? []).filter((r) => user.roles.includes(r.name)).map((r) => r.id),
    });
    setErrors({});
  }

  const updateField = (field: string, value: string) => {
    setForm((f) => ({ ...f, [field]: value }));
    setErrors((prev) => ({ ...prev, [field]: undefined }));
  };

  const handleSubmit = async () => {
    if (!user) return;
    const errs: FieldErrors = { email: validateEmail(form.email) };
    if (Object.values(errs).some(Boolean)) { setErrors(errs); return; }
    try {
      await update.mutateAsync({
        id: user.id, ...form,
        fullName: form.fullName || undefined,
        rowVersion: user.rowVersion,
      });
      onClose();
    } catch { /* handled by MutationCache */ }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Edit User: {user?.username}</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <TextField label="Email" type="email" value={form.email} onChange={(e) => updateField("email", e.target.value)} required error={!!errors.email} helperText={errors.email} />
        <TextField label="Full Name" value={form.fullName} onChange={(e) => setForm((f) => ({ ...f, fullName: e.target.value }))} />
        <FormControlLabel control={<Switch checked={form.isActive} onChange={(e) => setForm((f) => ({ ...f, isActive: e.target.checked }))} />} label="Active" />
        <Autocomplete
          multiple
          disableCloseOnSelect={false}
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
