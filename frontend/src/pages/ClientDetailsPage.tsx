import { useState, useMemo } from "react";
import { useParams, useNavigate, Link as RouterLink } from "react-router-dom";
import {
  Box, Button, Card, CardContent, Chip, CircularProgress, Divider,
  Table, TableBody, TableCell, TableContainer, TableHead, TableRow,
  Typography, Paper, Link,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import EditIcon from "@mui/icons-material/Edit";
import HistoryIcon from "@mui/icons-material/History";
import { useClient, useClientAccounts } from "../api/hooks";
import { useHasPermission } from "../auth/usePermission";
import { EditClientDialog } from "./ClientDialogs";
import { EntityHistoryDialog } from "../components/EntityHistoryDialog";
import { PageContainer } from "../components/PageContainer";
import { DetailField } from "../components/DetailField";

const STATUS_COLORS: Record<string, "success" | "error" | "warning" | "default"> = {
  Active: "success", Blocked: "error", PendingKyc: "warning",
};
const KYC_COLORS: Record<string, "success" | "error" | "warning" | "info" | "default"> = {
  Approved: "success", Rejected: "error", InProgress: "info", NotStarted: "default",
};

export function ClientDetailsPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: client, isLoading } = useClient(id ?? "");
  const { data: clientAccounts } = useClientAccounts(id ?? "");
  const canUpdate = useHasPermission("clients.update");
  const canAudit = useHasPermission("audit.read");
  const [editOpen, setEditOpen] = useState(false);
  const [historyOpen, setHistoryOpen] = useState(false);

  const displayName = client?.clientType === "Corporate"
    ? client?.companyName ?? ""
    : [client?.firstName, client?.middleName, client?.lastName].filter(Boolean).join(" ");

  const breadcrumbs = useMemo(() => [
    { label: "Clients", to: "/clients" },
    { label: displayName || client?.email || "" },
  ], [displayName, client?.email]);

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

  return (
    <PageContainer
      title={displayName || client.email}
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
      <Card variant="outlined">
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>General</Typography>
          <Box sx={{ display: "flex", flexWrap: "wrap", gap: 3 }}>
            <DetailField label="Type" value={<Chip label={client.clientType} size="small" variant="outlined" />} />
            <DetailField label="Status" value={<Chip label={client.status} color={STATUS_COLORS[client.status] ?? "default"} size="small" />} />
            <DetailField label="Email" value={client.email} />
            <DetailField label="Phone" value={client.phone} />
            <DetailField label="External ID" value={client.externalId} />
            <DetailField label="Preferred Language" value={client.preferredLanguage} />
            <DetailField label="Time Zone" value={client.timeZone} />
            <DetailField label="Created" value={new Date(client.createdAt).toLocaleString()} />
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
                <DetailField label="First Name" value={client.firstName} />
                <DetailField label="Last Name" value={client.lastName} />
                <DetailField label="Middle Name" value={client.middleName} />
                <DetailField label="Date of Birth" value={client.dateOfBirth ? new Date(client.dateOfBirth + "T00:00:00").toLocaleDateString() : null} />
                <DetailField label="Gender" value={client.gender} />
                <DetailField label="Marital Status" value={client.maritalStatus} />
                <DetailField label="Education" value={client.education} />
                <DetailField label="SSN" value={client.ssn} />
                <DetailField label="Passport Number" value={client.passportNumber} />
                <DetailField label="Driver License" value={client.driverLicenseNumber} />
              </>
            ) : (
              <>
                <DetailField label="Company Name" value={client.companyName} />
                <DetailField label="Registration Number" value={client.registrationNumber} />
                <DetailField label="Tax ID" value={client.taxId} />
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
            <DetailField label="KYC Status" value={<Chip label={client.kycStatus} color={KYC_COLORS[client.kycStatus] ?? "default"} size="small" />} />
            <DetailField label="KYC Reviewed At" value={client.kycReviewedAtUtc ? new Date(client.kycReviewedAtUtc).toLocaleString() : null} />
            <DetailField label="Risk Level" value={client.riskLevel} />
            <DetailField label="PEP Status" value={client.pepStatus ? "Yes" : "No"} />
            <DetailField label="Residence Country" value={client.residenceCountryName ? `${client.residenceCountryFlagEmoji ?? ""} ${client.residenceCountryName}` : null} />
            <DetailField label="Citizenship Country" value={client.citizenshipCountryName ? `${client.citizenshipCountryFlagEmoji ?? ""} ${client.citizenshipCountryName}` : null} />
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
              <DetailField label="Objective" value={client.investmentProfile.objective} />
              <DetailField label="Risk Tolerance" value={client.investmentProfile.riskTolerance} />
              <DetailField label="Liquidity Needs" value={client.investmentProfile.liquidityNeeds} />
              <DetailField label="Time Horizon" value={client.investmentProfile.timeHorizon} />
              <DetailField label="Knowledge" value={client.investmentProfile.knowledge} />
              <DetailField label="Experience" value={client.investmentProfile.experience} />
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

      {/* Accounts */}
      <Card variant="outlined">
        <CardContent>
          <Typography variant="subtitle1" gutterBottom>Accounts</Typography>
          {clientAccounts && clientAccounts.length > 0 ? (
            <TableContainer component={Paper} variant="outlined">
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell>Account Number</TableCell>
                    <TableCell>Status</TableCell>
                    <TableCell>Role</TableCell>
                    <TableCell>Primary</TableCell>
                    <TableCell>Added At</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {clientAccounts.map((a) => (
                    <TableRow key={`${a.accountId}-${a.role}`}>
                      <TableCell>
                        <Link component={RouterLink} to={`/accounts/${a.accountId}`}>
                          {a.accountNumber}
                        </Link>
                      </TableCell>
                      <TableCell>
                        <Chip label={a.accountStatus} size="small" color={
                          a.accountStatus === "Active" ? "success"
                          : a.accountStatus === "Blocked" ? "error"
                          : a.accountStatus === "Suspended" ? "warning"
                          : "default"
                        } />
                      </TableCell>
                      <TableCell>{a.role}</TableCell>
                      <TableCell>{a.isPrimary ? "Yes" : "No"}</TableCell>
                      <TableCell>{new Date(a.addedAt).toLocaleString()}</TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          ) : (
            <Typography variant="body2" color="text.secondary">No accounts.</Typography>
          )}
        </CardContent>
      </Card>

      <EditClientDialog open={editOpen} onClose={() => setEditOpen(false)} clientId={client.id} />
      <EntityHistoryDialog entityType="Client" entityId={client.id} open={historyOpen} onClose={() => setHistoryOpen(false)} />
    </PageContainer>
  );
}
