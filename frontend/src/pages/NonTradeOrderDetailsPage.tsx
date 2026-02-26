import { useState, useMemo } from "react";
import { useParams, useNavigate, Link as RouterLink } from "react-router-dom";
import {
  Box, Button, Card, CardContent, Chip, CircularProgress, Typography, Link,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import EditIcon from "@mui/icons-material/Edit";
import HistoryIcon from "@mui/icons-material/History";
import { useNonTradeOrder } from "../api/hooks";
import { useHasPermission } from "../auth/usePermission";
import { EditNonTradeOrderDialog } from "./NonTradeOrderDialogs";
import { EntityHistoryDialog } from "../components/EntityHistoryDialog";
import { PageContainer } from "../components/PageContainer";
import type { OrderStatus, NonTradeOrderType } from "../api/types";

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

const TYPE_COLORS: Record<NonTradeOrderType, "success" | "error" | "default" | "warning" | "info" | "primary"> = {
  Deposit: "success",
  Withdrawal: "error",
  Dividend: "primary",
  CorporateAction: "info",
  Fee: "warning",
  Interest: "info",
  Transfer: "default",
  Adjustment: "warning",
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

export function NonTradeOrderDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: order, isLoading } = useNonTradeOrder(id ?? "");
  const canUpdate = useHasPermission("orders.update");
  const canAudit = useHasPermission("audit.read");
  const [editOpen, setEditOpen] = useState(false);
  const [historyOpen, setHistoryOpen] = useState(false);

  const breadcrumbs = useMemo(() => [
    { label: "Non-Trade Orders", to: "/non-trade-orders" },
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
        <Typography>Non-trade order not found.</Typography>
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate("/non-trade-orders")} sx={{ mt: 1 }}>
          Back to Non-Trade Orders
        </Button>
      </Box>
    );
  }

  return (
    <PageContainer
      title={`Non-Trade Order: ${order.orderNumber}`}
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
            <Field label="Type" value={<Chip label={order.nonTradeType} color={TYPE_COLORS[order.nonTradeType] ?? "default"} size="small" />} />
          </Box>
        </CardContent>
      </Card>

      {/* Financial */}
      <Card variant="outlined">
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Financial</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <Field label="Amount" value={order.amount} />
            <Field label="Currency" value={order.currencyCode} />
            <Field label="Instrument" value={order.instrumentId ? (
              <Link component={RouterLink} to={`/instruments/${order.instrumentId}`}>
                {order.instrumentSymbol} — {order.instrumentName}
              </Link>
            ) : null} />
          </Box>
        </CardContent>
      </Card>

      {/* Details */}
      <Card variant="outlined">
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Details</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <Field label="Reference Number" value={order.referenceNumber} />
            <Field label="Description" value={order.description} />
            <Field label="Processed At" value={order.processedAt ? new Date(order.processedAt).toLocaleString() : null} />
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

      <EditNonTradeOrderDialog
        onClose={() => setEditOpen(false)}
        order={editOpen && order ? { id: order.id } : null}
      />
      <EntityHistoryDialog entityType="Order" entityId={order.id} open={historyOpen} onClose={() => setHistoryOpen(false)} />
    </PageContainer>
  );
}
