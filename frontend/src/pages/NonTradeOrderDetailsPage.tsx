import { useState, useMemo } from "react";
import { useParams, useNavigate, Link as RouterLink } from "react-router-dom";
import {
  Box, Button, Card, CardContent, Chip, CircularProgress, Tooltip, Typography, Link,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Paper,
} from "@mui/material";
import EditIcon from "@mui/icons-material/Edit";
import HistoryIcon from "@mui/icons-material/History";
import AddIcon from "@mui/icons-material/Add";
import { useNonTradeOrder, useNonTradeTransactionsByOrder } from "../api/hooks";
import { useHasPermission } from "../auth/usePermission";
import { EditNonTradeOrderDialog } from "./NonTradeOrderDialogs";
import { CreateNonTradeTransactionDialog } from "./NonTradeTransactionDialogs";
import { EntityHistoryDialog } from "../components/EntityHistoryDialog";
import { PageContainer } from "../components/PageContainer";
import { DetailField } from "../components/DetailField";
import { STATUS_DESCRIPTIONS } from "../utils/orderConstants";
import type { OrderStatus, NonTradeOrderType, TransactionStatus } from "../api/types";

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

export function NonTradeOrderDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: order, isLoading } = useNonTradeOrder(id ?? "");
  const canUpdate = useHasPermission("orders.update");
  const canAudit = useHasPermission("audit.read");
  const canViewTransactions = useHasPermission("transactions.read");
  const canCreateTransactions = useHasPermission("transactions.create");
  const [editOpen, setEditOpen] = useState(false);
  const [historyOpen, setHistoryOpen] = useState(false);
  const [createTxnOpen, setCreateTxnOpen] = useState(false);
  const { data: transactions } = useNonTradeTransactionsByOrder(canViewTransactions && order ? order.id : "");

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
        <Typography sx={{ mt: 1 }}>
          <Link component={RouterLink} to="/non-trade-orders">Return to Non-Trade Orders list</Link>
        </Typography>
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
      <Card>
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
            <DetailField label="Type" value={<Chip label={order.nonTradeType} color={TYPE_COLORS[order.nonTradeType] ?? "default"} size="small" />} />
          </Box>
        </CardContent>
      </Card>

      {/* Financial */}
      <Card>
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Financial</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <DetailField label="Amount" value={order.amount} />
            <DetailField label="Currency" value={order.currencyCode} />
            <DetailField label="Instrument" value={order.instrumentId ? (
              <Link component={RouterLink} to={`/instruments/${order.instrumentId}`}>
                {order.instrumentSymbol} — {order.instrumentName}
              </Link>
            ) : null} />
          </Box>
        </CardContent>
      </Card>

      {/* Details */}
      <Card>
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Details</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <DetailField label="Reference Number" value={order.referenceNumber} />
            <DetailField label="Description" value={order.description} />
            <DetailField label="Processed At" value={order.processedAt ? new Date(order.processedAt).toLocaleString() : null} />
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
        <Card>
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
                      <TableCell align="right">Amount</TableCell>
                      <TableCell>Currency</TableCell>
                      <TableCell>Reference</TableCell>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {transactions.map((txn) => (
                      <TableRow
                        key={txn.id}
                        hover
                        sx={{ cursor: "pointer" }}
                        onClick={() => navigate(`/non-trade-transactions/${txn.id}`)}
                      >
                        <TableCell>{txn.transactionNumber}</TableCell>
                        <TableCell><Chip label={txn.status} size="small" color={TXN_STATUS_COLORS[txn.status] ?? "default"} /></TableCell>
                        <TableCell>{new Date(txn.transactionDate).toLocaleDateString()}</TableCell>
                        <TableCell align="right">{txn.amount.toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 })}</TableCell>
                        <TableCell>{txn.currencyCode}</TableCell>
                        <TableCell>{txn.referenceNumber ?? "—"}</TableCell>
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

      <EditNonTradeOrderDialog
        onClose={() => setEditOpen(false)}
        order={editOpen && order ? { id: order.id } : null}
      />
      <CreateNonTradeTransactionDialog
        open={createTxnOpen}
        onClose={() => setCreateTxnOpen(false)}
        currentOrder={order ? { id: order.id, orderNumber: order.orderNumber } as import("../api/types").NonTradeOrderListItemDto : undefined}
      />
      <EntityHistoryDialog entityType="Order" entityId={order.id} open={historyOpen} onClose={() => setHistoryOpen(false)} />
    </PageContainer>
  );
}
