import { useState, useMemo } from "react";
import { useParams, useNavigate, Link as RouterLink } from "react-router-dom";
import {
  Box, Button, Card, CardContent, Chip, CircularProgress, Typography, Link,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import EditIcon from "@mui/icons-material/Edit";
import HistoryIcon from "@mui/icons-material/History";
import { useTradeOrder } from "../api/hooks";
import { useHasPermission } from "../auth/usePermission";
import { EditTradeOrderDialog } from "./TradeOrderDialogs";
import { EntityHistoryDialog } from "../components/EntityHistoryDialog";
import { PageContainer } from "../components/PageContainer";
import type { OrderStatus, TradeSide } from "../api/types";

const STATUS_COLORS: Record<OrderStatus, "success" | "error" | "default" | "warning" | "info" | "primary"> = {
  New: "info",
  PendingApproval: "warning",
  Approved: "primary",
  Rejected: "error",
  InProgress: "info",
  PartiallyFilled: "warning",
  Filled: "success",
  Completed: "success",
  Cancelled: "default",
  Failed: "error",
};

const SIDE_COLORS: Record<TradeSide, "success" | "error" | "warning" | "info"> = {
  Buy: "success",
  Sell: "error",
  ShortSell: "warning",
  BuyToCover: "info",
};

function Field({ label, value }: { label: string; value: React.ReactNode }) {
  if (value === null || value === undefined || value === "") return null;
  return (
    <Box sx={{ minWidth: 180 }}>
      <Typography variant="caption" color="text.secondary">{label}</Typography>
      <Typography variant="body2">{value}</Typography>
    </Box>
  );
}

export function TradeOrderDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: order, isLoading } = useTradeOrder(id ?? "");
  const canUpdate = useHasPermission("orders.update");
  const canAudit = useHasPermission("audit.read");
  const [editOpen, setEditOpen] = useState(false);
  const [historyOpen, setHistoryOpen] = useState(false);

  const breadcrumbs = useMemo(() => [
    { label: "Trade Orders", to: "/trade-orders" },
    { label: order?.orderNumber ?? "" },
  ], [order?.orderNumber]);

  if (isLoading) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", mt: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!order) {
    return (
      <Box sx={{ p: 3 }}>
        <Typography>Trade order not found.</Typography>
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate("/trade-orders")} sx={{ mt: 1 }}>
          Back to Trade Orders
        </Button>
      </Box>
    );
  }

  return (
    <PageContainer
      title={`Trade Order: ${order.orderNumber}`}
      breadcrumbs={breadcrumbs}
      actions={
        <Box sx={{ display: "flex", gap: 1 }}>
          {canAudit && (
            <Button startIcon={<HistoryIcon />} onClick={() => setHistoryOpen(true)}>History</Button>
          )}
          {canUpdate && (
            <Button variant="contained" startIcon={<EditIcon />} onClick={() => setEditOpen(true)}>Edit</Button>
          )}
        </Box>
      }
    >
      {/* Order Info */}
      <Card variant="outlined">
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Order Info</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <Field label="Order Number" value={order.orderNumber} />
            <Field label="Status" value={<Chip label={order.status} color={STATUS_COLORS[order.status] ?? "default"} size="small" />} />
            <Field label="Order Date" value={new Date(order.orderDate).toLocaleDateString()} />
            <Field label="Account" value={
              <Link component={RouterLink} to={`/accounts/${order.accountId}`}>
                {order.accountNumber}
              </Link>
            } />
            <Field label="Side" value={<Chip label={order.side} color={SIDE_COLORS[order.side] ?? "default"} size="small" />} />
            <Field label="Order Type" value={<Chip label={order.orderType} size="small" variant="outlined" />} />
            <Field label="Time In Force" value={order.timeInForce} />
          </Box>
        </CardContent>
      </Card>

      {/* Execution */}
      <Card variant="outlined">
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Execution</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <Field label="Quantity" value={order.quantity} />
            <Field label="Price" value={order.price} />
            <Field label="Stop Price" value={order.stopPrice} />
            <Field label="Executed Quantity" value={order.executedQuantity} />
            <Field label="Average Price" value={order.averagePrice} />
            <Field label="Commission" value={order.commission} />
            <Field label="Executed At" value={order.executedAt ? new Date(order.executedAt).toLocaleString() : null} />
            <Field label="Expiration Date" value={order.expirationDate ? new Date(order.expirationDate).toLocaleDateString() : null} />
          </Box>
        </CardContent>
      </Card>

      {/* Details */}
      <Card variant="outlined">
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Details</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <Field label="Instrument" value={
              <Link component={RouterLink} to={`/instruments/${order.instrumentId}`}>
                {order.instrumentSymbol} — {order.instrumentName}
              </Link>
            } />
            <Field label="External ID" value={order.externalId} />
            <Field label="Created" value={new Date(order.createdAt).toLocaleString()} />
          </Box>
          {order.comment && (
            <Box sx={{ mt: 2 }}>
              <Typography variant="caption" color="text.secondary">Comment</Typography>
              <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>{order.comment}</Typography>
            </Box>
          )}
        </CardContent>
      </Card>

      <EditTradeOrderDialog
        onClose={() => setEditOpen(false)}
        order={editOpen && order ? { id: order.id } : null}
      />
      <EntityHistoryDialog entityType="Order" entityId={order.id} open={historyOpen} onClose={() => setHistoryOpen(false)} />
    </PageContainer>
  );
}
