import { useState } from "react";
import {
  Dialog, DialogTitle, DialogContent, DialogActions, Button,
  Accordion, AccordionSummary, AccordionDetails,
  Typography, Box, Chip, CircularProgress, Pagination, Divider,
} from "@mui/material";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import { useEntityChanges } from "../api/hooks";
import type { OperationDto, EntityChangeGroupDto, FieldChangeDto } from "../api/types";

interface Props {
  entityType: string;
  entityId: string;
  open: boolean;
  onClose: () => void;
}

const CHANGE_TYPE_COLORS: Record<string, "success" | "warning" | "error"> = {
  Created: "success",
  Modified: "warning",
  Deleted: "error",
};

const FIELD_LABELS: Record<string, Record<string, string>> = {
  Client: {
    ClientType: "Client Type", ExternalId: "External ID", Status: "Status",
    Email: "Email", Phone: "Phone", PreferredLanguage: "Language", TimeZone: "Time Zone",
    ResidenceCountryId: "Residence Country", CitizenshipCountryId: "Citizenship Country",
    PepStatus: "PEP Status", RiskLevel: "Risk Level", KycStatus: "KYC Status",
    KycReviewedAtUtc: "KYC Reviewed At",
    FirstName: "First Name", LastName: "Last Name", MiddleName: "Middle Name",
    DateOfBirth: "Date of Birth", Gender: "Gender", MaritalStatus: "Marital Status",
    Education: "Education", Ssn: "SSN", PassportNumber: "Passport",
    DriverLicenseNumber: "Driver License",
    CompanyName: "Company", RegistrationNumber: "Registration No.", TaxId: "Tax ID",
  },
  ClientAddress: {
    Type: "Address Type", Line1: "Line 1", Line2: "Line 2",
    City: "City", State: "State", PostalCode: "Postal Code", CountryId: "Country",
  },
  InvestmentProfile: {
    Objective: "Objective", RiskTolerance: "Risk Tolerance", LiquidityNeeds: "Liquidity",
    TimeHorizon: "Time Horizon", Knowledge: "Knowledge", Experience: "Experience", Notes: "Notes",
  },
  Account: {
    Number: "Account Number", Status: "Status", AccountType: "Account Type",
    MarginType: "Margin Type", OptionLevel: "Option Level", Tariff: "Tariff",
    DeliveryType: "Delivery", ClearerId: "Clearer", TradePlatformId: "Platform",
    OpenedAt: "Opened At", ClosedAt: "Closed At", Comment: "Comment", ExternalId: "External ID",
  },
  AccountHolder: {
    AccountId: "Account", ClientId: "Client", Role: "Role",
    IsPrimary: "Primary", AddedAt: "Added At",
  },
  Instrument: {
    Symbol: "Symbol", Name: "Name", ISIN: "ISIN", CUSIP: "CUSIP",
    Type: "Type", AssetClass: "Asset Class", Status: "Status", Sector: "Sector",
    ExchangeId: "Exchange", CurrencyId: "Currency", CountryId: "Country",
    LotSize: "Lot Size", TickSize: "Tick Size",
    MarginRequirement: "Margin Requirement", IsMarginEligible: "Margin Eligible",
    ListingDate: "Listing Date", DelistingDate: "Delisting Date", ExpirationDate: "Expiration",
    IssuerName: "Issuer", Description: "Description", ExternalId: "External ID",
  },
  User: {
    Username: "Username", Email: "Email", FullName: "Full Name", IsActive: "Active",
  },
  UserRole: {
    UserId: "User", RoleId: "Role", CreatedAt: "Assigned At", CreatedBy: "Assigned By",
  },
  Role: {
    Name: "Name", Description: "Description", IsSystem: "System Role",
  },
  RolePermission: {
    RoleId: "Role", PermissionId: "Permission", CreatedAt: "Assigned At", CreatedBy: "Assigned By",
  },
};

function getFieldLabel(entityType: string | null, fieldName: string): string {
  if (entityType && FIELD_LABELS[entityType]?.[fieldName]) {
    return FIELD_LABELS[entityType][fieldName];
  }
  // Convert PascalCase to Title Case
  return fieldName.replace(/([A-Z])/g, " $1").trim();
}

function getEntityTypeLabel(type: string | null): string {
  if (!type) return "";
  const labels: Record<string, string> = {
    ClientAddress: "Address",
    InvestmentProfile: "Investment Profile",
    AccountHolder: "Account Holder",
    UserRole: "User Role",
    RolePermission: "Permission",
  };
  return labels[type] ?? type;
}

