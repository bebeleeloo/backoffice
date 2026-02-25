import { useState } from "react";
import {
  Dialog, DialogTitle, DialogContent, DialogActions, Button,
  TextField, MenuItem, Box, FormControlLabel, Switch,
} from "@mui/material";
import {
  useCreateInstrument, useUpdateInstrument, useInstrument,
  useExchanges, useCurrencies, useCountries,
} from "../api/hooks";
import { validateRequired, type FieldErrors } from "../utils/validateFields";
import type {
  InstrumentType, AssetClass, InstrumentStatus, Sector,
  CreateInstrumentRequest, ExchangeDto, CurrencyDto, CountryDto,
} from "../api/types";

const INSTRUMENT_TYPES: { value: InstrumentType; label: string }[] = [
  { value: "Stock", label: "Stock" },
  { value: "Bond", label: "Bond" },
  { value: "ETF", label: "ETF" },
  { value: "Option", label: "Option" },
  { value: "Future", label: "Future" },
  { value: "Forex", label: "Forex" },
  { value: "CFD", label: "CFD" },
  { value: "MutualFund", label: "Mutual Fund" },
  { value: "Warrant", label: "Warrant" },
  { value: "Index", label: "Index" },
];

const ASSET_CLASSES: { value: AssetClass; label: string }[] = [
  { value: "Equities", label: "Equities" },
  { value: "FixedIncome", label: "Fixed Income" },
  { value: "Derivatives", label: "Derivatives" },
  { value: "ForeignExchange", label: "Foreign Exchange" },
  { value: "Commodities", label: "Commodities" },
  { value: "Funds", label: "Funds" },
];

const STATUSES: InstrumentStatus[] = ["Active", "Inactive", "Delisted", "Suspended"];

const SECTORS: { value: Sector; label: string }[] = [
  { value: "Technology", label: "Technology" },
  { value: "Healthcare", label: "Healthcare" },
  { value: "Finance", label: "Finance" },
  { value: "Energy", label: "Energy" },
  { value: "ConsumerDiscretionary", label: "Consumer Discretionary" },
  { value: "ConsumerStaples", label: "Consumer Staples" },
  { value: "Industrials", label: "Industrials" },
  { value: "Materials", label: "Materials" },
  { value: "RealEstate", label: "Real Estate" },
  { value: "Utilities", label: "Utilities" },
  { value: "Communication", label: "Communication" },
  { value: "Other", label: "Other" },
];

function emptyForm(): CreateInstrumentRequest {
  return {
    symbol: "",
    name: "",
    type: "Stock",
    assetClass: "Equities",
    status: "Active",
    lotSize: 1,
    isMarginEligible: true,
  };
}

/* ── Shared form fields ── */

