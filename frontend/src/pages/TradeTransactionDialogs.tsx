import { useState, useMemo } from "react";
import {
  Dialog, DialogTitle, DialogContent, DialogActions, Button, TextField,
  MenuItem, Box, Autocomplete,
} from "@mui/material";
import {
  useCreateTradeTransaction, useUpdateTradeTransaction, useTradeTransaction, useInstruments, useTradeOrders,
} from "../api/hooks";
import { validateRequired, type FieldErrors } from "../utils/validateFields";
import type {
  CreateTradeTransactionRequest, TransactionStatus, TradeSide,
  InstrumentListItemDto, TradeOrderListItemDto,
} from "../api/types";

const SIDES: TradeSide[] = ["Buy", "Sell", "ShortSell", "BuyToCover"];
const TRANSACTION_STATUSES: TransactionStatus[] = ["Pending", "Settled", "Failed", "Cancelled"];

function emptyForm(): CreateTradeTransactionRequest {
  return {
    instrumentId: "",
    transactionDate: new Date().toISOString().split("T")[0],
    side: "Buy",
    quantity: 0,
    price: 0,
  };
}

/* -- Shared form fields -- */

function TradeTransactionFormFields({ form, set, errors = {}, isEdit = false, status, onStatusChange, selectedOrder, onOrderChange, currentInstrument }: {
  form: CreateTradeTransactionRequest;
  set: (key: string, value: unknown) => void;
  errors?: FieldErrors;
  isEdit?: boolean;
  status?: TransactionStatus;
  onStatusChange?: (value: TransactionStatus) => void;
  selectedOrder: TradeOrderListItemDto | null;
  onOrderChange: (order: TradeOrderListItemDto | null) => void;
  currentInstrument?: { id: string; symbol: string; name: string } | null;
}) {
  const { data: instrumentsData } = useInstruments({ page: 1, pageSize: 200 });
  const { data: ordersData } = useTradeOrders({ page: 1, pageSize: 200 });

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
        <Autocomplete
          options={instruments}
          getOptionLabel={(o) => `${o.symbol} — ${o.name}`}
          value={instruments.find((i) => i.id === form.instrumentId) ?? null}
          onChange={(_, v) => set("instrumentId", v?.id ?? "")}
          isOptionEqualToValue={(o, v) => o.id === v.id}
          size="small"
          renderInput={(params) => (
            <TextField {...params} label="Instrument" required error={!!errors.instrumentId} helperText={errors.instrumentId} />
          )}
        />
        <TextField select label="Side" value={form.side} onChange={(e) => set("side", e.target.value)} size="small">
          {SIDES.map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
        </TextField>
        <TextField
          label="Transaction Date" type="date" value={form.transactionDate ?? ""}
          onChange={(e) => set("transactionDate", e.target.value)}
          size="small" required slotProps={{ inputLabel: { shrink: true } }}
          error={!!errors.transactionDate} helperText={errors.transactionDate}
        />
        {isEdit && (
          <TextField select label="Status" value={status ?? "Pending"} onChange={(e) => onStatusChange?.(e.target.value as TransactionStatus)} size="small">
            {TRANSACTION_STATUSES.map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
          </TextField>
        )}
        <TextField
          label="Quantity" type="number" value={form.quantity}
          onChange={(e) => set("quantity", e.target.value === "" ? 0 : Number(e.target.value))}
          size="small" required error={!!errors.quantity} helperText={errors.quantity}
          slotProps={{ htmlInput: { min: 0 } }}
        />
        <TextField
          label="Price" type="number" value={form.price}
          onChange={(e) => set("price", e.target.value === "" ? 0 : Number(e.target.value))}
          size="small" required error={!!errors.price} helperText={errors.price}
          slotProps={{ htmlInput: { min: 0, step: "any" } }}
        />
        <TextField
          label="Commission" type="number" value={form.commission ?? ""}
          onChange={(e) => set("commission", e.target.value === "" ? undefined : Number(e.target.value))}
          size="small" slotProps={{ htmlInput: { min: 0, step: "any" } }}
        />
        <TextField
          label="Settlement Date" type="date" value={form.settlementDate ?? ""}
          onChange={(e) => set("settlementDate", e.target.value || undefined)}
          size="small" slotProps={{ inputLabel: { shrink: true } }}
        />
        <TextField label="Venue" value={form.venue ?? ""} onChange={(e) => set("venue", e.target.value || undefined)} size="small" />
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

/* -- Create Dialog -- */

interface CreateProps { open: boolean; onClose: () => void; currentOrder?: TradeOrderListItemDto | null }

export function CreateTradeTransactionDialog({ open, onClose, currentOrder }: CreateProps) {
  const [form, setForm] = useState<CreateTradeTransactionRequest>(emptyForm);
  const [selectedOrder, setSelectedOrder] = useState<TradeOrderListItemDto | null>(null);
  const [errors, setErrors] = useState<FieldErrors>({});
  const create = useCreateTradeTransaction();

  const [prevOpen, setPrevOpen] = useState(open);
  if (open && !prevOpen) {
    setForm({ ...emptyForm(), ...(currentOrder ? { orderId: currentOrder.id } : {}) });
    setSelectedOrder(currentOrder ?? null);
    setErrors({});
  }
  if (open !== prevOpen) setPrevOpen(open);

  const set = (key: string, value: unknown) => {
    setForm((f) => ({ ...f, [key]: value }));
    setErrors((prev) => ({ ...prev, [key]: undefined }));
  };

  const handleSubmit = async () => {
    const errs: FieldErrors = {
      instrumentId: validateRequired(form.instrumentId),
      transactionDate: validateRequired(form.transactionDate),
      quantity: form.quantity > 0 ? undefined : "Quantity is required",
      price: form.price > 0 ? undefined : "Price is required",
    };
    if (Object.values(errs).some(Boolean)) { setErrors(errs); return; }
    try {
      await create.mutateAsync({
        ...form,
        orderId: form.orderId || undefined,
        commission: form.commission ?? undefined,
        settlementDate: form.settlementDate || undefined,
        venue: form.venue || undefined,
        comment: form.comment || undefined,
        externalId: form.externalId || undefined,
      });
      setForm(emptyForm);
      onClose();
    } catch { /* handled by MutationCache */ }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Create Trade Transaction</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <TradeTransactionFormFields form={form} set={set} errors={errors} selectedOrder={selectedOrder} onOrderChange={setSelectedOrder} />
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

export function EditTradeTransactionDialog({ transaction, onClose }: EditProps) {
  const open = transaction !== null;
  const [form, setForm] = useState<CreateTradeTransactionRequest>(emptyForm);
  const [status, setStatus] = useState<TransactionStatus>("Pending");
  const [rowVersion, setRowVersion] = useState("");
  const [errors, setErrors] = useState<FieldErrors>({});
  const update = useUpdateTradeTransaction();
  const { data: fullTransaction } = useTradeTransaction(transaction?.id ?? "");

  const [selectedOrder, setSelectedOrder] = useState<TradeOrderListItemDto | null>(null);
  const [populated, setPopulated] = useState(false);

  const [prevOpen, setPrevOpen] = useState(open);
  if (open && !prevOpen) {
    setPopulated(false);
    setForm(emptyForm());
    setSelectedOrder(null);
    setStatus("Pending");
    setErrors({});
  }
  if (open !== prevOpen) setPrevOpen(open);

  if (open && !populated && fullTransaction) {
    setPopulated(true);
    setForm({
      orderId: fullTransaction.orderId ?? undefined,
      instrumentId: fullTransaction.instrumentId,
      transactionDate: fullTransaction.transactionDate ? fullTransaction.transactionDate.split("T")[0] : "",
      side: fullTransaction.side,
      quantity: fullTransaction.quantity,
      price: fullTransaction.price,
      commission: fullTransaction.commission ?? undefined,
      settlementDate: fullTransaction.settlementDate ? fullTransaction.settlementDate.split("T")[0] : undefined,
      venue: fullTransaction.venue ?? undefined,
      comment: fullTransaction.comment ?? undefined,
      externalId: fullTransaction.externalId ?? undefined,
    });
    if (fullTransaction.orderId && fullTransaction.orderNumber) {
      setSelectedOrder({ id: fullTransaction.orderId, orderNumber: fullTransaction.orderNumber } as TradeOrderListItemDto);
    }
    setStatus(fullTransaction.status);
    setRowVersion(fullTransaction.rowVersion);
  }

  const currentInstrument = useMemo(() =>
    fullTransaction ? { id: fullTransaction.instrumentId, symbol: fullTransaction.instrumentSymbol, name: fullTransaction.instrumentName } : null,
  [fullTransaction]);

  const set = (key: string, value: unknown) => {
    setForm((f) => ({ ...f, [key]: value }));
    setErrors((prev) => ({ ...prev, [key]: undefined }));
  };

  const handleSubmit = async () => {
    if (!transaction) return;
    const errs: FieldErrors = {
      instrumentId: validateRequired(form.instrumentId),
      transactionDate: validateRequired(form.transactionDate),
      quantity: form.quantity > 0 ? undefined : "Quantity is required",
      price: form.price > 0 ? undefined : "Price is required",
    };
    if (Object.values(errs).some(Boolean)) { setErrors(errs); return; }
    try {
      await update.mutateAsync({
        id: transaction.id,
        orderId: form.orderId || undefined,
        instrumentId: form.instrumentId,
        transactionDate: form.transactionDate,
        status,
        side: form.side,
        quantity: form.quantity,
        price: form.price,
        commission: form.commission ?? undefined,
        settlementDate: form.settlementDate || undefined,
        venue: form.venue || undefined,
        comment: form.comment || undefined,
        externalId: form.externalId || undefined,
        rowVersion,
      });
      onClose();
    } catch { /* handled by MutationCache */ }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Edit Trade Transaction: {fullTransaction?.transactionNumber}</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <TradeTransactionFormFields
          form={form} set={set} errors={errors}
          isEdit
          status={status} onStatusChange={setStatus}
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
