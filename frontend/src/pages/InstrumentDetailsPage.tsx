import { useState, useMemo } from "react";
import { useParams, Link as RouterLink } from "react-router-dom";
import {
  Box, Button, Card, CardContent, Chip, CircularProgress, Link, Typography,
} from "@mui/material";
import EditIcon from "@mui/icons-material/Edit";
import HistoryIcon from "@mui/icons-material/History";
import { useInstrument } from "../api/hooks";
import { useHasPermission } from "../auth/usePermission";
import { EditInstrumentDialog } from "./InstrumentDialogs";
import { EntityHistoryDialog } from "../components/EntityHistoryDialog";
import { PageContainer } from "../components/PageContainer";
import type { InstrumentStatus } from "../api/types";

const STATUS_COLORS: Record<InstrumentStatus, "success" | "error" | "default" | "warning"> = {
  Active: "success", Inactive: "default", Delisted: "error", Suspended: "warning",
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

export function InstrumentDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const { data: instrument, isLoading } = useInstrument(id ?? "");
  const canUpdate = useHasPermission("instruments.update");
  const canAudit = useHasPermission("audit.read");
  const [editOpen, setEditOpen] = useState(false);
  const [historyOpen, setHistoryOpen] = useState(false);

  const breadcrumbs = useMemo(() => [
    { label: "Instruments", to: "/instruments" },
    { label: instrument?.symbol ?? "" },
  ], [instrument?.symbol]);

  if (isLoading) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", mt: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!instrument) {
    return (
      <Box sx={{ p: 3 }}>
        <Typography>Instrument not found.</Typography>
        <Typography sx={{ mt: 1 }}>
          <Link component={RouterLink} to="/instruments">Return to Instruments list</Link>
        </Typography>
      </Box>
    );
  }

  return (
    <PageContainer
      title={`${instrument.symbol} — ${instrument.name}`}
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
      {/* General */}
      <Card>
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>General</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <Field label="Symbol" value={instrument.symbol} />
            <Field label="Name" value={instrument.name} />
            <Field label="ISIN" value={instrument.isin} />
            <Field label="CUSIP" value={instrument.cusip} />
            <Field label="Type" value={<Chip label={instrument.type} size="small" variant="outlined" />} />
            <Field label="Asset Class" value={instrument.assetClass} />
            <Field label="Status" value={<Chip label={instrument.status} color={STATUS_COLORS[instrument.status] ?? "default"} size="small" />} />
            <Field label="Sector" value={instrument.sector} />
          </Box>
        </CardContent>
      </Card>

      {/* Market */}
      <Card>
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Market</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <Field label="Exchange" value={instrument.exchangeCode ? `${instrument.exchangeCode} — ${instrument.exchangeName}` : null} />
            <Field label="Currency" value={instrument.currencyCode} />
            <Field label="Country" value={
              instrument.countryFlagEmoji && instrument.countryName
                ? `${instrument.countryFlagEmoji} ${instrument.countryName}`
                : instrument.countryName
            } />
            <Field label="Lot Size" value={instrument.lotSize} />
            <Field label="Tick Size" value={instrument.tickSize} />
            <Field label="Margin Requirement" value={instrument.marginRequirement != null ? `${instrument.marginRequirement}%` : null} />
            <Field label="Margin Eligible" value={instrument.isMarginEligible ? "Yes" : "No"} />
          </Box>
        </CardContent>
      </Card>

      {/* Dates */}
      <Card>
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Dates</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <Field label="Listing Date" value={instrument.listingDate ? new Date(instrument.listingDate).toLocaleDateString() : null} />
            <Field label="Delisting Date" value={instrument.delistingDate ? new Date(instrument.delistingDate).toLocaleDateString() : null} />
            <Field label="Expiration Date" value={instrument.expirationDate ? new Date(instrument.expirationDate).toLocaleDateString() : null} />
            <Field label="Created" value={new Date(instrument.createdAt).toLocaleString()} />
          </Box>
        </CardContent>
      </Card>

      {/* Additional */}
      <Card>
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Additional</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <Field label="Issuer" value={instrument.issuerName} />
            <Field label="External ID" value={instrument.externalId} />
          </Box>
          {instrument.description && (
            <Box sx={{ mt: 2 }}>
              <Typography variant="caption" color="text.secondary">Description</Typography>
              <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>{instrument.description}</Typography>
            </Box>
          )}
        </CardContent>
      </Card>

      <EditInstrumentDialog
        open={editOpen}
        onClose={() => setEditOpen(false)}
        instrument={instrument ? { id: instrument.id, symbol: instrument.symbol } : null}
      />
      <EntityHistoryDialog entityType="Instrument" entityId={instrument.id} open={historyOpen} onClose={() => setHistoryOpen(false)} />
    </PageContainer>
  );
}
