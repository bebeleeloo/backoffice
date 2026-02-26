import { useState, useMemo } from "react";
import {
  Dialog, DialogTitle, DialogContent, DialogActions, Autocomplete, Button, TextField,
  MenuItem, Box,
} from "@mui/material";
import {
  useCreateNonTradeOrder, useUpdateNonTradeOrder, useNonTradeOrder,
  useAccounts, useCurrencies, useInstruments,
} from "../api/hooks";
import { validateRequired, type FieldErrors } from "../utils/validateFields";
import type {
  CreateNonTradeOrderRequest, UpdateNonTradeOrderRequest,
  NonTradeOrderType, OrderStatus, AccountListItemDto, InstrumentListItemDto,
} from "../api/types";

const NON_TRADE_TYPES: NonTradeOrderType[] = [
  "Deposit", "Withdrawal", "Dividend", "CorporateAction",
  "Fee", "Interest", "Transfer", "Adjustment",
];

const ORDER_STATUSES: OrderStatus[] = [
  "New", "PendingApproval", "Approved", "Rejected", "InProgress",
  "PartiallyFilled", "Filled", "Completed", "Cancelled", "Failed",
];

function emptyForm(): CreateNonTradeOrderRequest {
  return {
    accountId: "",
    orderDate: new Date().toISOString().split("T")[0],
    nonTradeType: "Deposit",
    amount: 0,
    currencyId: "",
  };
}

/* ── Shared form fields ── */

