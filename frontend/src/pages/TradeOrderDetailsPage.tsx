import { useState, useMemo } from "react";
import { useParams, useNavigate, Link as RouterLink } from "react-router-dom";
import {
  Box, Button, Card, CardContent, Chip, CircularProgress, Tooltip, Typography, Link,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import EditIcon from "@mui/icons-material/Edit";
import HistoryIcon from "@mui/icons-material/History";
import AddIcon from "@mui/icons-material/Add";
import { useTradeOrder, useTradeTransactionsByOrder } from "../api/hooks";
import { useHasPermission } from "../auth/usePermission";
import { EditTradeOrderDialog } from "./TradeOrderDialogs";
import { CreateTradeTransactionDialog } from "./TradeTransactionDialogs";
import { EntityHistoryDialog } from "../components/EntityHistoryDialog";
import { PageContainer } from "../components/PageContainer";
import { DetailField } from "../components/DetailField";
import { STATUS_DESCRIPTIONS } from "../utils/orderConstants";
import type { OrderStatus, TradeSide, TransactionStatus } from "../api/types";

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

const TXN_STATUS_COLORS: Record<TransactionStatus, "success" | "error" | "default" | "warning"> = {
  Pending: "warning",
  Settled: "success",
  Failed: "error",
  Cancelled: "default",
};

const SIDE_COLORS: Record<TradeSide, "success" | "error" | "warning" | "info"> = {
  Buy: "success",
  Sell: "error",
  ShortSell: "warning",
  BuyToCover: "info",
};

export function TradeOrderDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: order, isLoading } = useTradeOrder(id ?? "");
  const canUpdate = useHasPermission("orders.update");
  const canAudit = useHasPermission("audit.read");
  const canViewTransactions = useHasPermission("transactions.read");
  const canCreateTransactions = useHasPermission("transactions.create");
  const [editOpen, setEditOpen] = useState(false);
  const [historyOpen, setHistoryOpen] = useState(false);
  const [createTxnOpen, setCreateTxnOpen] = useState(false);
  const { data: transactions } = useTradeTransactionsByOrder(canViewTransactions && order ? order.id : "");

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
            <DetailField label="Order Number" value={order.orderNumber} />
            <DetailField label="Status" value={<Tooltip title={STATUS_DESCRIPTIONS[order.status]} arrow><Chip label={order.status} color={STATUS_COLORS[order.status] ?? "default"} size="small" /></Tooltip>} />
            <DetailField label="Order Date" value={new Date(order.orderDate).toLocaleDateString()} />
            <DetailField label="Account" value={
              <Link component={RouterLink} to={`/accounts/${order.accountId}`}>
                {order.accountNumber}
              </Link>
            } />
            <DetailField label="Side" value={<Chip label={order.side} color={SIDE_COLORS[order.side] ?? "default"} size="small" />} />
            <DetailField label="Order Type" value={<Chip label={order.orderType} size="small" variant="outlined" />} />
            <DetailField label="Time In Force" value={order.timeInForce} />
          </Box>
        </CardContent>
      </Card>

      {/* Execution */}
      <Card variant="outlined">
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Execution</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <DetailField label="Quantity" value={order.quantity} />
            <DetailField label="Price" value={order.price} />
            <DetailField label="Stop Price" value={order.stopPrice} />
            <DetailField label="Executed Quantity" value={order.executedQuantity} />
            <DetailField label="Average Price" value={order.averagePrice} />
            <DetailField label="Commission" value={order.commission} />
            <DetailField label="Executed At" value={order.executedAt ? new Date(order.executedAt).toLocaleString() : null} />
            <DetailField label="Expiration Date" value={order.expirationDate ? new Date(order.expirationDate).toLocaleDateString() : null} />
          </Box>
        </CardContent>
      </Card>

      {/* Details */}
      <Card variant="outlined">
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Details</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <DetailField label="Instrument" value={
              <Link component={RouterLink} to={`/instruments/${order.instrumentId}`}>
                {order.instrumentSymbol} — {order.instrumentName}
              </Link>
            } />
            <DetailField label="External ID" value={order.externalId} />
            <DetailField label="Created" value={new Date(order.createdAt).toLocaleString()} />
          </Box>
          {order.comment && (
            <Box sx={{ mt: 2 }}>
              <Typography variant="caption" color="text.secondary">Comment</Typography>
              <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>{order.comment}</Typography>
            </Box>
          )}
        </CardContent>
      </Card>

      {/* Transactions */}
      {canViewTransactions && (
        <Card variant="outlined">
          <CardContent>
            <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center", mb: 1 }}>
              <Typography variant="subtitle1">Transactions</Typography>
              {canCreateTransactions && (
                <Button size="small" startIcon={<AddIcon />} onClick={() => setCreateTxnOpen(true)}>
                  Create Transaction
                </Button>
              )}
            </Box>
            {transactions && transactions.length > 0 ? (
              <TableContainer component={Paper} variant="outlined">
                <Table size="small">
                  <TableHead>
                    <TableRow>
                      <TableCell>Number</TableCell>
                      <TableCell>Status</TableCell>
                      <TableCell>Date</TableCell>
                      <TableCell>Side</TableCell>
                      <TableCell align="right">Quantity</TableCell>
                      <TableCell align="right">Price</TableCell>
                      <TableCell align="right">Commission</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {transactions.map((txn) => (
                      <TableRow
                        key={txn.id}
                        hover
                        sx={{ cursor: "pointer" }}
                        onClick={() => navigate(`/trade-transactions/${txn.id}`)}
                      >
                        <TableCell>{txn.transactionNumber}</TableCell>
                        <TableCell><Chip label={txn.status} size="small" color={TXN_STATUS_COLORS[txn.status] ?? "default"} /></TableCell>
                        <TableCell>{new Date(txn.transactionDate).toLocaleDateString()}</TableCell>
                        <TableCell><Chip label={txn.side} size="small" color={SIDE_COLORS[txn.side] ?? "default"} /></TableCell>
                        <TableCell align="right">{txn.quantity.toLocaleString()}</TableCell>
                        <TableCell align="right">{txn.price.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</TableCell>
                        <TableCell align="right">{txn.commission != null ? txn.commission.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : "—"}</TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              </TableContainer>
            ) : (
              <Typography variant="body2" color="text.secondary">No transactions</Typography>
            )}
          </CardContent>
        </Card>
      )}

      <EditTradeOrderDialog
        onClose={() => setEditOpen(false)}
        order={editOpen && order ? { id: order.id } : null}
      />
      <CreateTradeTransactionDialog
        open={createTxnOpen}
        onClose={() => setCreateTxnOpen(false)}
        currentOrder={order ? { id: order.id, orderNumber: order.orderNumber } as import("../api/types").TradeOrderListItemDto : undefined}
      />
      <EntityHistoryDialog entityType="Order" entityId={order.id} open={historyOpen} onClose={() => setHistoryOpen(false)} />
    </PageContainer>
  );
}
