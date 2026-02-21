import { useEffect, useState } from "react";
import {
  Dialog, DialogTitle, DialogContent, DialogActions, Button,
  TextField, MenuItem, Box, Typography, Switch, FormControlLabel, Divider,
  IconButton, Autocomplete, Accordion, AccordionSummary, AccordionDetails,
  Table, TableBody, TableCell, TableHead, TableRow, Checkbox,
} from "@mui/material";
import AddIcon from "@mui/icons-material/Add";
import DeleteIcon from "@mui/icons-material/Delete";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import { useCreateClient, useUpdateClient, useClient, useCountries, useClientAccounts, useSetClientAccounts, useAccounts } from "../api/hooks";
import type {
  CreateClientRequest, UpdateClientRequest, CreateClientAddressRequest, CreateInvestmentProfileRequest,
  ClientType, ClientStatus, KycStatus, AddressType, Gender, MaritalStatus, Education,
  RiskLevel, CountryDto,
  InvestmentObjective, InvestmentRiskTolerance, LiquidityNeeds, InvestmentTimeHorizon,
  InvestmentKnowledge, InvestmentExperience,
  ClientAccountInput, HolderRole, AccountListItemDto,
} from "../api/types";

const CLIENT_TYPES: ClientType[] = ["Individual", "Corporate"];
const STATUSES: ClientStatus[] = ["Active", "Blocked", "PendingKyc"];
const KYC_STATUSES: KycStatus[] = ["NotStarted", "InProgress", "Approved", "Rejected"];
const RISK_LEVELS: RiskLevel[] = ["Low", "Medium", "High"];
const GENDERS: Gender[] = ["Male", "Female", "Other", "Unspecified"];
const ADDRESS_TYPES: AddressType[] = ["Legal", "Mailing", "Working"];
const MARITAL_STATUSES: MaritalStatus[] = ["Single", "Married", "Divorced", "Widowed", "Separated", "CivilUnion", "Unspecified"];
const EDUCATIONS: Education[] = ["None", "HighSchool", "Bachelor", "Master", "PhD", "Other", "Unspecified"];

const HOLDER_ROLES: HolderRole[] = ["Owner", "Beneficiary", "Trustee", "PowerOfAttorney", "Custodian", "Authorized"];

const INV_OBJECTIVES: InvestmentObjective[] = ["Preservation", "Income", "Growth", "Speculation", "Hedging", "Other"];
const INV_RISK_TOLERANCES: InvestmentRiskTolerance[] = ["Low", "Medium", "High"];
const INV_LIQUIDITY: LiquidityNeeds[] = ["Low", "Medium", "High"];
const INV_TIME_HORIZONS: InvestmentTimeHorizon[] = ["Short", "Medium", "Long"];
const INV_KNOWLEDGE: InvestmentKnowledge[] = ["None", "Basic", "Good", "Advanced"];
const INV_EXPERIENCE: InvestmentExperience[] = ["None", "LessThan1Year", "OneToThreeYears", "ThreeToFiveYears", "MoreThan5Years"];

const emptyAddress = (type: AddressType): CreateClientAddressRequest => ({
  type, line1: "", city: "", countryId: "",
});

const emptyInvestmentProfile = (): CreateInvestmentProfileRequest => ({});

interface CreateProps { open: boolean; onClose: () => void }