function InstrumentFormFields({ form, set, exchanges, currencies, countries, errors = {} }: {
  form: CreateInstrumentRequest;
  set: <K extends keyof CreateInstrumentRequest>(key: K, value: CreateInstrumentRequest[K]) => void;
  exchanges: ExchangeDto[];
  currencies: CurrencyDto[];
  countries: CountryDto[];
  errors?: FieldErrors;
}) {
  return (
    <>
      <Box sx={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 2 }}>
        <TextField label="Symbol" value={form.symbol} onChange={(e) => set("symbol", e.target.value)} size="small" required error={!!errors.symbol} helperText={errors.symbol} />
        <TextField label="Name" value={form.name} onChange={(e) => set("name", e.target.value)} size="small" required error={!!errors.name} helperText={errors.name} />
        <TextField label="ISIN" value={form.isin ?? ""} onChange={(e) => set("isin", e.target.value || undefined)} size="small" />
        <TextField label="CUSIP" value={form.cusip ?? ""} onChange={(e) => set("cusip", e.target.value || undefined)} size="small" />
        <TextField select label="Type" value={form.type} onChange={(e) => set("type", e.target.value as InstrumentType)} size="small">
          {INSTRUMENT_TYPES.map((t) => <MenuItem key={t.value} value={t.value}>{t.label}</MenuItem>)}
        </TextField>
        <TextField select label="Asset Class" value={form.assetClass} onChange={(e) => set("assetClass", e.target.value as AssetClass)} size="small">
          {ASSET_CLASSES.map((a) => <MenuItem key={a.value} value={a.value}>{a.label}</MenuItem>)}
        </TextField>
        <TextField select label="Status" value={form.status} onChange={(e) => set("status", e.target.value as InstrumentStatus)} size="small">
          {STATUSES.map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
        </TextField>
        <TextField select label="Sector" value={form.sector ?? ""} onChange={(e) => set("sector", (e.target.value || undefined) as Sector | undefined)} size="small">
          <MenuItem value="">—</MenuItem>
          {SECTORS.map((s) => <MenuItem key={s.value} value={s.value}>{s.label}</MenuItem>)}
        </TextField>
        <TextField select label="Exchange" value={form.exchangeId ?? ""} onChange={(e) => set("exchangeId", e.target.value || undefined)} size="small">
          <MenuItem value="">—</MenuItem>
          {exchanges.map((ex) => <MenuItem key={ex.id} value={ex.id}>{ex.code} — {ex.name}</MenuItem>)}
        </TextField>
        <TextField select label="Currency" value={form.currencyId ?? ""} onChange={(e) => set("currencyId", e.target.value || undefined)} size="small">
          <MenuItem value="">—</MenuItem>
          {currencies.map((c) => <MenuItem key={c.id} value={c.id}>{c.code} — {c.name}</MenuItem>)}
        </TextField>
        <TextField select label="Country" value={form.countryId ?? ""} onChange={(e) => set("countryId", e.target.value || undefined)} size="small">
          <MenuItem value="">—</MenuItem>
          {countries.map((c) => <MenuItem key={c.id} value={c.id}>{c.flagEmoji} {c.name}</MenuItem>)}
        </TextField>
        <TextField
          label="Lot Size" type="number" value={form.lotSize}
          onChange={(e) => set("lotSize", Number(e.target.value) || 1)}
          size="small" required
        />
        <TextField
          label="Tick Size" type="number" value={form.tickSize ?? ""}
          onChange={(e) => set("tickSize", e.target.value ? Number(e.target.value) : undefined)}
          size="small"
          slotProps={{ htmlInput: { step: "0.000001" } }}
        />
        <TextField
          label="Margin Requirement (%)" type="number" value={form.marginRequirement ?? ""}
          onChange={(e) => set("marginRequirement", e.target.value ? Number(e.target.value) : undefined)}
          size="small"
          slotProps={{ htmlInput: { step: "0.01" } }}
        />
        <Box sx={{ display: "flex", alignItems: "center" }}>
          <FormControlLabel
            control={<Switch checked={form.isMarginEligible} onChange={(e) => set("isMarginEligible", e.target.checked)} />}
            label="Margin Eligible"
          />
        </Box>
        <TextField
          label="Listing Date" type="date" value={form.listingDate ?? ""}
          onChange={(e) => set("listingDate", e.target.value || undefined)}
          size="small" slotProps={{ inputLabel: { shrink: true } }}
        />
        <TextField
          label="Delisting Date" type="date" value={form.delistingDate ?? ""}
          onChange={(e) => set("delistingDate", e.target.value || undefined)}
          size="small" slotProps={{ inputLabel: { shrink: true } }}
        />
        <TextField
          label="Expiration Date" type="date" value={form.expirationDate ?? ""}
          onChange={(e) => set("expirationDate", e.target.value || undefined)}
          size="small" slotProps={{ inputLabel: { shrink: true } }}
        />
        <TextField label="Issuer" value={form.issuerName ?? ""} onChange={(e) => set("issuerName", e.target.value || undefined)} size="small" />
        <TextField label="External ID" value={form.externalId ?? ""} onChange={(e) => set("externalId", e.target.value || undefined)} size="small" />
      </Box>
      <TextField
        label="Description" value={form.description ?? ""}
        onChange={(e) => set("description", e.target.value || undefined)}
        size="small" multiline rows={2}
      />
    </>
  );
}

/* ── Create Dialog ── */

interface CreateProps { open: boolean; onClose: () => void }

export function CreateInstrumentDialog({ open, onClose }: CreateProps) {
  const [form, setForm] = useState<CreateInstrumentRequest>(emptyForm);
  const [errors, setErrors] = useState<FieldErrors>({});
  const create = useCreateInstrument();
  const { data: exchanges = [] } = useExchanges();
  const { data: currencies = [] } = useCurrencies();
  const { data: countries = [] } = useCountries();

  const set = <K extends keyof CreateInstrumentRequest>(key: K, value: CreateInstrumentRequest[K]) => {
    setForm((f) => ({ ...f, [key]: value }));
    setErrors((prev) => ({ ...prev, [key]: undefined }));
  };

  const handleSubmit = async () => {
    const errs: FieldErrors = { symbol: validateRequired(form.symbol), name: validateRequired(form.name) };
    if (Object.values(errs).some(Boolean)) { setErrors(errs); return; }
    try {
      await create.mutateAsync({
        ...form,
        isin: form.isin || undefined,
        cusip: form.cusip || undefined,
        sector: form.sector || undefined,
        exchangeId: form.exchangeId || undefined,
        currencyId: form.currencyId || undefined,
        countryId: form.countryId || undefined,
        tickSize: form.tickSize ?? undefined,
        marginRequirement: form.marginRequirement ?? undefined,
        listingDate: form.listingDate || undefined,
        delistingDate: form.delistingDate || undefined,
        expirationDate: form.expirationDate || undefined,
        issuerName: form.issuerName || undefined,
        description: form.description || undefined,
        externalId: form.externalId || undefined,
      });
      setForm(emptyForm);
      onClose();
    } catch { /* handled by MutationCache */ }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Create Instrument</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <InstrumentFormFields form={form} set={set} exchanges={exchanges} currencies={currencies} countries={countries} errors={errors} />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit} disabled={create.isPending}>Create</Button>
      </DialogActions>
    </Dialog>
  );
}

