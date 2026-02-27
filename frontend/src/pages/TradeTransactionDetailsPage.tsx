import { useState, useMemo } from "react";
import { useParams, Link as RouterLink } from "react-router-dom";
import {
  Box, Button, Card, CardContent, Chip, CircularProgress, Tooltip, Typography, Link,
} from "@mui/material";
import EditIcon from "@mui/icons-material/Edit";
import HistoryIcon from "@mui/icons-material/History";
import { useTradeTransaction } from "../api/hooks";
import { useHasPermission } from "../auth/usePermission";
import { EditTradeTransactionDialog } from "./TradeTransactionDialogs";
import { EntityHistoryDialog } from "../components/EntityHistoryDialog";
import { PageContainer } from "../components/PageContainer";
import { DetailField } from "../components/DetailField";
import { TRANSACTION_STATUS_DESCRIPTIONS } from "../utils/transactionConstants";
import type { TransactionStatus, TradeSide } from "../api/types";

const STATUS_COLORS: Record<TransactionStatus, "success" | "error" | "default" | "warning" | "info" | "primary"> = {
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

export function TradeTransactionDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const { data: transaction, isLoading } = useTradeTransaction(id ?? "");
  const canUpdate = useHasPermission("transactions.update");
  const canAudit = useHasPermission("audit.read");
  const [editOpen, setEditOpen] = useState(false);
  const [historyOpen, setHistoryOpen] = useState(false);

  const breadcrumbs = useMemo(() => [
    { label: "Trade Transactions", to: "/trade-transactions" },
    { label: transaction?.transactionNumber ?? "" },
  ], [transaction?.transactionNumber]);

  if (isLoading) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", mt: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!transaction) {
    return (
      <Box sx={{ p: 3 }}>
        <Typography>Trade transaction not found.</Typography>
        <Typography sx={{ mt: 1 }}>
          <Link component={RouterLink} to="/trade-transactions">Return to Trade Transactions list</Link>
        </Typography>
      </Box>
    );
  }

  return (
    <PageContainer
      title={`Trade Transaction: ${transaction.transactionNumber}`}
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
      {/* Transaction Info */}
      <Card>
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Transaction Info</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <DetailField label="Transaction Number" value={transaction.transactionNumber} />
            <DetailField label="Status" value={<Tooltip title={TRANSACTION_STATUS_DESCRIPTIONS[transaction.status]} arrow><Chip label={transaction.status} color={STATUS_COLORS[transaction.status] ?? "default"} size="small" /></Tooltip>} />
            <DetailField label="Transaction Date" value={new Date(transaction.transactionDate).toLocaleDateString()} />
            <DetailField label="Order" value={
              transaction.orderId && transaction.orderNumber
                ? <Link component={RouterLink} to={`/trade-orders/${transaction.orderId}`}>{transaction.orderNumber}</Link>
                : null
            } />
            <DetailField label="Side" value={<Chip label={transaction.side} color={SIDE_COLORS[transaction.side] ?? "default"} size="small" />} />
          </Box>
        </CardContent>
      </Card>

      {/* Execution */}
      <Card>
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Execution</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <DetailField label="Instrument" value={
              <Link component={RouterLink} to={`/instruments/${transaction.instrumentId}`}>
                {transaction.instrumentSymbol} — {transaction.instrumentName}
              </Link>
            } />
            <DetailField label="Quantity" value={transaction.quantity} />
            <DetailField label="Price" value={transaction.price} />
            <DetailField label="Commission" value={transaction.commission} />
            <DetailField label="Settlement Date" value={transaction.settlementDate ? new Date(transaction.settlementDate).toLocaleDateString() : null} />
            <DetailField label="Venue" value={transaction.venue} />
          </Box>
        </CardContent>
      </Card>

      {/* Details */}
      <Card>
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Details</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <DetailField label="External ID" value={transaction.externalId} />
            <DetailField label="Created" value={new Date(transaction.createdAt).toLocaleString()} />
          </Box>
          {transaction.comment && (
            <Box sx={{ mt: 2 }}>
              <Typography variant="caption" color="text.secondary">Comment</Typography>
              <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>{transaction.comment}</Typography>
            </Box>
          )}
        </CardContent>
      </Card>

      <EditTradeTransactionDialog
        onClose={() => setEditOpen(false)}
        transaction={editOpen && transaction ? { id: transaction.id } : null}
      />
      <EntityHistoryDialog entityType="Transaction" entityId={transaction.id} open={historyOpen} onClose={() => setHistoryOpen(false)} />
    </PageContainer>
  );
}