export function CreateClientDialog({ open, onClose }: CreateProps) {
  const create = useCreateClient();
  const { data: countries = [] } = useCountries();
  const [form, setForm] = useState<CreateClientRequest>({
    clientType: "Individual",
    status: "Active",
    email: "",
    pepStatus: false,
    kycStatus: "NotStarted",
    addresses: [emptyAddress("Legal")],
  });
  const [showInvestmentProfile, setShowInvestmentProfile] = useState(false);

  useEffect(() => {
    if (open) {
      setForm({
        clientType: "Individual", status: "Active", email: "",
        pepStatus: false, kycStatus: "NotStarted",
        addresses: [emptyAddress("Legal")],
      });
      setShowInvestmentProfile(false);
    }
  }, [open]);

  const set = <K extends keyof CreateClientRequest>(k: K, v: CreateClientRequest[K]) =>
    setForm((f) => ({ ...f, [k]: v }));

  const setAddr = (idx: number, field: string, value: string) =>
    setForm((f) => ({
      ...f,
      addresses: f.addresses.map((a, i) => i === idx ? { ...a, [field]: value } : a),
    }));

  const addAddress = () => setForm((f) => ({ ...f, addresses: [...f.addresses, emptyAddress("Legal")] }));
  const removeAddress = (idx: number) => setForm((f) => ({ ...f, addresses: f.addresses.filter((_, i) => i !== idx) }));

  const setIp = <K extends keyof CreateInvestmentProfileRequest>(k: K, v: CreateInvestmentProfileRequest[K]) =>
    setForm((f) => ({ ...f, investmentProfile: { ...f.investmentProfile, [k]: v } }));

  const handleSubmit = async () => {
    const payload = { ...form };
    if (!showInvestmentProfile) delete payload.investmentProfile;
    await create.mutateAsync(payload);
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>Create Client</DialogTitle>
      <DialogContent>
        <Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 1 }}>
          <Typography variant="subtitle2">General</Typography>
          <Box sx={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 2 }}>
            <TextField select label="Type" value={form.clientType} onChange={(e) => set("clientType", e.target.value as ClientType)} size="small">
              {CLIENT_TYPES.map((t) => <MenuItem key={t} value={t}>{t}</MenuItem>)}
            </TextField>
            <TextField select label="Status" value={form.status} onChange={(e) => set("status", e.target.value as ClientStatus)} size="small">
              {STATUSES.map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
            </TextField>
            <TextField label="Email" value={form.email} onChange={(e) => set("email", e.target.value)} size="small" required />
            <TextField label="Phone" value={form.phone ?? ""} onChange={(e) => set("phone", e.target.value || undefined)} size="small" />
            <TextField label="External ID" value={form.externalId ?? ""} onChange={(e) => set("externalId", e.target.value || undefined)} size="small" />
          </Box>

          {form.clientType === "Individual" && (
            <>
              <Typography variant="subtitle2">Personal Data</Typography>
              <Box sx={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: 2 }}>
                <TextField label="First Name" value={form.firstName ?? ""} onChange={(e) => set("firstName", e.target.value || undefined)} size="small" required />
                <TextField label="Last Name" value={form.lastName ?? ""} onChange={(e) => set("lastName", e.target.value || undefined)} size="small" required />
                <TextField label="Middle Name" value={form.middleName ?? ""} onChange={(e) => set("middleName", e.target.value || undefined)} size="small" />
                <TextField label="Date of Birth" type="date" value={form.dateOfBirth ?? ""} onChange={(e) => set("dateOfBirth", e.target.value || undefined)} size="small" slotProps={{ inputLabel: { shrink: true } }} />
                <TextField select label="Gender" value={form.gender ?? ""} onChange={(e) => set("gender", (e.target.value || undefined) as Gender | undefined)} size="small">
                  <MenuItem value="">—</MenuItem>
                  {GENDERS.map((g) => <MenuItem key={g} value={g}>{g}</MenuItem>)}
                </TextField>
                <TextField select label="Marital Status" value={form.maritalStatus ?? ""} onChange={(e) => set("maritalStatus", (e.target.value || undefined) as MaritalStatus | undefined)} size="small">
                  <MenuItem value="">—</MenuItem>
                  {MARITAL_STATUSES.map((m) => <MenuItem key={m} value={m}>{m}</MenuItem>)}
                </TextField>
                <TextField select label="Education" value={form.education ?? ""} onChange={(e) => set("education", (e.target.value || undefined) as Education | undefined)} size="small">
                  <MenuItem value="">—</MenuItem>
                  {EDUCATIONS.map((ed) => <MenuItem key={ed} value={ed}>{ed}</MenuItem>)}
                </TextField>
                <TextField label="SSN" value={form.ssn ?? ""} onChange={(e) => set("ssn", e.target.value || undefined)} size="small" />
                <TextField label="Passport Number" value={form.passportNumber ?? ""} onChange={(e) => set("passportNumber", e.target.value || undefined)} size="small" />
                <TextField label="Driver License" value={form.driverLicenseNumber ?? ""} onChange={(e) => set("driverLicenseNumber", e.target.value || undefined)} size="small" />
              </Box>
            </>
          )}

          {form.clientType === "Corporate" && (
            <>
              <Typography variant="subtitle2">Corporate</Typography>
              <Box sx={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 2 }}>
                <TextField label="Company Name" value={form.companyName ?? ""} onChange={(e) => set("companyName", e.target.value || undefined)} size="small" required />
                <TextField label="Registration Number" value={form.registrationNumber ?? ""} onChange={(e) => set("registrationNumber", e.target.value || undefined)} size="small" />
                <TextField label="Tax ID" value={form.taxId ?? ""} onChange={(e) => set("taxId", e.target.value || undefined)} size="small" />
              </Box>
            </>
          )}

          <Typography variant="subtitle2">KYC & Compliance</Typography>
          <Box sx={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 2 }}>
            <TextField select label="KYC Status" value={form.kycStatus} onChange={(e) => set("kycStatus", e.target.value as KycStatus)} size="small">
              {KYC_STATUSES.map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
            </TextField>
            <TextField select label="Risk Level" value={form.riskLevel ?? ""} onChange={(e) => set("riskLevel", (e.target.value || undefined) as RiskLevel | undefined)} size="small">
              <MenuItem value="">—</MenuItem>
              {RISK_LEVELS.map((r) => <MenuItem key={r} value={r}>{r}</MenuItem>)}
            </TextField>
            <CountryAutocomplete countries={countries} value={form.residenceCountryId ?? null} onChange={(id) => set("residenceCountryId", id ?? undefined)} label="Residence Country" />
            <CountryAutocomplete countries={countries} value={form.citizenshipCountryId ?? null} onChange={(id) => set("citizenshipCountryId", id ?? undefined)} label="Citizenship Country" />
            <FormControlLabel control={<Switch checked={form.pepStatus} onChange={(e) => set("pepStatus", e.target.checked)} />} label="PEP Status" />
          </Box>

          <Divider />
          <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
            <Typography variant="subtitle2">Addresses</Typography>
            <Button size="small" startIcon={<AddIcon />} onClick={addAddress}>Add</Button>
          </Box>
          {form.addresses.map((addr, idx) => (
            <AddressFields
              key={idx}
              addr={addr}
              countries={countries}
              onChange={(f, v) => setAddr(idx, f, v)}
              onRemove={form.addresses.length > 1 ? () => removeAddress(idx) : undefined}
            />
          ))}

          <Divider />
          <Accordion
            expanded={showInvestmentProfile}
            onChange={(_, expanded) => {
              setShowInvestmentProfile(expanded);
              if (expanded && !form.investmentProfile) set("investmentProfile", emptyInvestmentProfile());
            }}
            disableGutters elevation={0} sx={{ "&:before": { display: "none" } }}
          >
            <AccordionSummary expandIcon={<ExpandMoreIcon />}>
              <Typography variant="subtitle2">Investment Profile</Typography>
            </AccordionSummary>
            <AccordionDetails>
              {form.investmentProfile && <InvestmentProfileFields profile={form.investmentProfile} onChange={setIp} />}
            </AccordionDetails>
          </Accordion>
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit} disabled={create.isPending}>Create</Button>
      </DialogActions>
    </Dialog>
  );
}

