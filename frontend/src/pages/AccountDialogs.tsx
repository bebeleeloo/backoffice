import { useState } from "react";
import {
  Dialog, DialogTitle, DialogContent, DialogActions, Button, TextField,
  MenuItem, Box, Typography, IconButton, Table, TableBody, TableCell,
  TableHead, TableRow, Checkbox, FormControlLabel, Autocomplete,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import {
  useCreateAccount, useUpdateAccount, useAccount, useClearers, useTradePlatforms,
  useSetAccountHolders, useClients,
} from "../api/hooks";
import type {
  AccountStatus, AccountType, MarginType, OptionLevel, Tariff,
  DeliveryType, ClearerDto, TradePlatformDto, CreateAccountRequest,
  HolderRole, AccountHolderInput, ClientListItemDto,
} from "../api/types";

const STATUSES: AccountStatus[] = ["Active", "Blocked", "Closed", "Suspended"];
const ACCOUNT_TYPES: AccountType[] = ["Individual", "Corporate", "Joint", "Trust", "IRA"];
const MARGIN_TYPES: { value: MarginType; label: string }[] = [
  { value: "Cash", label: "Cash" },
  { value: "MarginX1", label: "Margin X1" },
  { value: "MarginX2", label: "Margin X2" },
  { value: "MarginX4", label: "Margin X4" },
  { value: "DayTrader", label: "Day Trader" },
];
const OPTION_LEVELS: OptionLevel[] = ["Level0", "Level1", "Level2", "Level3", "Level4"];
const TARIFFS: Tariff[] = ["Basic", "Standard", "Premium", "VIP"];
const DELIVERY_TYPES: { value: DeliveryType; label: string }[] = [
  { value: "Paper", label: "Paper" },
  { value: "Electronic", label: "Electronic" },
];
const HOLDER_ROLES: HolderRole[] = ["Owner", "Beneficiary", "Trustee", "PowerOfAttorney", "Custodian", "Authorized"];

function emptyForm(): CreateAccountRequest {
  return {
    number: "",
    status: "Active",
    accountType: "Individual",
    marginType: "Cash",
    optionLevel: "Level0",
    tariff: "Basic",
  };
}

/* ── Shared form fields ── */

function AccountFormFields({ form, set, clearers, platforms }: {
  form: CreateAccountRequest;
  set: <K extends keyof CreateAccountRequest>(key: K, value: CreateAccountRequest[K]) => void;
  clearers: ClearerDto[];
  platforms: TradePlatformDto[];
}) {
  return (
    <>
      <Box sx={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 2 }}>
        <TextField label="Number" value={form.number} onChange={(e) => set("number", e.target.value)} size="small" required />
        <TextField select label="Status" value={form.status} onChange={(e) => set("status", e.target.value as AccountStatus)} size="small">
          {STATUSES.map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
        </TextField>
        <TextField select label="Account Type" value={form.accountType} onChange={(e) => set("accountType", e.target.value as AccountType)} size="small">
          {ACCOUNT_TYPES.map((t) => <MenuItem key={t} value={t}>{t}</MenuItem>)}
        </TextField>
        <TextField select label="Margin Type" value={form.marginType} onChange={(e) => set("marginType", e.target.value as MarginType)} size="small">
          {MARGIN_TYPES.map((m) => <MenuItem key={m.value} value={m.value}>{m.label}</MenuItem>)}
        </TextField>
        <TextField select label="Option Level" value={form.optionLevel} onChange={(e) => set("optionLevel", e.target.value as OptionLevel)} size="small">
          {OPTION_LEVELS.map((o) => <MenuItem key={o} value={o}>{o}</MenuItem>)}
        </TextField>
        <TextField select label="Tariff" value={form.tariff} onChange={(e) => set("tariff", e.target.value as Tariff)} size="small">
          {TARIFFS.map((t) => <MenuItem key={t} value={t}>{t}</MenuItem>)}
        </TextField>
        <TextField select label="Delivery Type" value={form.deliveryType ?? ""} onChange={(e) => set("deliveryType", (e.target.value || undefined) as DeliveryType | undefined)} size="small">
          <MenuItem value="">—</MenuItem>
          {DELIVERY_TYPES.map((d) => <MenuItem key={d.value} value={d.value}>{d.label}</MenuItem>)}
        </TextField>
        <TextField select label="Clearer" value={form.clearerId ?? ""} onChange={(e) => set("clearerId", e.target.value || undefined)} size="small">
          <MenuItem value="">—</MenuItem>
          {clearers.map((c) => <MenuItem key={c.id} value={c.id}>{c.name}</MenuItem>)}
        </TextField>
        <TextField select label="Trade Platform" value={form.tradePlatformId ?? ""} onChange={(e) => set("tradePlatformId", e.target.value || undefined)} size="small">
          <MenuItem value="">—</MenuItem>
          {platforms.map((p) => <MenuItem key={p.id} value={p.id}>{p.name}</MenuItem>)}
        </TextField>
        <TextField
          label="Opened At" type="date" value={form.openedAt ?? ""}
          onChange={(e) => set("openedAt", e.target.value || undefined)}
          size="small" slotProps={{ inputLabel: { shrink: true } }}
        />
        <TextField
          label="Closed At" type="date" value={form.closedAt ?? ""}
          onChange={(e) => set("closedAt", e.target.value || undefined)}
          size="small" slotProps={{ inputLabel: { shrink: true } }}
        />
        <TextField label="External ID" value={form.externalId ?? ""} onChange={(e) => set("externalId", e.target.value || undefined)} size="small" />
      </Box>
      <TextField
        label="Comment" value={form.comment ?? ""}
        onChange={(e) => set("comment", e.target.value || undefined)}
        size="small" multiline rows={2}
      />
    </>
  );
}

/* ── Create Dialog ── */

interface CreateProps { open: boolean; onClose: () => void }

export function CreateAccountDialog({ open, onClose }: CreateProps) {
  const [form, setForm] = useState<CreateAccountRequest>(emptyForm);
  const create = useCreateAccount();
  const clearers = useClearers();
  const platforms = useTradePlatforms();

  const set = <K extends keyof CreateAccountRequest>(key: K, value: CreateAccountRequest[K]) =>
    setForm((f) => ({ ...f, [key]: value }));

  const handleSubmit = async () => {
    try {
      await create.mutateAsync({
        ...form,
        comment: form.comment || undefined,
        externalId: form.externalId || undefined,
        openedAt: form.openedAt || undefined,
        closedAt: form.closedAt || undefined,
        clearerId: form.clearerId || undefined,
        tradePlatformId: form.tradePlatformId || undefined,
      });
      setForm(emptyForm);
      onClose();
    } catch { /* handled by MutationCache */ }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Create Account</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <AccountFormFields form={form} set={set} clearers={clearers.data ?? []} platforms={platforms.data ?? []} />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit} disabled={create.isPending || !form.number}>Create</Button>
      </DialogActions>
    </Dialog>
  );
}