/* ── Edit Dialog ── */

interface EditProps { open: boolean; onClose: () => void; instrument: { id: string; symbol: string } | null }

export function EditInstrumentDialog({ open, onClose, instrument }: EditProps) {
  const [form, setForm] = useState<CreateInstrumentRequest>(emptyForm);
  const [rowVersion, setRowVersion] = useState("");
  const [errors, setErrors] = useState<FieldErrors>({});
  const update = useUpdateInstrument();
  const { data: exchanges = [] } = useExchanges();
  const { data: currencies = [] } = useCurrencies();
  const { data: countries = [] } = useCountries();
  const { data: fullInstrument } = useInstrument(instrument?.id ?? "");

  const [populated, setPopulated] = useState(false);

  const [prevOpen, setPrevOpen] = useState(open);
  if (open && !prevOpen) {
    setPopulated(false);
    setForm(emptyForm());
    setErrors({});
  }
  if (open !== prevOpen) setPrevOpen(open);

  if (open && !populated && fullInstrument) {
    setPopulated(true);
    setForm({
      symbol: fullInstrument.symbol,
      name: fullInstrument.name,
      isin: fullInstrument.isin ?? undefined,
      cusip: fullInstrument.cusip ?? undefined,
      type: fullInstrument.type,
      assetClass: fullInstrument.assetClass,
      status: fullInstrument.status,
      sector: fullInstrument.sector ?? undefined,
      exchangeId: fullInstrument.exchangeId ?? undefined,
      currencyId: fullInstrument.currencyId ?? undefined,
      countryId: fullInstrument.countryId ?? undefined,
      lotSize: fullInstrument.lotSize,
      tickSize: fullInstrument.tickSize ?? undefined,
      marginRequirement: fullInstrument.marginRequirement ?? undefined,
      isMarginEligible: fullInstrument.isMarginEligible,
      listingDate: fullInstrument.listingDate ? fullInstrument.listingDate.split("T")[0] : undefined,
      delistingDate: fullInstrument.delistingDate ? fullInstrument.delistingDate.split("T")[0] : undefined,
      expirationDate: fullInstrument.expirationDate ? fullInstrument.expirationDate.split("T")[0] : undefined,
      issuerName: fullInstrument.issuerName ?? undefined,
      description: fullInstrument.description ?? undefined,
      externalId: fullInstrument.externalId ?? undefined,
    });
    setRowVersion(fullInstrument.rowVersion);
  }

  const set = <K extends keyof CreateInstrumentRequest>(key: K, value: CreateInstrumentRequest[K]) => {
    setForm((f) => ({ ...f, [key]: value }));
    setErrors((prev) => ({ ...prev, [key]: undefined }));
  };

  const handleSubmit = async () => {
    if (!instrument) return;
    const errs: FieldErrors = { symbol: validateRequired(form.symbol), name: validateRequired(form.name) };
    if (Object.values(errs).some(Boolean)) { setErrors(errs); return; }
    try {
      await update.mutateAsync({
        id: instrument.id,
        ...form,
        isin: form.isin || undefined,
        cusip: form.cusip || undefined,
        sector: form.sector || undefined,
        exchangeId: form.exchangeId || undefined,
        currencyId: form.currencyId || undefined,
        countryId: form.countryId || undefined,
        tickSize: form.tickSize ?? undefined,
        marginRequirement: form.marginRequirement ?? undefined,
        listingDate: form.listingDate || undefined,
        delistingDate: form.delistingDate || undefined,
        expirationDate: form.expirationDate || undefined,
        issuerName: form.issuerName || undefined,
        description: form.description || undefined,
        externalId: form.externalId || undefined,
        rowVersion,
      });
      onClose();
    } catch { /* handled by MutationCache */ }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Edit Instrument: {instrument?.symbol}</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <InstrumentFormFields form={form} set={set} exchanges={exchanges} currencies={currencies} countries={countries} errors={errors} />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit} disabled={update.isPending}>Save</Button>
      </DialogActions>
    </Dialog>
  );
}