interface EditProps { open: boolean; onClose: () => void; clientId: string | null }

export function EditClientDialog({ open, onClose, clientId }: EditProps) {
  const { data: client } = useClient(clientId ?? "");
  const update = useUpdateClient();
  const { data: countries = [] } = useCountries();
  const { data: clientAccounts } = useClientAccounts(clientId ?? "");
  const setClientAccounts = useSetClientAccounts();
  const { data: accountsData } = useAccounts({ page: 1, pageSize: 200 });
  const allAccounts = accountsData?.items ?? [];
  const [form, setForm] = useState<UpdateClientRequest | null>(null);
  const [showInvestmentProfile, setShowInvestmentProfile] = useState(false);
  const [accounts, setAccounts] = useState<ClientAccountInput[]>([]);

  useEffect(() => {
    if (client && open) {
      const hasIp = !!client.investmentProfile;
      setShowInvestmentProfile(hasIp);
      setForm({
        id: client.id,
        clientType: client.clientType,
        externalId: client.externalId ?? undefined,
        status: client.status,
        email: client.email,
        phone: client.phone ?? undefined,
        preferredLanguage: client.preferredLanguage ?? undefined,
        timeZone: client.timeZone ?? undefined,
        residenceCountryId: client.residenceCountryId ?? undefined,
        citizenshipCountryId: client.citizenshipCountryId ?? undefined,
        pepStatus: client.pepStatus,
        riskLevel: client.riskLevel ?? undefined,
        kycStatus: client.kycStatus,
        kycReviewedAtUtc: client.kycReviewedAtUtc ?? undefined,
        firstName: client.firstName ?? undefined,
        lastName: client.lastName ?? undefined,
        middleName: client.middleName ?? undefined,
        dateOfBirth: client.dateOfBirth ?? undefined,
        gender: client.gender ?? undefined,
        maritalStatus: client.maritalStatus ?? undefined,
        education: client.education ?? undefined,
        ssn: client.ssn ?? undefined,
        passportNumber: client.passportNumber ?? undefined,
        driverLicenseNumber: client.driverLicenseNumber ?? undefined,
        companyName: client.companyName ?? undefined,
        registrationNumber: client.registrationNumber ?? undefined,
        taxId: client.taxId ?? undefined,
        addresses: client.addresses.map((a) => ({
          type: a.type, line1: a.line1, line2: a.line2 ?? undefined,
          city: a.city, state: a.state ?? undefined,
          postalCode: a.postalCode ?? undefined, countryId: a.countryId,
        })),
        investmentProfile: client.investmentProfile ? {
          objective: client.investmentProfile.objective ?? undefined,
          riskTolerance: client.investmentProfile.riskTolerance ?? undefined,
          liquidityNeeds: client.investmentProfile.liquidityNeeds ?? undefined,
          timeHorizon: client.investmentProfile.timeHorizon ?? undefined,
          knowledge: client.investmentProfile.knowledge ?? undefined,
          experience: client.investmentProfile.experience ?? undefined,
          notes: client.investmentProfile.notes ?? undefined,
        } : undefined,
        rowVersion: client.rowVersion,
      });
    }
  }, [client, open]);

  useEffect(() => {
    if (clientAccounts && open) {
      setAccounts(clientAccounts.map((a) => ({ accountId: a.accountId, role: a.role, isPrimary: a.isPrimary })));
    }
  }, [clientAccounts, open]);

  if (!form) return null;

  const set = <K extends keyof UpdateClientRequest>(k: K, v: UpdateClientRequest[K]) =>
    setForm((f) => f ? { ...f, [k]: v } : f);

  const setAddr = (idx: number, field: string, value: string) =>
    setForm((f) => f ? {
      ...f,
      addresses: f.addresses.map((a, i) => i === idx ? { ...a, [field]: value } : a),
    } : f);

  const addAddress = () => setForm((f) => f ? { ...f, addresses: [...f.addresses, emptyAddress("Legal")] } : f);
  const removeAddress = (idx: number) => setForm((f) => f ? { ...f, addresses: f.addresses.filter((_, i) => i !== idx) } : f);

  const addAccountRow = () => {
    setAccounts((prev) => [...prev, { accountId: "", role: "Owner", isPrimary: false }]);
  };
  const removeAccountRow = (index: number) => {
    setAccounts((prev) => prev.filter((_, i) => i !== index));
  };
  const updateAccountRow = (index: number, patch: Partial<ClientAccountInput>) => {
    setAccounts((prev) => prev.map((a, i) => (i === index ? { ...a, ...patch } : a)));
  };

  const setIp = <K extends keyof CreateInvestmentProfileRequest>(k: K, v: CreateInvestmentProfileRequest[K]) =>
    setForm((f) => f ? { ...f, investmentProfile: { ...f.investmentProfile, [k]: v } } : f);

  const handleSubmit = async () => {
    if (!form || !clientId) return;
    const payload = { ...form };
    if (!showInvestmentProfile) delete payload.investmentProfile;
    await update.mutateAsync(payload);
    const validAccounts = accounts.filter((a) => a.accountId);
    await setClientAccounts.mutateAsync({ clientId, accounts: validAccounts });
    onClose();
  };

  return (
    <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
      <DialogTitle>Edit Client</DialogTitle>
      <DialogContent>
        <Box sx={{ display: "flex", flexDirection: "column", gap: 2, mt: 1 }}>
          <Typography variant="subtitle2">General</Typography>
          <Box sx={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 2 }}>
            <TextField select label="Type" value={form.clientType} onChange={(e) => set("clientType", e.target.value as ClientType)} size="small">
              {CLIENT_TYPES.map((t) => <MenuItem key={t} value={t}>{t}</MenuItem>)}
            </TextField>
            <TextField select label="Status" value={form.status} onChange={(e) => set("status", e.target.value as ClientStatus)} size="small">
              {STATUSES.map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
            </TextField>
            <TextField label="Email" value={form.email} onChange={(e) => set("email", e.target.value)} size="small" required />
            <TextField label="Phone" value={form.phone ?? ""} onChange={(e) => set("phone", e.target.value || undefined)} size="small" />
            <TextField label="External ID" value={form.externalId ?? ""} onChange={(e) => set("externalId", e.target.value || undefined)} size="small" />
          </Box>

          {form.clientType === "Individual" && (
            <>
              <Typography variant="subtitle2">Personal Data</Typography>
              <Box sx={{ display: "grid", gridTemplateColumns: "1fr 1fr 1fr", gap: 2 }}>
                <TextField label="First Name" value={form.firstName ?? ""} onChange={(e) => set("firstName", e.target.value || undefined)} size="small" required />
                <TextField label="Last Name" value={form.lastName ?? ""} onChange={(e) => set("lastName", e.target.value || undefined)} size="small" required />
                <TextField label="Middle Name" value={form.middleName ?? ""} onChange={(e) => set("middleName", e.target.value || undefined)} size="small" />
                <TextField label="Date of Birth" type="date" value={form.dateOfBirth ?? ""} onChange={(e) => set("dateOfBirth", e.target.value || undefined)} size="small" slotProps={{ inputLabel: { shrink: true } }} />
                <TextField select label="Gender" value={form.gender ?? ""} onChange={(e) => set("gender", (e.target.value || undefined) as Gender | undefined)} size="small">
                  <MenuItem value="">—</MenuItem>
                  {GENDERS.map((g) => <MenuItem key={g} value={g}>{g}</MenuItem>)}
                </TextField>
                <TextField select label="Marital Status" value={form.maritalStatus ?? ""} onChange={(e) => set("maritalStatus", (e.target.value || undefined) as MaritalStatus | undefined)} size="small">
                  <MenuItem value="">—</MenuItem>
                  {MARITAL_STATUSES.map((m) => <MenuItem key={m} value={m}>{m}</MenuItem>)}
                </TextField>
                <TextField select label="Education" value={form.education ?? ""} onChange={(e) => set("education", (e.target.value || undefined) as Education | undefined)} size="small">
                  <MenuItem value="">—</MenuItem>
                  {EDUCATIONS.map((ed) => <MenuItem key={ed} value={ed}>{ed}</MenuItem>)}
                </TextField>
                <TextField label="SSN" value={form.ssn ?? ""} onChange={(e) => set("ssn", e.target.value || undefined)} size="small" />
                <TextField label="Passport Number" value={form.passportNumber ?? ""} onChange={(e) => set("passportNumber", e.target.value || undefined)} size="small" />
                <TextField label="Driver License" value={form.driverLicenseNumber ?? ""} onChange={(e) => set("driverLicenseNumber", e.target.value || undefined)} size="small" />
              </Box>
            </>
          )}

          {form.clientType === "Corporate" && (
            <>
              <Typography variant="subtitle2">Corporate</Typography>
              <Box sx={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 2 }}>
                <TextField label="Company Name" value={form.companyName ?? ""} onChange={(e) => set("companyName", e.target.value || undefined)} size="small" required />
                <TextField label="Registration Number" value={form.registrationNumber ?? ""} onChange={(e) => set("registrationNumber", e.target.value || undefined)} size="small" />
                <TextField label="Tax ID" value={form.taxId ?? ""} onChange={(e) => set("taxId", e.target.value || undefined)} size="small" />
              </Box>
            </>
          )}

          <Typography variant="subtitle2">KYC & Compliance</Typography>
          <Box sx={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 2 }}>
            <TextField select label="KYC Status" value={form.kycStatus} onChange={(e) => set("kycStatus", e.target.value as KycStatus)} size="small">
              {KYC_STATUSES.map((s) => <MenuItem key={s} value={s}>{s}</MenuItem>)}
            </TextField>
            <TextField select label="Risk Level" value={form.riskLevel ?? ""} onChange={(e) => set("riskLevel", (e.target.value || undefined) as RiskLevel | undefined)} size="small">
              <MenuItem value="">—</MenuItem>
              {RISK_LEVELS.map((r) => <MenuItem key={r} value={r}>{r}</MenuItem>)}
            </TextField>
            <CountryAutocomplete countries={countries} value={form.residenceCountryId ?? null} onChange={(id) => set("residenceCountryId", id ?? undefined)} label="Residence Country" />
            <CountryAutocomplete countries={countries} value={form.citizenshipCountryId ?? null} onChange={(id) => set("citizenshipCountryId", id ?? undefined)} label="Citizenship Country" />
            <FormControlLabel control={<Switch checked={form.pepStatus} onChange={(e) => set("pepStatus", e.target.checked)} />} label="PEP Status" />
          </Box>

          <Divider />
          <Box sx={{ display: "flex", alignItems: "center", gap: 1 }}>
            <Typography variant="subtitle2">Addresses</Typography>
            <Button size="small" startIcon={<AddIcon />} onClick={addAddress}>Add</Button>
          </Box>
          {form.addresses.map((addr, idx) => (
            <AddressFields
              key={idx}
              addr={addr}
              countries={countries}
              onChange={(f, v) => setAddr(idx, f, v)}
              onRemove={form.addresses.length > 1 ? () => removeAddress(idx) : undefined}
            />
          ))}

          <Divider />
          <Box>
            <Box sx={{ display: "flex", justifyContent: "space-between", alignItems: "center", mb: 1 }}>
              <Typography variant="subtitle2">Accounts</Typography>
              <Button size="small" startIcon={<AddIcon />} onClick={addAccountRow}>Add</Button>
            </Box>
            {accounts.length === 0 ? (
              <Typography variant="body2" color="text.secondary">No accounts.</Typography>
            ) : (
              <Table size="small">
                <TableHead>
                  <TableRow>
                    <TableCell sx={{ width: "40%" }}>Account</TableCell>
                    <TableCell>Role</TableCell>
                    <TableCell>Primary</TableCell>
                    <TableCell sx={{ width: 50 }} />
                  </TableRow>
                </TableHead>
                <TableBody>
                  {accounts.map((a, i) => (
                    <TableRow key={i}>
                      <TableCell>
                        <AccountAutocomplete
                          accounts={allAccounts}
                          value={a.accountId}
                          onChange={(id) => updateAccountRow(i, { accountId: id })}
                        />
                      </TableCell>
                      <TableCell>
                        <TextField
                          select size="small" fullWidth value={a.role}
                          onChange={(e) => updateAccountRow(i, { role: e.target.value as HolderRole })}
                        >
                          {HOLDER_ROLES.map((r) => <MenuItem key={r} value={r}>{r}</MenuItem>)}
                        </TextField>
                      </TableCell>
                      <TableCell>
                        <FormControlLabel
                          control={<Checkbox checked={a.isPrimary} onChange={(e) => updateAccountRow(i, { isPrimary: e.target.checked })} size="small" />}
                          label=""
                        />
                      </TableCell>
                      <TableCell>
                        <IconButton size="small" onClick={() => removeAccountRow(i)} color="error">
                          <DeleteIcon fontSize="small" />
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </Box>

          <Divider />
          <Accordion
            expanded={showInvestmentProfile}
            onChange={(_, expanded) => {
              setShowInvestmentProfile(expanded);
              if (expanded && !form.investmentProfile) set("investmentProfile", emptyInvestmentProfile());
            }}
            disableGutters elevation={0} sx={{ "&:before": { display: "none" } }}
          >
            <AccordionSummary expandIcon={<ExpandMoreIcon />}>
              <Typography variant="subtitle2">Investment Profile</Typography>
            </AccordionSummary>
            <AccordionDetails>
              {form.investmentProfile && <InvestmentProfileFields profile={form.investmentProfile} onChange={setIp} />}
            </AccordionDetails>
          </Accordion>
        </Box>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>Cancel</Button>
        <Button variant="contained" onClick={handleSubmit} disabled={update.isPending || setClientAccounts.isPending}>Save</Button>
      </DialogActions>
    </Dialog>
  );
}

function CountryAutocomplete({ countries, value, onChange, label }: {
  countries: CountryDto[];
  value: string | null;
  onChange: (id: string | null) => void;
  label: string;
}) {
  const selected = countries.find((c) => c.id === value) ?? null;
  return (
    <Autocomplete
      size="small"
      options={countries}
      getOptionLabel={(o) => `${o.flagEmoji} ${o.name}`}
      value={selected}
      onChange={(_, v) => onChange(v?.id ?? null)}
      renderInput={(params) => <TextField {...params} label={label} />}
      isOptionEqualToValue={(o, v) => o.id === v.id}
    />
  );
}

function AddressFields({ addr, countries, onChange, onRemove }: {
  addr: CreateClientAddressRequest;
  countries: CountryDto[];
  onChange: (field: string, value: string) => void;
  onRemove?: () => void;
}) {
  return (
    <Box sx={{ border: 1, borderColor: "divider", borderRadius: 1, p: 2 }}>
      <Box sx={{ display: "flex", justifyContent: "space-between", mb: 1 }}>
        <TextField select label="Type" value={addr.type} onChange={(e) => onChange("type", e.target.value)} size="small" sx={{ minWidth: 120 }}>
          {ADDRESS_TYPES.map((t) => <MenuItem key={t} value={t}>{t}</MenuItem>)}
        </TextField>
        {onRemove && (
          <IconButton size="small" onClick={onRemove} color="error"><DeleteIcon fontSize="small" /></IconButton>
        )}
      </Box>
      <Box sx={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 2 }}>
        <TextField label="Line 1" value={addr.line1} onChange={(e) => onChange("line1", e.target.value)} size="small" required />
        <TextField label="Line 2" value={addr.line2 ?? ""} onChange={(e) => onChange("line2", e.target.value)} size="small" />
        <TextField label="City" value={addr.city} onChange={(e) => onChange("city", e.target.value)} size="small" required />
        <TextField label="State" value={addr.state ?? ""} onChange={(e) => onChange("state", e.target.value)} size="small" />
        <TextField label="Postal Code" value={addr.postalCode ?? ""} onChange={(e) => onChange("postalCode", e.target.value)} size="small" />
        <CountryAutocomplete countries={countries} value={addr.countryId || null} onChange={(id) => onChange("countryId", id ?? "")} label="Country" />
      </Box>
    </Box>
  );
}

/* ── Account Autocomplete (strict, for account selection) ── */

function AccountAutocomplete({ accounts, value, onChange }: {
  accounts: AccountListItemDto[];
  value: string;
  onChange: (id: string) => void;
}) {
  const selected = accounts.find((a) => a.id === value) ?? null;
  return (
    <Autocomplete
      size="small"
      options={accounts}
      getOptionLabel={(o) => o.number}
      value={selected}
      onChange={(_, v) => onChange(v?.id ?? "")}
      renderInput={(params) => <TextField {...params} placeholder="Select account..." />}
      isOptionEqualToValue={(o, v) => o.id === v.id}
      autoHighlight
      openOnFocus
    />
  );
}

function InvestmentProfileFields({ profile, onChange }: {
  profile: CreateInvestmentProfileRequest;
  onChange: <K extends keyof CreateInvestmentProfileRequest>(k: K, v: CreateInvestmentProfileRequest[K]) => void;
}) {
  return (
    <Box sx={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 2 }}>
      <TextField select label="Objective" value={profile.objective ?? ""} onChange={(e) => onChange("objective", (e.target.value || undefined) as InvestmentObjective | undefined)} size="small">
        <MenuItem value="">—</MenuItem>
        {INV_OBJECTIVES.map((o) => <MenuItem key={o} value={o}>{o}</MenuItem>)}
      </TextField>
      <TextField select label="Risk Tolerance" value={profile.riskTolerance ?? ""} onChange={(e) => onChange("riskTolerance", (e.target.value || undefined) as InvestmentRiskTolerance | undefined)} size="small">
        <MenuItem value="">—</MenuItem>
        {INV_RISK_TOLERANCES.map((r) => <MenuItem key={r} value={r}>{r}</MenuItem>)}
      </TextField>
      <TextField select label="Liquidity Needs" value={profile.liquidityNeeds ?? ""} onChange={(e) => onChange("liquidityNeeds", (e.target.value || undefined) as LiquidityNeeds | undefined)} size="small">
        <MenuItem value="">—</MenuItem>
        {INV_LIQUIDITY.map((l) => <MenuItem key={l} value={l}>{l}</MenuItem>)}
      </TextField>
      <TextField select label="Time Horizon" value={profile.timeHorizon ?? ""} onChange={(e) => onChange("timeHorizon", (e.target.value || undefined) as InvestmentTimeHorizon | undefined)} size="small">
        <MenuItem value="">—</MenuItem>
        {INV_TIME_HORIZONS.map((t) => <MenuItem key={t} value={t}>{t}</MenuItem>)}
      </TextField>
      <TextField select label="Knowledge" value={profile.knowledge ?? ""} onChange={(e) => onChange("knowledge", (e.target.value || undefined) as InvestmentKnowledge | undefined)} size="small">
        <MenuItem value="">—</MenuItem>
        {INV_KNOWLEDGE.map((k) => <MenuItem key={k} value={k}>{k}</MenuItem>)}
      </TextField>
      <TextField select label="Experience" value={profile.experience ?? ""} onChange={(e) => onChange("experience", (e.target.value || undefined) as InvestmentExperience | undefined)} size="small">
        <MenuItem value="">—</MenuItem>
        {INV_EXPERIENCE.map((ex) => <MenuItem key={ex} value={ex}>{ex}</MenuItem>)}
      </TextField>
      <TextField
        label="Notes" value={profile.notes ?? ""}
        onChange={(e) => onChange("notes", e.target.value || undefined)}
        size="small" multiline rows={3} sx={{ gridColumn: "1 / -1" }}
      />
    </Box>
  );
}
