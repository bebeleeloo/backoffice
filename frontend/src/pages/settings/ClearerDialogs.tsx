import { useEffect, useState } from "react";
import { Button, Dialog, DialogActions, DialogContent, DialogTitle, Switch, FormControlLabel, TextField } from "@mui/material";
import { useCreateClearer, useUpdateClearer } from "../../api/hooks";
import type { ClearerDto } from "../../api/types";

interface CreateProps { open: boolean; onClose: () => void }

export function CreateClearerDialog({ open, onClose }: CreateProps) {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const create = useCreateClearer();

  const handleSubmit = async () => {
    await create.mutateAsync({ name, description: description || undefined });
    setName(""); setDescription("");
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>Create Clearer</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <TextField label="Name" value={name} onChange={(e) => setName(e.target.value)} size="small" required />
        <TextField label="Description" value={description} onChange={(e) => setDescription(e.target.value)} size="small" multiline rows={2} />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit} disabled={!name || create.isPending}>Create</Button>
      </DialogActions>
    </Dialog>
  );
}

interface EditProps { open: boolean; onClose: () => void; item: ClearerDto | null }

export function EditClearerDialog({ open, onClose, item }: EditProps) {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [isActive, setIsActive] = useState(true);
  const update = useUpdateClearer();

  useEffect(() => {
    if (item) { setName(item.name); setDescription(item.description ?? ""); setIsActive(item.isActive); }
  }, [item]);

  const handleSubmit = async () => {
    if (!item) return;
    await update.mutateAsync({ id: item.id, name, description: description || undefined, isActive });
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>Edit Clearer</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <TextField label="Name" value={name} onChange={(e) => setName(e.target.value)} size="small" required />
        <TextField label="Description" value={description} onChange={(e) => setDescription(e.target.value)} size="small" multiline rows={2} />
        <FormControlLabel control={<Switch checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />} label="Active" />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit} disabled={!name || update.isPending}>Save</Button>
      </DialogActions>
    </Dialog>
  );
}