function FieldRow({ field, entityType }: { field: FieldChangeDto; entityType: string | null }) {
  const label = getFieldLabel(entityType, field.fieldName);
  const color = CHANGE_TYPE_COLORS[field.changeType] ?? "default";

  return (
    <Box sx={{ display: "flex", alignItems: "baseline", gap: 1, py: 0.25, pl: 2 }}>
      <Typography variant="body2" color="text.secondary" sx={{ minWidth: 140, flexShrink: 0 }}>
        {label}:
      </Typography>
      {field.changeType === "Created" && (
        <Typography variant="body2" color={`${color}.main`}>{field.newValue ?? "—"}</Typography>
      )}
      {field.changeType === "Deleted" && (
        <Typography variant="body2" sx={{ textDecoration: "line-through" }} color={`${color}.main`}>
          {field.oldValue ?? "—"}
        </Typography>
      )}
      {field.changeType === "Modified" && (
        <Typography variant="body2">
          <Box component="span" sx={{ textDecoration: "line-through", color: "text.secondary" }}>
            {field.oldValue ?? "—"}
          </Box>
          {" → "}
          <Box component="span" color={`${color}.main`}>{field.newValue ?? "—"}</Box>
        </Typography>
      )}
    </Box>
  );
}

function ChangeGroup({ group }: { group: EntityChangeGroupDto }) {
  const entityLabel = group.relatedEntityType
    ? getEntityTypeLabel(group.relatedEntityType)
    : null;
  const displayName = group.relatedEntityDisplayName;
  const color = CHANGE_TYPE_COLORS[group.changeType] ?? "default";

  return (
    <Box sx={{ mb: 1 }}>
      {entityLabel && (
        <Box sx={{ display: "flex", alignItems: "center", gap: 1, mb: 0.5 }}>
          <Typography variant="body2" fontWeight={600}>
            {entityLabel}{displayName ? ` — ${displayName}` : ""}
          </Typography>
          <Chip label={group.changeType} color={color} size="small" variant="outlined" />
        </Box>
      )}
      {group.fields.map((f, i) => (
        <FieldRow key={i} field={f} entityType={group.relatedEntityType ?? undefined ?? null} />
      ))}
    </Box>
  );
}

function OperationItem({ op, entityType }: { op: OperationDto; entityType: string }) {
  const ts = new Date(op.timestamp);
  const dateStr = ts.toLocaleDateString(undefined, { year: "numeric", month: "short", day: "numeric" });
  const timeStr = ts.toLocaleTimeString(undefined, { hour: "2-digit", minute: "2-digit", second: "2-digit" });
  const color = CHANGE_TYPE_COLORS[op.changeType] ?? "default";
  const entityLabel = getEntityTypeLabel(entityType) || entityType;
  const displayName = op.entityDisplayName;

  // Separate root entity changes and related entity changes
  const rootChanges = op.changes.filter((c) => !c.relatedEntityType);
  const relatedChanges = op.changes.filter((c) => c.relatedEntityType);

  return (
    <Accordion disableGutters variant="outlined" sx={{ "&:before": { display: "none" } }}>
      <AccordionSummary expandIcon={<ExpandMoreIcon />}>
        <Box sx={{ display: "flex", alignItems: "center", gap: 1, width: "100%" }}>
          <Typography variant="body2" fontWeight={600}>{dateStr}, {timeStr}</Typography>
          <Typography variant="body2" color="text.secondary">— {op.userName ?? "system"}</Typography>
          <Chip label={op.changeType} color={color} size="small" sx={{ ml: "auto" }} />
        </Box>
      </AccordionSummary>
      <AccordionDetails sx={{ pt: 0 }}>
        {/* Root entity changes */}
        {rootChanges.length > 0 && (
          <Box sx={{ mb: 1 }}>
            <Typography variant="body2" fontWeight={600} sx={{ mb: 0.5 }}>
              {entityLabel}{displayName ? ` — ${displayName}` : ""}
            </Typography>
            {rootChanges.flatMap((g) =>
              g.fields.map((f, i) => <FieldRow key={i} field={f} entityType={entityType} />)
            )}
          </Box>
        )}

        {rootChanges.length > 0 && relatedChanges.length > 0 && <Divider sx={{ my: 1 }} />}

        {/* Related entity changes */}
        {relatedChanges.map((g, i) => (
          <ChangeGroup key={i} group={g} />
        ))}
      </AccordionDetails>
    </Accordion>
  );
}

export function EntityHistoryDialog({ entityType, entityId, open, onClose }: Props) {
  const [page, setPage] = useState(1);
  const pageSize = 10;

  const { data, isLoading } = useEntityChanges(
    { entityType, entityId, page, pageSize },
    open && !!entityId,
  );

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>Change History</DialogTitle>
      <DialogContent sx={{ minHeight: 200 }}>
        {isLoading ? (
          <Box sx={{ display: "flex", justifyContent: "center", mt: 4 }}>
            <CircularProgress />
          </Box>
        ) : !data || data.items.length === 0 ? (
          <Typography color="text.secondary" sx={{ mt: 2 }}>No changes recorded.</Typography>
        ) : (
          <Box sx={{ display: "flex", flexDirection: "column", gap: 1 }}>
            {data.items.map((op) => (
              <OperationItem key={op.operationId} op={op} entityType={entityType} />
            ))}
          </Box>
        )}

        {data && data.totalPages > 1 && (
          <Box sx={{ display: "flex", justifyContent: "center", mt: 2 }}>
            <Pagination
              count={data.totalPages}
              page={page}
              onChange={(_, p) => setPage(p)}
              size="small"
            />
          </Box>
        )}
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Close</Button>
      </DialogActions>
    </Dialog>
  );
}
