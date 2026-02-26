import { useState, useMemo } from "react";
import {
  Dialog, DialogTitle, DialogContent, DialogActions, Button, TextField,
  MenuItem, Box, Autocomplete,
} from "@mui/material";
import {
  useCreateTradeOrder, useUpdateTradeOrder, useTradeOrder, useAccounts, useInstruments,
} from "../api/hooks";
import { validateRequired, type FieldErrors } from "../utils/validateFields";
import type {
  CreateTradeOrderRequest, UpdateTradeOrderRequest, OrderStatus, TradeSide,
  TradeOrderType, TimeInForce, AccountListItemDto, InstrumentListItemDto,
} from "../api/types";

const SIDES: TradeSide[] = ["Buy", "Sell", "ShortSell", "BuyToCover"];
const ORDER_TYPES: TradeOrderType[] = ["Market", "Limit", "Stop", "StopLimit"];
const TIME_IN_FORCE_OPTIONS: TimeInForce[] = ["Day", "GTC", "IOC", "FOK", "GTD"];
const ORDER_STATUSES: OrderStatus[] = [
  "New", "PendingApproval", "Approved", "Rejected", "InProgress",
  "PartiallyFilled", "Filled", "Completed", "Cancelled", "Failed",
];

function emptyForm(): CreateTradeOrderRequest {
  return {
    accountId: "",
    instrumentId: "",
    orderDate: new Date().toISOString().split("T")[0],
    side: "Buy",
    orderType: "Market",
    timeInForce: "Day",
    quantity: 0,
  };
}

/* ── Shared form fields ── */

