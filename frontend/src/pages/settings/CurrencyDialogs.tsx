import { useState } from "react";
import { Button, Dialog, DialogActions, DialogContent, DialogTitle, Switch, FormControlLabel, TextField } from "@mui/material";
import { useCreateCurrency, useUpdateCurrency } from "../../api/hooks";
import type { CurrencyDto } from "../../api/types";

interface CreateProps { open: boolean; onClose: () => void }

export function CreateCurrencyDialog({ open, onClose }: CreateProps) {
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [symbol, setSymbol] = useState("");
  const create = useCreateCurrency();

  const handleSubmit = async () => {
    try {
      await create.mutateAsync({ code, name, symbol: symbol || undefined });
      setCode(""); setName(""); setSymbol("");
      onClose();
    } catch { /* handled by MutationCache */ }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>Create Currency</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <TextField label="Code" value={code} onChange={(e) => setCode(e.target.value)} size="small" required />
        <TextField label="Name" value={name} onChange={(e) => setName(e.target.value)} size="small" required />
        <TextField label="Symbol" value={symbol} onChange={(e) => setSymbol(e.target.value)} size="small" />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit} disabled={!code || !name || create.isPending}>Create</Button>
      </DialogActions>
    </Dialog>
  );
}

interface EditProps { open: boolean; onClose: () => void; item: CurrencyDto | null }

export function EditCurrencyDialog({ open, onClose, item }: EditProps) {
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [symbol, setSymbol] = useState("");
  const [isActive, setIsActive] = useState(true);
  const update = useUpdateCurrency();

  const [prevItem, setPrevItem] = useState(item);
  if (item && item !== prevItem) {
    setPrevItem(item);
    setCode(item.code); setName(item.name); setSymbol(item.symbol ?? ""); setIsActive(item.isActive);
  }

  const handleSubmit = async () => {
    if (!item) return;
    try {
      await update.mutateAsync({ id: item.id, code, name, symbol: symbol || undefined, isActive });
      onClose();
    } catch { /* handled by MutationCache */ }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>Edit Currency</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <TextField label="Code" value={code} onChange={(e) => setCode(e.target.value)} size="small" required />
        <TextField label="Name" value={name} onChange={(e) => setName(e.target.value)} size="small" required />
        <TextField label="Symbol" value={symbol} onChange={(e) => setSymbol(e.target.value)} size="small" />
        <FormControlLabel control={<Switch checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />} label="Active" />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit} disabled={!code || !name || update.isPending}>Save</Button>
      </DialogActions>
    </Dialog>
  );
}
