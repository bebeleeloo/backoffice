import { useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  Box, Button, Card, CardContent, Chip, CircularProgress, Divider,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  Typography, Paper,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import EditIcon from "@mui/icons-material/Edit";
import { useClient } from "../api/hooks";
import { useHasPermission } from "../auth/usePermission";
import { EditClientDialog } from "./ClientDialogs";
import { PageContainer } from "../components/PageContainer";

const STATUS_COLORS: Record<string, "success" | "error" | "warning" | "default"> = {
  Active: "success", Blocked: "error", PendingKyc: "warning",
};
const KYC_COLORS: Record<string, "success" | "error" | "warning" | "info" | "default"> = {
  Approved: "success", Rejected: "error", InProgress: "info", NotStarted: "default",
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

export function ClientDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: client, isLoading } = useClient(id ?? "");
  const canUpdate = useHasPermission("clients.update");
  const [editOpen, setEditOpen] = useState(false);

  if (isLoading) {
    return (
      <Box sx={{ display: "flex", justifyContent: "center", mt: 8 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (!client) {
    return (
      <Box sx={{ p: 3 }}>
        <Typography>Client not found.</Typography>
        <Button startIcon={<ArrowBackIcon />} onClick={() => navigate("/clients")} sx={{ mt: 1 }}>
          Back to Clients
        </Button>
      </Box>
    );
  }

  const displayName = client.clientType === "Corporate"
    ? client.companyName ?? ""
    : [client.firstName, client.middleName, client.lastName].filter(Boolean).join(" ");

  return (
    <PageContainer
      title={displayName || client.email}
      actions={
        <Box sx={{ display: "flex", gap: 1 }}>
          <Button startIcon={<ArrowBackIcon />} onClick={() => navigate("/clients")}>Back</Button>
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
            <Field label="Type" value={<Chip label={client.clientType} size="small" variant="outlined" />} />
            <Field label="Status" value={<Chip label={client.status} color={STATUS_COLORS[client.status] ?? "default"} size="small" />} />
            <Field label="Email" value={client.email} />
            <Field label="Phone" value={client.phone} />
            <Field label="External ID" value={client.externalId} />
            <Field label="Preferred Language" value={client.preferredLanguage} />
            <Field label="Time Zone" value={client.timeZone} />
            <Field label="Created" value={new Date(client.createdAt).toLocaleString()} />
          </Box>
        </CardContent>
      </Card>

      {/* Personal / Corporate */}
      <Card variant="outlined">
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>
            {client.clientType === "Corporate" ? "Corporate Info" : "Personal Data"}
          </Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            {client.clientType === "Individual" ? (
              <>
                <Field label="First Name" value={client.firstName} />
                <Field label="Last Name" value={client.lastName} />
                <Field label="Middle Name" value={client.middleName} />
                <Field label="Date of Birth" value={client.dateOfBirth} />
                <Field label="Gender" value={client.gender} />
                <Field label="Marital Status" value={client.maritalStatus} />
                <Field label="Education" value={client.education} />
                <Field label="SSN" value={client.ssn} />
                <Field label="Passport Number" value={client.passportNumber} />
                <Field label="Driver License" value={client.driverLicenseNumber} />
              </>
            ) : (
              <>
                <Field label="Company Name" value={client.companyName} />
                <Field label="Registration Number" value={client.registrationNumber} />
                <Field label="Tax ID" value={client.taxId} />
              </>
            )}
          </Box>
        </CardContent>
      </Card>

      {/* KYC & Compliance */}
      <Card variant="outlined">
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>KYC & Compliance</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <Field label="KYC Status" value={<Chip label={client.kycStatus} color={KYC_COLORS[client.kycStatus] ?? "default"} size="small" />} />
            <Field label="KYC Reviewed At" value={client.kycReviewedAtUtc ? new Date(client.kycReviewedAtUtc).toLocaleString() : null} />
            <Field label="Risk Level" value={client.riskLevel} />
            <Field label="PEP Status" value={client.pepStatus ? "Yes" : "No"} />
            <Field label="Residence Country" value={client.residenceCountryName ? `${client.residenceCountryFlagEmoji ?? ""} ${client.residenceCountryName}` : null} />
            <Field label="Citizenship Country" value={client.citizenshipCountryName ? `${client.citizenshipCountryFlagEmoji ?? ""} ${client.citizenshipCountryName}` : null} />
          </Box>
        </CardContent>
      </Card>

      {/* Addresses */}
      {client.addresses.length > 0 && (
        <Card variant="outlined">
          <CardContent>
            <Typography variant="subtitle1" gutterBottom>Addresses</Typography>
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Type</TableCell>
                    <TableCell>Line 1</TableCell>
                    <TableCell>Line 2</TableCell>
                    <TableCell>City</TableCell>
                    <TableCell>State</TableCell>
                    <TableCell>Postal Code</TableCell>
                    <TableCell>Country</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {client.addresses.map((a) => (
                    <TableRow key={a.id}>
                      <TableCell>{a.type}</TableCell>
                      <TableCell>{a.line1}</TableCell>
                      <TableCell>{a.line2}</TableCell>
                      <TableCell>{a.city}</TableCell>
                      <TableCell>{a.state}</TableCell>
                      <TableCell>{a.postalCode}</TableCell>
                      <TableCell>{a.countryFlagEmoji} {a.countryName}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </CardContent>
        </Card>
      )}

      {/* Investment Profile */}
      {client.investmentProfile && (
        <Card variant="outlined">
          <CardContent>
            <Typography variant="subtitle1" gutterBottom>Investment Profile</Typography>
            <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
              <Field label="Objective" value={client.investmentProfile.objective} />
              <Field label="Risk Tolerance" value={client.investmentProfile.riskTolerance} />
              <Field label="Liquidity Needs" value={client.investmentProfile.liquidityNeeds} />
              <Field label="Time Horizon" value={client.investmentProfile.timeHorizon} />
              <Field label="Knowledge" value={client.investmentProfile.knowledge} />
              <Field label="Experience" value={client.investmentProfile.experience} />
            </Box>
            {client.investmentProfile.notes && (
              <>
                <Divider sx={{ my: 1 }} />
                <Typography variant="caption" color="text.secondary">Notes</Typography>
                <Typography variant="body2" sx={{ whiteSpace: "pre-wrap" }}>
                  {client.investmentProfile.notes}
                </Typography>
              </>
            )}
          </CardContent>
        </Card>
      )}

      <EditClientDialog open={editOpen} onClose={() => setEditOpen(false)} clientId={client.id} />
    </PageContainer>
  );
}
