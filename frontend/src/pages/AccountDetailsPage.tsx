import { useState } from "react";
import { useParams, useNavigate, Link as RouterLink } from "react-router-dom";
import {
  Box, Button, Card, CardContent, Chip, CircularProgress,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  Typography, Paper, Link,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import EditIcon from "@mui/icons-material/Edit";
import { useAccount } from "../api/hooks";
import { useHasPermission } from "../auth/usePermission";
import { EditAccountDialog } from "./AccountDialogs";
import { PageContainer } from "../components/PageContainer";

const STATUS_COLORS: Record<string, "success" | "error" | "default" | "warning"> = {
  Active: "success", Blocked: "error", Closed: "default", Suspended: "warning",
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

export function AccountDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: account, isLoading } = useAccount(id ?? "");
  const canUpdate = useHasPermission("accounts.update");
  const [editOpen, setEditOpen] = useState(false);

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
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate("/accounts")} sx={{ mt: 1 }}>
          Back to Accounts
        </Button>
      </Box>
    );
  }

  return (
    <PageContainer
      title={`Account: ${account.number}`}
      actions={
        <Box sx={{ display: "flex", gap: 1 }}>
          <Button startIcon={<ArrowBackIcon />} onClick={() => navigate("/accounts")}>Back</Button>
          {canUpdate && (
            <Button variant="contained" startIcon={<EditIcon />} onClick={() => setEditOpen(true)}>Edit</Button>
          )}
        </Box>
      }
    >
      {/* General */}
      <Card variant="outlined">
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>General</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <Field label="Number" value={account.number} />
            <Field label="Status" value={<Chip label={account.status} color={STATUS_COLORS[account.status] ?? "default"} size="small" />} />
            <Field label="Account Type" value={account.accountType} />
            <Field label="Margin Type" value={account.marginType} />
            <Field label="Option Level" value={account.optionLevel} />
            <Field label="Tariff" value={<Chip label={account.tariff} size="small" variant="outlined" />} />
            <Field label="Delivery Type" value={account.deliveryType} />
            <Field label="Clearer" value={account.clearerName} />
            <Field label="Trade Platform" value={account.tradePlatformName} />
            <Field label="Opened At" value={account.openedAt ? new Date(account.openedAt).toLocaleDateString() : null} />
            <Field label="Closed At" value={account.closedAt ? new Date(account.closedAt).toLocaleDateString() : null} />
            <Field label="External ID" value={account.externalId} />
            <Field label="Created" value={new Date(account.createdAt).toLocaleString()} />
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
      <Card variant="outlined">
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
                      <TableCell>{new Date(h.addedAt).toLocaleDateString()}</TableCell>
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
        account={account ? { id: account.id, number: account.number } as any : null}
      />
    </PageContainer>
  );
}

