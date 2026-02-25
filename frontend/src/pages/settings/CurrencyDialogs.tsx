import { useState } from "react";
import { Button, Dialog, DialogActions, DialogContent, DialogTitle, Switch, FormControlLabel, TextField } from "@mui/material";
import { useCreateCurrency, useUpdateCurrency } from "../../api/hooks";
import { validateRequired, type FieldErrors } from "../../utils/validateFields";
import type { CurrencyDto } from "../../api/types";

interface CreateProps { open: boolean; onClose: () => void }

export function CreateCurrencyDialog({ open, onClose }: CreateProps) {
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [symbol, setSymbol] = useState("");
  const [errors, setErrors] = useState<FieldErrors>({});
  const create = useCreateCurrency();

  const handleSubmit = async () => {
    const errs: FieldErrors = { code: validateRequired(code), name: validateRequired(name) };
    if (Object.values(errs).some(Boolean)) { setErrors(errs); return; }
    try {
      await create.mutateAsync({ code, name, symbol: symbol || undefined });
      setCode(""); setName(""); setSymbol(""); setErrors({});
      onClose();
    } catch { /* handled by MutationCache */ }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>Create Currency</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <TextField label="Code" value={code} onChange={(e) => { setCode(e.target.value); setErrors((prev) => ({ ...prev, code: undefined })); }} size="small" required error={!!errors.code} helperText={errors.code} />
        <TextField label="Name" value={name} onChange={(e) => { setName(e.target.value); setErrors((prev) => ({ ...prev, name: undefined })); }} size="small" required error={!!errors.name} helperText={errors.name} />
        <TextField label="Symbol" value={symbol} onChange={(e) => setSymbol(e.target.value)} size="small" />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit} disabled={create.isPending}>Create</Button>
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
  const [errors, setErrors] = useState<FieldErrors>({});
  const update = useUpdateCurrency();

  const [prevItem, setPrevItem] = useState(item);
  if (item && item !== prevItem) {
    setPrevItem(item);
    setCode(item.code); setName(item.name); setSymbol(item.symbol ?? ""); setIsActive(item.isActive);
    setErrors({});
  }

  const handleSubmit = async () => {
    if (!item) return;
    const errs: FieldErrors = { code: validateRequired(code), name: validateRequired(name) };
    if (Object.values(errs).some(Boolean)) { setErrors(errs); return; }
    try {
      await update.mutateAsync({ id: item.id, code, name, symbol: symbol || undefined, isActive });
      onClose();
    } catch { /* handled by MutationCache */ }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>Edit Currency</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <TextField label="Code" value={code} onChange={(e) => { setCode(e.target.value); setErrors((prev) => ({ ...prev, code: undefined })); }} size="small" required error={!!errors.code} helperText={errors.code} />
        <TextField label="Name" value={name} onChange={(e) => { setName(e.target.value); setErrors((prev) => ({ ...prev, name: undefined })); }} size="small" required error={!!errors.name} helperText={errors.name} />
        <TextField label="Symbol" value={symbol} onChange={(e) => setSymbol(e.target.value)} size="small" />
        <FormControlLabel control={<Switch checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />} label="Active" />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit} disabled={update.isPending}>Save</Button>
      </DialogActions>
    </Dialog>
  );
}
