export const CHANGE_TYPE_COLORS: Record<string, "success" | "warning" | "error"> = {
  Created: "success",
  Modified: "warning",
  Deleted: "error",
};

export const FIELD_LABELS: Record<string, Record<string, string>> = {
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
    UserId: "User", RoleId: "Role",
  },
  Role: {
    Name: "Name", Description: "Description", IsSystem: "System Role",
  },
  RolePermission: {
    RoleId: "Role", PermissionId: "Permission",
  },
};

export function getFieldLabel(entityType: string | null, fieldName: string): string {
  if (entityType && FIELD_LABELS[entityType]?.[fieldName]) {
    return FIELD_LABELS[entityType][fieldName];
  }
  return fieldName.replace(/([A-Z])/g, " $1").trim();
}

export function getEntityTypeLabel(type: string | null): string {
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