/* ── Edit Dialog ── */

interface EditProps { open: boolean; onClose: () => void; account: { id: string } | null }

export function EditAccountDialog({ open, onClose, account }: EditProps) {
  const [form, setForm] = useState<CreateAccountRequest>(emptyForm);
  const [rowVersion, setRowVersion] = useState("");
  const [holders, setHolders] = useState<AccountHolderInput[]>([]);
  const update = useUpdateAccount();
  const setAccountHolders = useSetAccountHolders();
  const clearers = useClearers();
  const platforms = useTradePlatforms();
  const { data: fullAccount } = useAccount(account?.id ?? "");
  const { data: clientsData } = useClients({ page: 1, pageSize: 200 });
  const clients = clientsData?.items ?? [];

  const [prevFullAccount, setPrevFullAccount] = useState(fullAccount);
  if (fullAccount && fullAccount !== prevFullAccount) {
    setPrevFullAccount(fullAccount);
    setForm({
      number: fullAccount.number,
      status: fullAccount.status,
      accountType: fullAccount.accountType,
      marginType: fullAccount.marginType,
      optionLevel: fullAccount.optionLevel,
      tariff: fullAccount.tariff,
      deliveryType: fullAccount.deliveryType ?? undefined,
      openedAt: fullAccount.openedAt ? fullAccount.openedAt.split("T")[0] : undefined,
      closedAt: fullAccount.closedAt ? fullAccount.closedAt.split("T")[0] : undefined,
      externalId: fullAccount.externalId ?? undefined,
      clearerId: fullAccount.clearerId ?? undefined,
      tradePlatformId: fullAccount.tradePlatformId ?? undefined,
      comment: fullAccount.comment ?? undefined,
    });
    setRowVersion(fullAccount.rowVersion);
    setHolders(
      fullAccount.holders.map((h) => ({ clientId: h.clientId, role: h.role, isPrimary: h.isPrimary }))
    );
  }

  const set = <K extends keyof CreateAccountRequest>(key: K, value: CreateAccountRequest[K]) =>
    setForm((f) => ({ ...f, [key]: value }));

  const addHolderRow = () => {
    setHolders((prev) => [...prev, { clientId: "", role: "Owner", isPrimary: false }]);
  };
  const removeHolderRow = (index: number) => {
    setHolders((prev) => prev.filter((_, i) => i !== index));
  };
  const updateHolderRow = (index: number, patch: Partial<AccountHolderInput>) => {
    setHolders((prev) => prev.map((h, i) => (i === index ? { ...h, ...patch } : h)));
  };

  const handleSubmit = async () => {
    if (!account) return;
    try {
      await update.mutateAsync({
        id: account.id,
        ...form,
        comment: form.comment || undefined,
        externalId: form.externalId || undefined,
        openedAt: form.openedAt || undefined,
        closedAt: form.closedAt || undefined,
        clearerId: form.clearerId || undefined,
        tradePlatformId: form.tradePlatformId || undefined,
        rowVersion,
      });
      // Save holders
      const validHolders = holders.filter((h) => h.clientId);
      await setAccountHolders.mutateAsync({ accountId: account.id, holders: validHolders });
      onClose();
    } catch { /* handled by MutationCache */ }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>Edit Account: {fullAccount?.number}</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <AccountFormFields form={form} set={set} clearers={clearers.data ?? []} platforms={platforms.data ?? []} />

        {/* Holders */}
        <Box>
          <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center", mb: 1 }}>
            <Typography variant="subtitle2">Holders</Typography>
            <Button size="small" startIcon={<AddIcon />} onClick={addHolderRow}>Add</Button>
          </Box>
          {holders.length === 0 ? (
            <Typography variant="body2" color="text.secondary">No holders.</Typography>
          ) : (
            <Table size="small">
              <TableHead>
                <TableRow>
                  <TableCell sx={{ width: "40%" }}>Client</TableCell>
                  <TableCell>Role</TableCell>
                  <TableCell>Primary</TableCell>
                  <TableCell sx={{ width: 50 }} />
                </TableRow>
              </TableHead>
              <TableBody>
                {holders.map((h, i) => (
                  <TableRow key={i}>
                    <TableCell>
                      <ClientAutocomplete
                        clients={clients}
                        value={h.clientId}
                        onChange={(id) => updateHolderRow(i, { clientId: id })}
                      />
                    </TableCell>
                    <TableCell>
                      <TextField
                        select size="small" fullWidth value={h.role}
                        onChange={(e) => updateHolderRow(i, { role: e.target.value as HolderRole })}
                      >
                        {HOLDER_ROLES.map((r) => <MenuItem key={r} value={r}>{r}</MenuItem>)}
                      </TextField>
                    </TableCell>
                    <TableCell>
                      <FormControlLabel
                        control={<Checkbox checked={h.isPrimary} onChange={(e) => updateHolderRow(i, { isPrimary: e.target.checked })} size="small" />}
                        label=""
                      />
                    </TableCell>
                    <TableCell>
                      <IconButton size="small" onClick={() => removeHolderRow(i)} color="error">
                        <DeleteIcon fontSize="small" />
                      </IconButton>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit} disabled={update.isPending || setAccountHolders.isPending || !form.number}>Save</Button>
      </DialogActions>
    </Dialog>
  );
}

/* ── Client Autocomplete (strict, for holder selection) ── */

function ClientAutocomplete({ clients, value, onChange }: {
  clients: ClientListItemDto[];
  value: string;
  onChange: (id: string) => void;
}) {
  const selected = clients.find((c) => c.id === value) ?? null;
  return (
    <Autocomplete
      size="small"
      options={clients}
      getOptionLabel={(o) => o.displayName}
      value={selected}
      onChange={(_, v) => onChange(v?.id ?? "")}
      renderInput={(params) => <TextField {...params} placeholder="Select client..." />}
      isOptionEqualToValue={(o, v) => o.id === v.id}
      autoHighlight
      openOnFocus
    />
  );
}