function TradeOrderFormFields({ form, set, errors = {}, isEdit = false, currentAccount, currentInstrument }: {
  form: CreateTradeOrderRequest & Partial<Pick<UpdateTradeOrderRequest, "status" | "executedQuantity" | "averagePrice" | "executedAt">>;
  set: (key: string, value: unknown) => void;
  errors?: FieldErrors;
  isEdit?: boolean;
  currentAccount?: { id: string; number: string } | null;
  currentInstrument?: { id: string; symbol: string; name: string } | null;
}) {
  const { data: accountsData } = useAccounts({ page: 1, pageSize: 200 });
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
        <TextField select label="Order Type" value={form.orderType} onChange={(e) => set("orderType", e.target.value)} size="small">
          {ORDER_TYPES.map((t) => <MenuItem key={t} value={t}>{t}</MenuItem>)}
        </TextField>
        <TextField select label="Time In Force" value={form.timeInForce} onChange={(e) => set("timeInForce", e.target.value)} size="small">
          {TIME_IN_FORCE_OPTIONS.map((t) => <MenuItem key={t} value={t}>{t}</MenuItem>)}
        </TextField>
        <TextField
          label="Order Date" type="date" value={form.orderDate ?? ""}
          onChange={(e) => set("orderDate", e.target.value)}
          size="small" required slotProps={{ inputLabel: { shrink: true } }}
          error={!!errors.orderDate} helperText={errors.orderDate}
        />
        {isEdit && (
          <TextField select label="Status" value={form.status ?? "New"} onChange={(e) => set("status", e.target.value)} size="small">
            {ORDER_STATUSES.map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
          </TextField>
        )}
        <TextField
          label="Quantity" type="number" value={form.quantity}
          onChange={(e) => set("quantity", e.target.value === "" ? 0 : Number(e.target.value))}
          size="small" required error={!!errors.quantity} helperText={errors.quantity}
          slotProps={{ htmlInput: { min: 0 } }}
        />
        <TextField
          label="Price" type="number" value={form.price ?? ""}
          onChange={(e) => set("price", e.target.value === "" ? undefined : Number(e.target.value))}
          size="small" slotProps={{ htmlInput: { min: 0, step: "any" } }}
          error={!!errors.price} helperText={errors.price}
        />
        <TextField
          label="Stop Price" type="number" value={form.stopPrice ?? ""}
          onChange={(e) => set("stopPrice", e.target.value === "" ? undefined : Number(e.target.value))}
          size="small" slotProps={{ htmlInput: { min: 0, step: "any" } }}
          error={!!errors.stopPrice} helperText={errors.stopPrice}
        />
        <TextField
          label="Commission" type="number" value={form.commission ?? ""}
          onChange={(e) => set("commission", e.target.value === "" ? undefined : Number(e.target.value))}
          size="small" slotProps={{ htmlInput: { min: 0, step: "any" } }}
        />
        {isEdit && (
          <>
            <TextField
              label="Executed Quantity" type="number" value={form.executedQuantity ?? 0}
              onChange={(e) => set("executedQuantity", e.target.value === "" ? 0 : Number(e.target.value))}
              size="small" slotProps={{ htmlInput: { min: 0 } }}
            />
            <TextField
              label="Average Price" type="number" value={form.averagePrice ?? ""}
              onChange={(e) => set("averagePrice", e.target.value === "" ? undefined : Number(e.target.value))}
              size="small" slotProps={{ htmlInput: { min: 0, step: "any" } }}
            />
            <TextField
              label="Executed At" type="datetime-local" value={form.executedAt ?? ""}
              onChange={(e) => set("executedAt", e.target.value || undefined)}
              size="small" slotProps={{ inputLabel: { shrink: true } }}
            />
          </>
        )}
        <TextField
          label="Expiration Date" type="date" value={form.expirationDate ?? ""}
          onChange={(e) => set("expirationDate", e.target.value || undefined)}
          size="small" slotProps={{ inputLabel: { shrink: true } }}
          error={!!errors.expirationDate} helperText={errors.expirationDate}
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

interface CreateProps { open: boolean; onClose: () => void; currentAccount?: { id: string; number: string } | null }

export function CreateTradeOrderDialog({ open, onClose, currentAccount }: CreateProps) {
  const [form, setForm] = useState<CreateTradeOrderRequest>(emptyForm);
  const [errors, setErrors] = useState<FieldErrors>({});
  const create = useCreateTradeOrder();

  const [prevOpen, setPrevOpen] = useState(open);
  if (open && !prevOpen) {
    setForm({ ...emptyForm(), ...(currentAccount ? { accountId: currentAccount.id } : {}) });
    setErrors({});
  }
  if (open !== prevOpen) setPrevOpen(open);

  const set = (key: string, value: unknown) => {
    setForm((f) => ({ ...f, [key]: value }));
    setErrors((prev) => ({ ...prev, [key]: undefined }));
  };

  const handleSubmit = async () => {
    const errs: FieldErrors = {
      accountId: validateRequired(form.accountId),
      instrumentId: validateRequired(form.instrumentId),
      orderDate: validateRequired(form.orderDate),
      quantity: form.quantity > 0 ? undefined : "Quantity is required",
      price: (form.orderType === "Limit" || form.orderType === "StopLimit") && !form.price
        ? "Price is required for Limit/StopLimit orders" : undefined,
      stopPrice: (form.orderType === "Stop" || form.orderType === "StopLimit") && !form.stopPrice
        ? "Stop Price is required for Stop/StopLimit orders" : undefined,
      expirationDate: form.timeInForce === "GTD" && !form.expirationDate
        ? "Expiration Date is required for GTD orders" : undefined,
    };
    if (Object.values(errs).some(Boolean)) { setErrors(errs); return; }
    try {
      await create.mutateAsync({
        ...form,
        orderDate: form.orderDate,
        price: form.price ?? undefined,
        stopPrice: form.stopPrice ?? undefined,
        commission: form.commission ?? undefined,
        expirationDate: form.expirationDate || undefined,
        comment: form.comment || undefined,
        externalId: form.externalId || undefined,
      });
      setForm(emptyForm);
      onClose();
    } catch { /* handled by MutationCache */ }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Create Trade Order</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <TradeOrderFormFields form={form} set={set} errors={errors} currentAccount={currentAccount} />
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

type EditForm = CreateTradeOrderRequest & Pick<UpdateTradeOrderRequest, "status" | "executedQuantity" | "averagePrice" | "executedAt">;

function emptyEditForm(): EditForm {
  return {
    ...emptyForm(),
    status: "New",
    executedQuantity: 0,
    averagePrice: undefined,
    executedAt: undefined,
  };
}

export function EditTradeOrderDialog({ order, onClose }: EditProps) {
  const open = order !== null;
  const [form, setForm] = useState<EditForm>(emptyEditForm);
  const [rowVersion, setRowVersion] = useState("");
  const [errors, setErrors] = useState<FieldErrors>({});
  const update = useUpdateTradeOrder();
  const { data: fullOrder } = useTradeOrder(order?.id ?? "");

  const [populated, setPopulated] = useState(false);

  const [prevOpen, setPrevOpen] = useState(open);
  if (open && !prevOpen) {
    setPopulated(false);
    setForm(emptyEditForm());
    setErrors({});
  }
  if (open !== prevOpen) setPrevOpen(open);

  if (open && !populated && fullOrder) {
    setPopulated(true);
    setForm({
      accountId: fullOrder.accountId,
      instrumentId: fullOrder.instrumentId,
      orderDate: fullOrder.orderDate ? fullOrder.orderDate.split("T")[0] : "",
      side: fullOrder.side,
      orderType: fullOrder.orderType,
      timeInForce: fullOrder.timeInForce,
      quantity: fullOrder.quantity,
      price: fullOrder.price ?? undefined,
      stopPrice: fullOrder.stopPrice ?? undefined,
      commission: fullOrder.commission ?? undefined,
      expirationDate: fullOrder.expirationDate ? fullOrder.expirationDate.split("T")[0] : undefined,
      comment: fullOrder.comment ?? undefined,
      externalId: fullOrder.externalId ?? undefined,
      status: fullOrder.status,
      executedQuantity: fullOrder.executedQuantity,
      averagePrice: fullOrder.averagePrice ?? undefined,
      executedAt: fullOrder.executedAt ? fullOrder.executedAt.slice(0, 16) : undefined,
    });
    setRowVersion(fullOrder.rowVersion);
  }

  const currentAccount = useMemo(() =>
    fullOrder ? { id: fullOrder.accountId, number: fullOrder.accountNumber } : null,
  [fullOrder]);

  const currentInstrument = useMemo(() =>
    fullOrder ? { id: fullOrder.instrumentId, symbol: fullOrder.instrumentSymbol, name: fullOrder.instrumentName } : null,
  [fullOrder]);

  const set = (key: string, value: unknown) => {
    setForm((f) => ({ ...f, [key]: value }));
    setErrors((prev) => ({ ...prev, [key]: undefined }));
  };

  const handleSubmit = async () => {
    if (!order) return;
    const errs: FieldErrors = {
      accountId: validateRequired(form.accountId),
      instrumentId: validateRequired(form.instrumentId),
      orderDate: validateRequired(form.orderDate),
      quantity: form.quantity > 0 ? undefined : "Quantity is required",
      price: (form.orderType === "Limit" || form.orderType === "StopLimit") && !form.price
        ? "Price is required for Limit/StopLimit orders" : undefined,
      stopPrice: (form.orderType === "Stop" || form.orderType === "StopLimit") && !form.stopPrice
        ? "Stop Price is required for Stop/StopLimit orders" : undefined,
      expirationDate: form.timeInForce === "GTD" && !form.expirationDate
        ? "Expiration Date is required for GTD orders" : undefined,
    };
    if (Object.values(errs).some(Boolean)) { setErrors(errs); return; }
    try {
      await update.mutateAsync({
        id: order.id,
        accountId: form.accountId,
        instrumentId: form.instrumentId,
        orderDate: form.orderDate,
        status: form.status,
        side: form.side,
        orderType: form.orderType,
        timeInForce: form.timeInForce,
        quantity: form.quantity,
        price: form.price ?? undefined,
        stopPrice: form.stopPrice ?? undefined,
        executedQuantity: form.executedQuantity,
        averagePrice: form.averagePrice ?? undefined,
        commission: form.commission ?? undefined,
        executedAt: form.executedAt || undefined,
        expirationDate: form.expirationDate || undefined,
        comment: form.comment || undefined,
        externalId: form.externalId || undefined,
        rowVersion,
      });
      onClose();
    } catch { /* handled by MutationCache */ }
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
      <DialogTitle>Edit Trade Order: {fullOrder?.orderNumber}</DialogTitle>
      <DialogContent sx={{ display: "flex", flexDirection: "column", gap: 2, pt: "8px !important" }}>
        <TradeOrderFormFields form={form} set={set} errors={errors} isEdit currentAccount={currentAccount} currentInstrument={currentInstrument} />
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit} disabled={update.isPending}>Save</Button>
      </DialogActions>
    </Dialog>
  );
}