function NonTradeOrderFormFields({ form, set, errors = {}, showStatusFields = false, status, onStatusChange, processedAt, onProcessedAtChange, currentAccount, currentInstrument }: {
  form: CreateNonTradeOrderRequest;
  set: <K extends keyof CreateNonTradeOrderRequest>(key: K, value: CreateNonTradeOrderRequest[K]) => void;
  errors?: FieldErrors;
  showStatusFields?: boolean;
  status?: OrderStatus;
  onStatusChange?: (value: OrderStatus) => void;
  processedAt?: string;
  onProcessedAtChange?: (value: string | undefined) => void;
  currentAccount?: { id: string; number: string } | null;
  currentInstrument?: { id: string; symbol: string; name: string } | null;
}) {
  const { data: accountsData } = useAccounts({ page: 1, pageSize: 200 });
  const currencies = useCurrencies();
  const { data: instrumentsData } = useInstruments({ page: 1, pageSize: 200 });

  const accounts = useMemo(() => {
    const items = accountsData?.items ?? [];
    if (currentAccount && !items.some((a) => a.id === currentAccount.id)) {
      return [{ id: currentAccount.id, number: currentAccount.number } as AccountListItemDto, ...items];
    }
    return items;
  }, [accountsData, currentAccount]);

  const instruments = useMemo(() => {
    const items = instrumentsData?.items ?? [];
    if (currentInstrument && !items.some((i) => i.id === currentInstrument.id)) {
      return [{ id: currentInstrument.id, symbol: currentInstrument.symbol, name: currentInstrument.name } as InstrumentListItemDto, ...items];
    }
    return items;
  }, [instrumentsData, currentInstrument]);

  return (
    <>
      <Box sx={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 2 }}>
        <TextField
          select label="Account" value={form.accountId}
          onChange={(e) => set("accountId", e.target.value)} size="small" required
          error={!!errors.accountId} helperText={errors.accountId}
        >
          <MenuItem value="">—</MenuItem>
          {accounts.map((a) => <MenuItem key={a.id} value={a.id}>{a.number}</MenuItem>)}
        </TextField>
        <TextField
          label="Order Date" type="date" value={form.orderDate ?? ""}
          onChange={(e) => set("orderDate", e.target.value)}
          size="small" required slotProps={{ inputLabel: { shrink: true } }}
          error={!!errors.orderDate} helperText={errors.orderDate}
        />
        <TextField
          select label="Type" value={form.nonTradeType}
          onChange={(e) => set("nonTradeType", e.target.value as NonTradeOrderType)} size="small" required
          error={!!errors.nonTradeType} helperText={errors.nonTradeType}
        >
          {NON_TRADE_TYPES.map((t) => <MenuItem key={t} value={t}>{t}</MenuItem>)}
        </TextField>
        <TextField
          label="Amount" type="number" value={form.amount}
          onChange={(e) => set("amount", Number(e.target.value))} size="small" required
          error={!!errors.amount} helperText={errors.amount}
        />
        <TextField
          select label="Currency" value={form.currencyId}
          onChange={(e) => set("currencyId", e.target.value)} size="small" required
          error={!!errors.currencyId} helperText={errors.currencyId}
        >
          <MenuItem value="">—</MenuItem>
          {(currencies.data ?? []).map((c) => <MenuItem key={c.id} value={c.id}>{c.code} — {c.name}</MenuItem>)}
        </TextField>
        <Autocomplete
          options={instruments}
          getOptionLabel={(o) => `${o.symbol} — ${o.name}`}
          value={instruments.find((i) => i.id === form.instrumentId) ?? null}
          onChange={(_, v) => set("instrumentId", v?.id ?? undefined)}
          isOptionEqualToValue={(o, v) => o.id === v.id}
          size="small"
          renderInput={(params) => (
            <TextField {...params} label="Instrument" />
          )}
        />
        <TextField
          label="Reference Number" value={form.referenceNumber ?? ""}
          onChange={(e) => set("referenceNumber", e.target.value || undefined)} size="small"
        />
        {showStatusFields && (
          <>
            <TextField
              select label="Status" value={status ?? "New"}
              onChange={(e) => onStatusChange?.(e.target.value as OrderStatus)} size="small"
            >
              {ORDER_STATUSES.map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
            </TextField>
            <TextField
              label="Processed At" type="date" value={processedAt ?? ""}
              onChange={(e) => onProcessedAtChange?.(e.target.value || undefined)}
              size="small" slotProps={{ inputLabel: { shrink: true } }}
            />
          </>
        )}
        <TextField
          label="External ID" value={form.externalId ?? ""}
          onChange={(e) => set("externalId", e.target.value || undefined)} size="small"
        />
        <TextField
          label="Comment" value={form.comment ?? ""}
          onChange={(e) => set("comment", e.target.value || undefined)} size="small"
        />
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

export function CreateNonTradeOrderDialog({ open, onClose }: CreateProps) {
  const [form, setForm] = useState<CreateNonTradeOrderRequest>(emptyForm);
  const [errors, setErrors] = useState<FieldErrors>({});
  const create = useCreateNonTradeOrder();

  const set = <K extends keyof CreateNonTradeOrderRequest>(key: K, value: CreateNonTradeOrderRequest[K]) => {
    setForm((f) => ({ ...f, [key]: value }));
    setErrors((prev) => ({ ...prev, [key]: undefined }));
  };

  const handleSubmit = async () => {
    const errs: FieldErrors = {
      accountId: validateRequired(form.accountId),
      orderDate: validateRequired(form.orderDate),
      nonTradeType: validateRequired(form.nonTradeType),
      amount: form.amount ? undefined : "Required",
      currencyId: validateRequired(form.currencyId),
    };
    if (Object.values(errs).some(Boolean)) { setErrors(errs); return; }
    try {
      await create.mutateAsync({
        ...form,
        instrumentId: form.instrumentId || undefined,
        referenceNumber: form.referenceNumber || undefined,
        description: form.description || undefined,
        comment: form.comment || undefined,
        externalId: form.externalId || undefined,
      });
      setForm(emptyForm);
      onClose();
    } catch { /* handled by MutationCache */ }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Create Non-Trade Order</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <NonTradeOrderFormFields form={form} set={set} errors={errors} />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit} disabled={create.isPending}>Create</Button>
      </DialogActions>
    </Dialog>
  );
}

/* ── Edit Dialog ── */

interface EditProps { order: { id: string } | null; onClose: () => void }

export function EditNonTradeOrderDialog({ order, onClose }: EditProps) {
  const open = !!order;
  const [form, setForm] = useState<CreateNonTradeOrderRequest>(emptyForm);
  const [status, setStatus] = useState<OrderStatus>("New");
  const [processedAt, setProcessedAt] = useState<string | undefined>(undefined);
  const [rowVersion, setRowVersion] = useState("");
  const [errors, setErrors] = useState<FieldErrors>({});
  const update = useUpdateNonTradeOrder();
  const { data: fullOrder } = useNonTradeOrder(order?.id ?? "");

  const [populated, setPopulated] = useState(false);

  const [prevOpen, setPrevOpen] = useState(open);
  if (open && !prevOpen) {
    setPopulated(false);
    setForm(emptyForm());
    setStatus("New");
    setProcessedAt(undefined);
    setErrors({});
  }
  if (open !== prevOpen) setPrevOpen(open);

  if (open && !populated && fullOrder) {
    setPopulated(true);
    setForm({
      accountId: fullOrder.accountId,
      orderDate: fullOrder.orderDate ? fullOrder.orderDate.split("T")[0] : "",
      nonTradeType: fullOrder.nonTradeType,
      amount: fullOrder.amount,
      currencyId: fullOrder.currencyId,
      instrumentId: fullOrder.instrumentId ?? undefined,
      referenceNumber: fullOrder.referenceNumber ?? undefined,
      description: fullOrder.description ?? undefined,
      comment: fullOrder.comment ?? undefined,
      externalId: fullOrder.externalId ?? undefined,
    });
    setStatus(fullOrder.status);
    setProcessedAt(fullOrder.processedAt ? fullOrder.processedAt.split("T")[0] : undefined);
    setRowVersion(fullOrder.rowVersion);
  }

  const currentAccount = useMemo(() =>
    fullOrder ? { id: fullOrder.accountId, number: fullOrder.accountNumber } : null,
  [fullOrder]);

  const currentInstrument = useMemo(() =>
    fullOrder?.instrumentId ? { id: fullOrder.instrumentId, symbol: fullOrder.instrumentSymbol ?? "", name: fullOrder.instrumentName ?? "" } : null,
  [fullOrder]);

  const set = <K extends keyof CreateNonTradeOrderRequest>(key: K, value: CreateNonTradeOrderRequest[K]) => {
    setForm((f) => ({ ...f, [key]: value }));
    setErrors((prev) => ({ ...prev, [key]: undefined }));
  };

  const handleSubmit = async () => {
    if (!order) return;
    const errs: FieldErrors = {
      accountId: validateRequired(form.accountId),
      orderDate: validateRequired(form.orderDate),
      nonTradeType: validateRequired(form.nonTradeType),
      amount: form.amount ? undefined : "Required",
      currencyId: validateRequired(form.currencyId),
    };
    if (Object.values(errs).some(Boolean)) { setErrors(errs); return; }
    try {
      const payload: UpdateNonTradeOrderRequest = {
        id: order.id,
        accountId: form.accountId,
        orderDate: form.orderDate,
        status,
        nonTradeType: form.nonTradeType,
        amount: form.amount,
        currencyId: form.currencyId,
        instrumentId: form.instrumentId || undefined,
        referenceNumber: form.referenceNumber || undefined,
        description: form.description || undefined,
        processedAt: processedAt || undefined,
        comment: form.comment || undefined,
        externalId: form.externalId || undefined,
        rowVersion,
      };
      await update.mutateAsync(payload);
      onClose();
    } catch { /* handled by MutationCache */ }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Edit Non-Trade Order: {fullOrder?.orderNumber}</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <NonTradeOrderFormFields
          form={form} set={set} errors={errors}
          showStatusFields
          status={status} onStatusChange={setStatus}
          processedAt={processedAt} onProcessedAtChange={setProcessedAt}
          currentAccount={currentAccount} currentInstrument={currentInstrument}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit} disabled={update.isPending}>Save</Button>
      </DialogActions>
    </Dialog>
  );
}
