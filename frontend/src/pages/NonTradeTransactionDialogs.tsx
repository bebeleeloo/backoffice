import { useState, useMemo } from "react";
import {
  Dialog, DialogTitle, DialogContent, DialogActions, Autocomplete, Button, TextField,
  MenuItem, Box,
} from "@mui/material";
import {
  useCreateNonTradeTransaction, useUpdateNonTradeTransaction, useNonTradeTransaction,
  useCurrencies, useInstruments, useNonTradeOrders,
} from "../api/hooks";
import { validateRequired, type FieldErrors } from "../utils/validateFields";
import type {
  CreateNonTradeTransactionRequest, UpdateNonTradeTransactionRequest,
  TransactionStatus, InstrumentListItemDto, NonTradeOrderListItemDto,
} from "../api/types";

const TRANSACTION_STATUSES: TransactionStatus[] = ["Pending", "Settled", "Failed", "Cancelled"];

function emptyForm(): CreateNonTradeTransactionRequest {
  return {
    transactionDate: new Date().toISOString().split("T")[0],
    amount: 0,
    currencyId: "",
  };
}

/* -- Shared form fields -- */

function NonTradeTransactionFormFields({ form, set, errors = {}, showStatusFields = false, status, onStatusChange, processedAt, onProcessedAtChange, selectedOrder, onOrderChange, currentInstrument }: {
  form: CreateNonTradeTransactionRequest;
  set: <K extends keyof CreateNonTradeTransactionRequest>(key: K, value: CreateNonTradeTransactionRequest[K]) => void;
  errors?: FieldErrors;
  showStatusFields?: boolean;
  status?: TransactionStatus;
  onStatusChange?: (value: TransactionStatus) => void;
  processedAt?: string;
  onProcessedAtChange?: (value: string | undefined) => void;
  selectedOrder: NonTradeOrderListItemDto | null;
  onOrderChange: (order: NonTradeOrderListItemDto | null) => void;
  currentInstrument?: { id: string; symbol: string; name: string } | null;
}) {
  const currencies = useCurrencies();
  const { data: instrumentsData } = useInstruments({ page: 1, pageSize: 200 });
  const { data: ordersData } = useNonTradeOrders({ page: 1, pageSize: 200 });

  const instruments = useMemo(() => {
    const items = instrumentsData?.items ?? [];
    if (currentInstrument && !items.some((i) => i.id === currentInstrument.id)) {
      return [{ id: currentInstrument.id, symbol: currentInstrument.symbol, name: currentInstrument.name } as InstrumentListItemDto, ...items];
    }
    return items;
  }, [instrumentsData, currentInstrument]);

  const orders = useMemo(() => {
    const items = ordersData?.items ?? [];
    if (selectedOrder && !items.some((o) => o.id === selectedOrder.id)) {
      return [selectedOrder, ...items];
    }
    return items;
  }, [ordersData, selectedOrder]);

  return (
    <>
      <Box sx={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 2 }}>
        <Autocomplete
          options={orders}
          getOptionLabel={(o) => o.orderNumber}
          value={selectedOrder}
          onChange={(_, v) => {
            onOrderChange(v);
            set("orderId", v?.id ?? undefined);
          }}
          isOptionEqualToValue={(o, v) => o.id === v.id}
          size="small"
          renderInput={(params) => (
            <TextField {...params} label="Order" />
          )}
        />
        <TextField
          label="Transaction Date" type="date" value={form.transactionDate ?? ""}
          onChange={(e) => set("transactionDate", e.target.value)}
          size="small" required slotProps={{ inputLabel: { shrink: true } }}
          error={!!errors.transactionDate} helperText={errors.transactionDate}
        />
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
              select label="Status" value={status ?? "Pending"}
              onChange={(e) => onStatusChange?.(e.target.value as TransactionStatus)} size="small"
            >
              {TRANSACTION_STATUSES.map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
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

/* -- Create Dialog -- */

interface CreateProps { open: boolean; onClose: () => void; currentOrder?: NonTradeOrderListItemDto | null }

export function CreateNonTradeTransactionDialog({ open, onClose, currentOrder }: CreateProps) {
  const [form, setForm] = useState<CreateNonTradeTransactionRequest>(emptyForm);
  const [selectedOrder, setSelectedOrder] = useState<NonTradeOrderListItemDto | null>(null);
  const [errors, setErrors] = useState<FieldErrors>({});
  const create = useCreateNonTradeTransaction();

  const [prevOpen, setPrevOpen] = useState(open);
  if (open && !prevOpen) {
    setForm({ ...emptyForm(), ...(currentOrder ? { orderId: currentOrder.id } : {}) });
    setSelectedOrder(currentOrder ?? null);
    setErrors({});
  }
  if (open !== prevOpen) setPrevOpen(open);

  const set = <K extends keyof CreateNonTradeTransactionRequest>(key: K, value: CreateNonTradeTransactionRequest[K]) => {
    setForm((f) => ({ ...f, [key]: value }));
    setErrors((prev) => ({ ...prev, [key]: undefined }));
  };

  const handleSubmit = async () => {
    const errs: FieldErrors = {
      transactionDate: validateRequired(form.transactionDate),
      amount: form.amount ? undefined : "Required",
      currencyId: validateRequired(form.currencyId),
    };
    if (Object.values(errs).some(Boolean)) { setErrors(errs); return; }
    try {
      await create.mutateAsync({
        ...form,
        orderId: form.orderId || undefined,
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
      <DialogTitle>Create Non-Trade Transaction</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <NonTradeTransactionFormFields form={form} set={set} errors={errors} selectedOrder={selectedOrder} onOrderChange={setSelectedOrder} />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit} disabled={create.isPending}>Create</Button>
      </DialogActions>
    </Dialog>
  );
}

/* -- Edit Dialog -- */

interface EditProps { transaction: { id: string } | null; onClose: () => void }

export function EditNonTradeTransactionDialog({ transaction, onClose }: EditProps) {
  const open = !!transaction;
  const [form, setForm] = useState<CreateNonTradeTransactionRequest>(emptyForm);
  const [status, setStatus] = useState<TransactionStatus>("Pending");
  const [processedAt, setProcessedAt] = useState<string | undefined>(undefined);
  const [rowVersion, setRowVersion] = useState("");
  const [errors, setErrors] = useState<FieldErrors>({});
  const update = useUpdateNonTradeTransaction();
  const { data: fullTransaction } = useNonTradeTransaction(transaction?.id ?? "");

  const [selectedOrder, setSelectedOrder] = useState<NonTradeOrderListItemDto | null>(null);
  const [populated, setPopulated] = useState(false);

  const [prevOpen, setPrevOpen] = useState(open);
  if (open && !prevOpen) {
    setPopulated(false);
    setForm(emptyForm());
    setSelectedOrder(null);
    setStatus("Pending");
    setProcessedAt(undefined);
    setErrors({});
  }
  if (open !== prevOpen) setPrevOpen(open);

  if (open && !populated && fullTransaction) {
    setPopulated(true);
    setForm({
      orderId: fullTransaction.orderId ?? undefined,
      transactionDate: fullTransaction.transactionDate ? fullTransaction.transactionDate.split("T")[0] : "",
      amount: fullTransaction.amount,
      currencyId: fullTransaction.currencyId,
      instrumentId: fullTransaction.instrumentId ?? undefined,
      referenceNumber: fullTransaction.referenceNumber ?? undefined,
      description: fullTransaction.description ?? undefined,
      comment: fullTransaction.comment ?? undefined,
      externalId: fullTransaction.externalId ?? undefined,
    });
    if (fullTransaction.orderId && fullTransaction.orderNumber) {
      setSelectedOrder({ id: fullTransaction.orderId, orderNumber: fullTransaction.orderNumber } as NonTradeOrderListItemDto);
    }
    setStatus(fullTransaction.status);
    setProcessedAt(fullTransaction.processedAt ? fullTransaction.processedAt.split("T")[0] : undefined);
    setRowVersion(fullTransaction.rowVersion);
  }

  const currentInstrument = useMemo(() =>
    fullTransaction?.instrumentId ? { id: fullTransaction.instrumentId, symbol: fullTransaction.instrumentSymbol ?? "", name: fullTransaction.instrumentName ?? "" } : null,
  [fullTransaction]);

  const set = <K extends keyof CreateNonTradeTransactionRequest>(key: K, value: CreateNonTradeTransactionRequest[K]) => {
    setForm((f) => ({ ...f, [key]: value }));
    setErrors((prev) => ({ ...prev, [key]: undefined }));
  };

  const handleSubmit = async () => {
    if (!transaction) return;
    const errs: FieldErrors = {
      transactionDate: validateRequired(form.transactionDate),
      amount: form.amount ? undefined : "Required",
      currencyId: validateRequired(form.currencyId),
    };
    if (Object.values(errs).some(Boolean)) { setErrors(errs); return; }
    try {
      const payload: UpdateNonTradeTransactionRequest = {
        id: transaction.id,
        orderId: form.orderId || undefined,
        transactionDate: form.transactionDate,
        status,
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
      <DialogTitle>Edit Non-Trade Transaction: {fullTransaction?.transactionNumber}</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <NonTradeTransactionFormFields
          form={form} set={set} errors={errors}
          showStatusFields
          status={status} onStatusChange={setStatus}
          processedAt={processedAt} onProcessedAtChange={setProcessedAt}
          selectedOrder={selectedOrder} onOrderChange={setSelectedOrder} currentInstrument={currentInstrument}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit} disabled={update.isPending}>Save</Button>
      </DialogActions>
    </Dialog>
  );
}
