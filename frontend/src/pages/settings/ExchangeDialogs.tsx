import { useEffect, useState } from "react";
import { Autocomplete, Button, Dialog, DialogActions, DialogContent, DialogTitle, Switch, FormControlLabel, TextField } from "@mui/material";
import { useCreateExchange, useUpdateExchange, useCountries } from "../../api/hooks";
import type { ExchangeDto, CountryDto } from "../../api/types";

interface CreateProps { open: boolean; onClose: () => void }

export function CreateExchangeDialog({ open, onClose }: CreateProps) {
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [country, setCountry] = useState<CountryDto | null>(null);
  const create = useCreateExchange();
  const { data: countries } = useCountries();

  const handleSubmit = async () => {
    await create.mutateAsync({ code, name, countryId: country?.id });
    setCode(""); setName(""); setCountry(null);
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>Create Exchange</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <TextField label="Code" value={code} onChange={(e) => setCode(e.target.value)} size="small" required />
        <TextField label="Name" value={name} onChange={(e) => setName(e.target.value)} size="small" required />
        <Autocomplete
          options={countries ?? []}
          getOptionLabel={(o) => `${o.flagEmoji} ${o.name}`}
          value={country}
          onChange={(_, v) => setCountry(v)}
          renderInput={(params) => <TextField {...params} label="Country" size="small" />}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit} disabled={!code || !name || create.isPending}>Create</Button>
      </DialogActions>
    </Dialog>
  );
}

interface EditProps { open: boolean; onClose: () => void; item: ExchangeDto | null }

export function EditExchangeDialog({ open, onClose, item }: EditProps) {
  const [code, setCode] = useState("");
  const [name, setName] = useState("");
  const [country, setCountry] = useState<CountryDto | null>(null);
  const [isActive, setIsActive] = useState(true);
  const update = useUpdateExchange();
  const { data: countries } = useCountries();

  useEffect(() => {
    if (item) {
      setCode(item.code); setName(item.name); setIsActive(item.isActive);
      setCountry(countries?.find((c) => c.id === item.countryId) ?? null);
    }
  }, [item, countries]);

  const handleSubmit = async () => {
    if (!item) return;
    await update.mutateAsync({ id: item.id, code, name, countryId: country?.id, isActive });
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>Edit Exchange</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <TextField label="Code" value={code} onChange={(e) => setCode(e.target.value)} size="small" required />
        <TextField label="Name" value={name} onChange={(e) => setName(e.target.value)} size="small" required />
        <Autocomplete
          options={countries ?? []}
          getOptionLabel={(o) => `${o.flagEmoji} ${o.name}`}
          value={country}
          onChange={(_, v) => setCountry(v)}
          renderInput={(params) => <TextField {...params} label="Country" size="small" />}
        />
        <FormControlLabel control={<Switch checked={isActive} onChange={(e) => setIsActive(e.target.checked)} />} label="Active" />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit} disabled={!code || !name || update.isPending}>Save</Button>
      </DialogActions>
    </Dialog>
  );
}
