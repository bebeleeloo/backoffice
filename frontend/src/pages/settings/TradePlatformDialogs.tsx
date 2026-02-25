import { useState } from "react";
import { Button, Dialog, DialogActions, DialogContent, DialogTitle, Switch, FormControlLabel, TextField } from "@mui/material";
import { useCreateTradePlatform, useUpdateTradePlatform } from "../../api/hooks";
import type { TradePlatformDto } from "../../api/types";

interface CreateProps { open: boolean; onClose: () => void }

export function CreateTradePlatformDialog({ open, onClose }: CreateProps) {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const create = useCreateTradePlatform();

  const handleSubmit = async () => {
    try {
      await create.mutateAsync({ name, description: description || undefined });
      setName(""); setDescription("");
      onClose();
    } catch { /* handled by MutationCache */ }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>Create Trade Platform</DialogTitle>
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

interface EditProps { open: boolean; onClose: () => void; item: TradePlatformDto | null }

export function EditTradePlatformDialog({ open, onClose, item }: EditProps) {
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [isActive, setIsActive] = useState(true);
  const update = useUpdateTradePlatform();

  const [prevItem, setPrevItem] = useState(item);
  if (item && item !== prevItem) {
    setPrevItem(item);
    setName(item.name); setDescription(item.description ?? ""); setIsActive(item.isActive);
  }

  const handleSubmit = async () => {
    if (!item) return;
    try {
      await update.mutateAsync({ id: item.id, name, description: description || undefined, isActive });
      onClose();
    } catch { /* handled by MutationCache */ }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>Edit Trade Platform</DialogTitle>
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
