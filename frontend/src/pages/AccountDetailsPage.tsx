import { useState, useMemo } from "react";
import { useParams, Link as RouterLink } from "react-router-dom";
import {
  Box, Button, Card, CardContent, Chip, CircularProgress,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  Typography, Paper, Link,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import EditIcon from "@mui/icons-material/Edit";
import HistoryIcon from "@mui/icons-material/History";
import { useAccount } from "../api/hooks";
import { useHasPermission } from "../auth/usePermission";
import { EditAccountDialog } from "./AccountDialogs";
import { CreateTradeOrderDialog } from "./TradeOrderDialogs";
import { CreateNonTradeOrderDialog } from "./NonTradeOrderDialogs";
import { EntityHistoryDialog } from "../components/EntityHistoryDialog";
import { PageContainer } from "../components/PageContainer";
import { DetailField } from "../components/DetailField";

const STATUS_COLORS: Record<string, "success" | "error" | "default" | "warning"> = {
  Active: "success", Blocked: "error", Closed: "default", Suspended: "warning",
};

export function AccountDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const { data: account, isLoading } = useAccount(id ?? "");
  const canUpdate = useHasPermission("accounts.update");
  const canAudit = useHasPermission("audit.read");
  const canCreateOrders = useHasPermission("orders.create");
  const [editOpen, setEditOpen] = useState(false);
  const [historyOpen, setHistoryOpen] = useState(false);
  const [createTradeOpen, setCreateTradeOpen] = useState(false);
  const [createNonTradeOpen, setCreateNonTradeOpen] = useState(false);

  const breadcrumbs = useMemo(() => [
    { label: "Accounts", to: "/accounts" },
    { label: account?.number ?? "" },
  ], [account?.number]);

  if (isLoading) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", mt: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!account) {
    return (
      <Box sx={{ p: 3 }}>
        <Typography>Account not found.</Typography>
        <Typography sx={{ mt: 1 }}>
          <Link component={RouterLink} to="/accounts">Return to Accounts list</Link>
        </Typography>
      </Box>
    );
  }

  return (
    <PageContainer
      title={`Account: ${account.number}`}
      breadcrumbs={breadcrumbs}
      actions={
        <Box sx={{ display: "flex", gap: 1 }}>
          {canCreateOrders && (
            <>
              <Button startIcon={<AddIcon />} onClick={() => setCreateTradeOpen(true)}>Trade Order</Button>
              <Button startIcon={<AddIcon />} onClick={() => setCreateNonTradeOpen(true)}>Non-Trade Order</Button>
            </>
          )}
          {canAudit && (
            <Button startIcon={<HistoryIcon />} onClick={() => setHistoryOpen(true)}>History</Button>
          )}
          {canUpdate && (
            <Button variant="contained" startIcon={<EditIcon />} onClick={() => setEditOpen(true)}>Edit</Button>
          )}
        </Box>
      }
    >
      {/* General */}
      <Card>
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>General</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <DetailField label="Number" value={account.number} />
            <DetailField label="Status" value={<Chip label={account.status} color={STATUS_COLORS[account.status] ?? "default"} size="small" />} />
            <DetailField label="Account Type" value={account.accountType} />
            <DetailField label="Margin Type" value={account.marginType} />
            <DetailField label="Option Level" value={account.optionLevel} />
            <DetailField label="Tariff" value={<Chip label={account.tariff} size="small" variant="outlined" />} />
            <DetailField label="Delivery Type" value={account.deliveryType} />
            <DetailField label="Clearer" value={account.clearerName} />
            <DetailField label="Trade Platform" value={account.tradePlatformName} />
            <DetailField label="Opened At" value={account.openedAt ? new Date(account.openedAt).toLocaleDateString() : null} />
            <DetailField label="Closed At" value={account.closedAt ? new Date(account.closedAt).toLocaleDateString() : null} />
            <DetailField label="External ID" value={account.externalId} />
            <DetailField label="Created" value={new Date(account.createdAt).toLocaleString()} />
          </Box>
          {account.comment && (
            <Box sx={{ mt: 2 }}>
              <Typography variant="caption" color="text.secondary">Comment</Typography>
              <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>{account.comment}</Typography>
            </Box>
          )}
        </CardContent>
      </Card>

      {/* Holders */}
      <Card>
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Holders</Typography>
          {account.holders.length === 0 ? (
            <Typography variant="body2" color="text.secondary">No holders assigned.</Typography>
          ) : (
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Client</TableCell>
                    <TableCell>Role</TableCell>
                    <TableCell>Primary</TableCell>
                    <TableCell>Added At</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {account.holders.map((h) => (
                    <TableRow key={`${h.clientId}-${h.role}`}>
                      <TableCell>
                        <Link component={RouterLink} to={`/clients/${h.clientId}`}>
                          {h.clientDisplayName}
                        </Link>
                      </TableCell>
                      <TableCell>{h.role}</TableCell>
                      <TableCell>{h.isPrimary ? "Yes" : "No"}</TableCell>
                      <TableCell>{new Date(h.addedAt).toLocaleString()}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          )}
        </CardContent>
      </Card>

      <EditAccountDialog
        open={editOpen}
        onClose={() => setEditOpen(false)}
        account={account ? { id: account.id } : null}
      />
      <CreateTradeOrderDialog
        open={createTradeOpen}
        onClose={() => setCreateTradeOpen(false)}
        currentAccount={{ id: account.id, number: account.number }}
      />
      <CreateNonTradeOrderDialog
        open={createNonTradeOpen}
        onClose={() => setCreateNonTradeOpen(false)}
        currentAccount={{ id: account.id, number: account.number }}
      />
      <EntityHistoryDialog entityType="Account" entityId={account.id} open={historyOpen} onClose={() => setHistoryOpen(false)} />
    </PageContainer>
  );
}

