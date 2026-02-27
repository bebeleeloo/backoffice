import { useState, useMemo } from "react";
import { useParams, useNavigate, Link as RouterLink } from "react-router-dom";
import {
  Box, Button, Card, CardContent, Chip, CircularProgress, Tooltip, Typography, Link,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import EditIcon from "@mui/icons-material/Edit";
import HistoryIcon from "@mui/icons-material/History";
import { useNonTradeTransaction } from "../api/hooks";
import { useHasPermission } from "../auth/usePermission";
import { EditNonTradeTransactionDialog } from "./NonTradeTransactionDialogs";
import { EntityHistoryDialog } from "../components/EntityHistoryDialog";
import { PageContainer } from "../components/PageContainer";
import { DetailField } from "../components/DetailField";
import { TRANSACTION_STATUS_DESCRIPTIONS } from "../utils/transactionConstants";
import type { TransactionStatus } from "../api/types";

const STATUS_COLORS: Record<TransactionStatus, "success" | "error" | "default" | "warning" | "info" | "primary"> = {
  Pending: "warning",
  Settled: "success",
  Failed: "error",
  Cancelled: "default",
};

export function NonTradeTransactionDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: transaction, isLoading } = useNonTradeTransaction(id ?? "");
  const canUpdate = useHasPermission("transactions.update");
  const canAudit = useHasPermission("audit.read");
  const [editOpen, setEditOpen] = useState(false);
  const [historyOpen, setHistoryOpen] = useState(false);

  const breadcrumbs = useMemo(() => [
    { label: "Non-Trade Transactions", to: "/non-trade-transactions" },
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
        <Typography>Non-trade transaction not found.</Typography>
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate("/non-trade-transactions")} sx={{ mt: 1 }}>
          Back to Non-Trade Transactions
        </Button>
      </Box>
    );
  }

  return (
    <PageContainer
      title={`Non-Trade Transaction: ${transaction.transactionNumber}`}
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
      <Card variant="outlined">
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Transaction Info</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <DetailField label="Transaction Number" value={transaction.transactionNumber} />
            <DetailField label="Status" value={<Tooltip title={TRANSACTION_STATUS_DESCRIPTIONS[transaction.status]} arrow><Chip label={transaction.status} color={STATUS_COLORS[transaction.status] ?? "default"} size="small" /></Tooltip>} />
            <DetailField label="Transaction Date" value={new Date(transaction.transactionDate).toLocaleDateString()} />
            <DetailField label="Order" value={
              transaction.orderId && transaction.orderNumber
                ? <Link component={RouterLink} to={`/non-trade-orders/${transaction.orderId}`}>{transaction.orderNumber}</Link>
                : null
            } />
          </Box>
        </CardContent>
      </Card>

      {/* Financial */}
      <Card variant="outlined">
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Financial</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <DetailField label="Amount" value={transaction.amount} />
            <DetailField label="Currency" value={transaction.currencyCode} />
            <DetailField label="Instrument" value={transaction.instrumentId ? (
              <Link component={RouterLink} to={`/instruments/${transaction.instrumentId}`}>
                {transaction.instrumentSymbol} — {transaction.instrumentName}
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
            <DetailField label="Reference Number" value={transaction.referenceNumber} />
            <DetailField label="Description" value={transaction.description} />
            <DetailField label="Processed At" value={transaction.processedAt ? new Date(transaction.processedAt).toLocaleString() : null} />
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

      <EditNonTradeTransactionDialog
        onClose={() => setEditOpen(false)}
        transaction={editOpen && transaction ? { id: transaction.id } : null}
      />
      <EntityHistoryDialog entityType="Transaction" entityId={transaction.id} open={historyOpen} onClose={() => setHistoryOpen(false)} />
    </PageContainer>
  );
}
