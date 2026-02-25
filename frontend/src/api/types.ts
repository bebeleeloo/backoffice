export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

export interface UserProfile {
  id: string;
  username: string;
  email: string;
  fullName: string | null;
  roles: string[];
  permissions: string[];
  scopes: { scopeType: string; scopeValue: string }[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface UserDto {
  id: string;
  username: string;
  email: string;
  fullName: string | null;
  isActive: boolean;
  roles: string[];
  createdAt: string;
  rowVersion: string;
}

export interface RoleDto {
  id: string;
  name: string;
  description: string | null;
  isSystem: boolean;
  permissions: string[];
  createdAt: string;
  rowVersion: string;
}

export interface PermissionDto {
  id: string;
  code: string;
  name: string;
  description: string | null;
  group: string;
}

export interface AuditLogDto {
  id: string;
  userId: string | null;
  userName: string | null;
  action: string;
  entityType: string | null;
  entityId: string | null;
  beforeJson: string | null;
  afterJson: string | null;
  correlationId: string | null;
  ipAddress: string | null;
  path: string;
  method: string;
  statusCode: number;
  isSuccess: boolean;
  createdAt: string;
}

export interface CreateUserRequest {
  username: string;
  email: string;
  password: string;
  fullName?: string;
  isActive: boolean;
  roleIds: string[];
}

export interface UpdateUserRequest {
  id: string;
  email: string;
  fullName?: string;
  isActive: boolean;
  roleIds: string[];
  rowVersion: string;
}

export interface CreateRoleRequest {
  name: string;
  description?: string;
}

export interface UpdateRoleRequest {
  id: string;
  name: string;
  description?: string;
  rowVersion: string;
}

export interface PagedParams {
  page?: number;
  pageSize?: number;
  sort?: string;
  q?: string;
}

export interface UsersParams extends PagedParams {
  isActive?: boolean;
  username?: string;
  email?: string;
  fullName?: string;
  role?: string;
}

export interface RolesParams extends PagedParams {
  name?: string;
  description?: string;
  isSystem?: boolean;
  permission?: string;
}

export interface AuditParams extends PagedParams {
  from?: string;
  to?: string;
  userId?: string;
  action?: string;
  entityType?: string;
  isSuccess?: boolean;
  userName?: string;
  method?: string;
  path?: string;
  statusCode?: number;
}

// Countries
export interface CountryDto {
  id: string;
  iso2: string;
  iso3: string | null;
  name: string;
  flagEmoji: string;
}

// Clients
export type ClientType = "Individual" | "Corporate";
export type ClientStatus = "Active" | "Blocked" | "PendingKyc";
export type KycStatus = "NotStarted" | "InProgress" | "Approved" | "Rejected";
export type RiskLevel = "Low" | "Medium" | "High";
export type Gender = "Male" | "Female" | "Other" | "Unspecified";
export type AddressType = "Legal" | "Mailing" | "Working";
export type MaritalStatus = "Single" | "Married" | "Divorced" | "Widowed" | "Separated" | "CivilUnion" | "Unspecified";
export type Education = "None" | "HighSchool" | "Bachelor" | "Master" | "PhD" | "Other" | "Unspecified";

// Investment Profile enums
export type InvestmentObjective = "Preservation" | "Income" | "Growth" | "Speculation" | "Hedging" | "Other";
export type InvestmentRiskTolerance = "Low" | "Medium" | "High";
export type LiquidityNeeds = "Low" | "Medium" | "High";
export type InvestmentTimeHorizon = "Short" | "Medium" | "Long";
export type InvestmentKnowledge = "None" | "Basic" | "Good" | "Advanced";
export type InvestmentExperience = "None" | "LessThan1Year" | "OneToThreeYears" | "ThreeToFiveYears" | "MoreThan5Years";

export interface InvestmentProfileDto {
  id: string;
  objective: InvestmentObjective | null;
  riskTolerance: InvestmentRiskTolerance | null;
  liquidityNeeds: LiquidityNeeds | null;
  timeHorizon: InvestmentTimeHorizon | null;
  knowledge: InvestmentKnowledge | null;
  experience: InvestmentExperience | null;
  notes: string | null;
}

export interface CreateInvestmentProfileRequest {
  objective?: InvestmentObjective;
  riskTolerance?: InvestmentRiskTolerance;
  liquidityNeeds?: LiquidityNeeds;
  timeHorizon?: InvestmentTimeHorizon;
  knowledge?: InvestmentKnowledge;
  experience?: InvestmentExperience;
  notes?: string;
}

export interface ClientListItemDto {
  id: string;
  clientType: ClientType;
  displayName: string;
  email: string;
  status: ClientStatus;
  kycStatus: KycStatus;
  residenceCountryIso2: string | null;
  residenceCountryFlagEmoji: string | null;
  createdAt: string;
  rowVersion: string;
  phone: string | null;
  externalId: string | null;
  pepStatus: boolean;
  riskLevel: RiskLevel | null;
  residenceCountryName: string | null;
  citizenshipCountryIso2: string | null;
  citizenshipCountryFlagEmoji: string | null;
  citizenshipCountryName: string | null;
}

export interface ClientAddressDto {
  id: string;
  type: AddressType;
  line1: string;
  line2: string | null;
  city: string;
  state: string | null;
  postalCode: string | null;
  countryId: string;
  countryIso2: string;
  countryName: string;
  countryFlagEmoji: string;
}

export interface ClientDto {
  id: string;
  clientType: ClientType;
  externalId: string | null;
  status: ClientStatus;
  email: string;
  phone: string | null;
  preferredLanguage: string | null;
  timeZone: string | null;
  residenceCountryId: string | null;
  residenceCountryIso2: string | null;
  residenceCountryName: string | null;
  residenceCountryFlagEmoji: string | null;
  citizenshipCountryId: string | null;
  citizenshipCountryIso2: string | null;
  citizenshipCountryName: string | null;
  citizenshipCountryFlagEmoji: string | null;
  pepStatus: boolean;
  riskLevel: RiskLevel | null;
  kycStatus: KycStatus;
  kycReviewedAtUtc: string | null;
  firstName: string | null;
  lastName: string | null;
  middleName: string | null;
  dateOfBirth: string | null;
  gender: Gender | null;
  maritalStatus: MaritalStatus | null;
  education: Education | null;
  ssn: string | null;
  passportNumber: string | null;
  driverLicenseNumber: string | null;
  companyName: string | null;
  registrationNumber: string | null;
  taxId: string | null;
  createdAt: string;
  rowVersion: string;
  addresses: ClientAddressDto[];
  investmentProfile: InvestmentProfileDto | null;
}

export interface CreateClientAddressRequest {
  type: AddressType;
  line1: string;
  line2?: string;
  city: string;
  state?: string;
  postalCode?: string;
  countryId?: string;
}

export interface CreateClientRequest {
  clientType: ClientType;
  externalId?: string;
  status: ClientStatus;
  email: string;
  phone?: string;
  preferredLanguage?: string;
  timeZone?: string;
  residenceCountryId?: string;
  citizenshipCountryId?: string;
  pepStatus: boolean;
  riskLevel?: RiskLevel;
  kycStatus: KycStatus;
  firstName?: string;
  lastName?: string;
  middleName?: string;
  dateOfBirth?: string;
  gender?: Gender;
  maritalStatus?: MaritalStatus;
  education?: Education;
  ssn?: string;
  passportNumber?: string;
  driverLicenseNumber?: string;
  companyName?: string;
  registrationNumber?: string;
  taxId?: string;
  addresses: CreateClientAddressRequest[];
  investmentProfile?: CreateInvestmentProfileRequest;
}

export interface UpdateClientRequest extends CreateClientRequest {
  id: string;
  kycReviewedAtUtc?: string;
  rowVersion: string;
}

// Accounts
export type AccountStatus = "Active" | "Blocked" | "Closed" | "Suspended";
export type AccountType = "Individual" | "Corporate" | "Joint" | "Trust" | "IRA";
export type MarginType = "Cash" | "MarginX1" | "MarginX2" | "MarginX4" | "DayTrader";
export type OptionLevel = "Level0" | "Level1" | "Level2" | "Level3" | "Level4";
export type Tariff = "Basic" | "Standard" | "Premium" | "VIP";
export type DeliveryType = "Paper" | "Electronic";
export type HolderRole = "Owner" | "Beneficiary" | "Trustee" | "PowerOfAttorney" | "Custodian" | "Authorized";

export interface ClearerDto {
  id: string;
  name: string;
  description: string | null;
  isActive: boolean;
}

export interface TradePlatformDto {
  id: string;
  name: string;
  description: string | null;
  isActive: boolean;
}

export interface AccountHolderDto {
  clientId: string;
  clientDisplayName: string;
  role: HolderRole;
  isPrimary: boolean;
  addedAt: string;
}

export interface AccountHolderInput {
  clientId: string;
  role: HolderRole;
  isPrimary: boolean;
}

export interface ClientAccountDto {
  accountId: string;
  accountNumber: string;
  accountStatus: AccountStatus;
  role: HolderRole;
  isPrimary: boolean;
  addedAt: string;
}

export interface ClientAccountInput {
  accountId: string;
  role: HolderRole;
  isPrimary: boolean;
}

export interface AccountDto {
  id: string;
  number: string;
  clearerId: string | null;
  clearerName: string | null;
  tradePlatformId: string | null;
  tradePlatformName: string | null;
  status: AccountStatus;
  accountType: AccountType;
  marginType: MarginType;
  optionLevel: OptionLevel;
  tariff: Tariff;
  deliveryType: DeliveryType | null;
  openedAt: string | null;
  closedAt: string | null;
  comment: string | null;
  externalId: string | null;
  createdAt: string;
  rowVersion: string;
  holders: AccountHolderDto[];
}

export interface AccountListItemDto {
  id: string;
  number: string;
  clearerName: string | null;
  tradePlatformName: string | null;
  status: AccountStatus;
  accountType: AccountType;
  marginType: MarginType;
  optionLevel: OptionLevel;
  tariff: Tariff;
  deliveryType: DeliveryType | null;
  openedAt: string | null;
  closedAt: string | null;
  externalId: string | null;
  createdAt: string;
  rowVersion: string;
  holderCount: number;
}

export interface CreateAccountRequest {
  number: string;
  clearerId?: string;
  tradePlatformId?: string;
  status: AccountStatus;
  accountType: AccountType;
  marginType: MarginType;
  optionLevel: OptionLevel;
  tariff: Tariff;
  deliveryType?: DeliveryType;
  openedAt?: string;
  closedAt?: string;
  comment?: string;
  externalId?: string;
}

export interface UpdateAccountRequest extends CreateAccountRequest {
  id: string;
  rowVersion: string;
}

export interface AccountsParams extends PagedParams {
  number?: string;
  status?: AccountStatus[];
  accountType?: AccountType[];
  marginType?: MarginType[];
  tariff?: Tariff[];
  clearerName?: string;
  tradePlatformName?: string;
  externalId?: string;
}

// Instruments
export type InstrumentType = "Stock" | "Bond" | "ETF" | "Option" | "Future" | "Forex" | "CFD" | "MutualFund" | "Warrant" | "Index";
export type AssetClass = "Equities" | "FixedIncome" | "Derivatives" | "ForeignExchange" | "Commodities" | "Funds";
export type InstrumentStatus = "Active" | "Inactive" | "Delisted" | "Suspended";
export type Sector = "Technology" | "Healthcare" | "Finance" | "Energy" | "ConsumerDiscretionary" | "ConsumerStaples" | "Industrials" | "Materials" | "RealEstate" | "Utilities" | "Communication" | "Other";

export interface ExchangeDto {
  id: string;
  code: string;
  name: string;
  countryId: string | null;
  isActive: boolean;
}

export interface CurrencyDto {
  id: string;
  code: string;
  name: string;
  symbol: string | null;
  isActive: boolean;
}

export interface InstrumentListItemDto {
  id: string;
  symbol: string;
  name: string;
  isin: string | null;
  cusip: string | null;
  type: InstrumentType;
  assetClass: AssetClass;
  status: InstrumentStatus;
  exchangeCode: string | null;
  exchangeName: string | null;
  currencyCode: string | null;
  countryName: string | null;
  countryFlagEmoji: string | null;
  sector: Sector | null;
  lotSize: number;
  isMarginEligible: boolean;
  externalId: string | null;
  createdAt: string;
  rowVersion: string;
}

export interface InstrumentDto {
  id: string;
  symbol: string;
  name: string;
  isin: string | null;
  cusip: string | null;
  type: InstrumentType;
  assetClass: AssetClass;
  status: InstrumentStatus;
  exchangeId: string | null;
  exchangeCode: string | null;
  exchangeName: string | null;
  currencyId: string | null;
  currencyCode: string | null;
  countryId: string | null;
  countryName: string | null;
  countryFlagEmoji: string | null;
  sector: Sector | null;
  lotSize: number;
  tickSize: number | null;
  marginRequirement: number | null;
  isMarginEligible: boolean;
  listingDate: string | null;
  delistingDate: string | null;
  expirationDate: string | null;
  issuerName: string | null;
  description: string | null;
  externalId: string | null;
  createdAt: string;
  rowVersion: string;
}

export interface CreateInstrumentRequest {
  symbol: string;
  name: string;
  isin?: string;
  cusip?: string;
  type: InstrumentType;
  assetClass: AssetClass;
  status: InstrumentStatus;
  exchangeId?: string;
  currencyId?: string;
  countryId?: string;
  sector?: Sector;
  lotSize: number;
  tickSize?: number;
  marginRequirement?: number;
  isMarginEligible: boolean;
  listingDate?: string;
  delistingDate?: string;
  expirationDate?: string;
  issuerName?: string;
  description?: string;
  externalId?: string;
}

export interface UpdateInstrumentRequest extends CreateInstrumentRequest {
  id: string;
  rowVersion: string;
}

export interface InstrumentsParams extends PagedParams {
  symbol?: string;
  name?: string;
  type?: InstrumentType[];
  assetClass?: AssetClass[];
  status?: InstrumentStatus[];
  sector?: Sector[];
  exchangeName?: string;
  currencyCode?: string;
  isMarginEligible?: boolean;
}

export interface ClientsParams extends PagedParams {
  name?: string;
  email?: string;
  phone?: string;
  externalId?: string;
  residenceCountryName?: string;
  citizenshipCountryName?: string;
  status?: ClientStatus[];
  clientType?: ClientType[];
  kycStatus?: KycStatus[];
  riskLevel?: RiskLevel[];
  residenceCountryId?: string;
  residenceCountryIds?: string[];
  citizenshipCountryIds?: string[];
  createdFrom?: string;
  createdTo?: string;
  pepStatus?: boolean;
}

// Entity Change History
export interface FieldChangeDto {
  fieldName: string;
  changeType: "Created" | "Modified" | "Deleted";
  oldValue: string | null;
  newValue: string | null;
}

export interface EntityChangeGroupDto {
  relatedEntityType: string | null;
  relatedEntityId: string | null;
  relatedEntityDisplayName: string | null;
  changeType: string;
  fields: FieldChangeDto[];
}

export interface OperationDto {
  operationId: string;
  timestamp: string;
  userId: string | null;
  userName: string | null;
  entityDisplayName: string | null;
  changeType: string;
  changes: EntityChangeGroupDto[];
}

export interface EntityChangesParams {
  entityType: string;
  entityId: string;
  page?: number;
  pageSize?: number;
}

export interface GlobalOperationDto {
  operationId: string;
  timestamp: string;
  userId: string | null;
  userName: string | null;
  entityType: string;
  entityId: string;
  entityDisplayName: string | null;
  changeType: string;
  changes: EntityChangeGroupDto[];
}

// Dashboard
export interface DashboardStatsDto {
  totalClients: number;
  clientsByStatus: Record<string, number>;
  clientsByType: Record<string, number>;
  totalAccounts: number;
  accountsByStatus: Record<string, number>;
  accountsByType: Record<string, number>;
  totalInstruments: number;
  instrumentsByStatus: Record<string, number>;
  instrumentsByType: Record<string, number>;
  totalUsers: number;
  activeUsers: number;
}

// Settings / Profile
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface UpdateProfileRequest {
  fullName?: string;
  email: string;
}

// Reference data mutations
export interface CreateClearerRequest {
  name: string;
  description?: string;
}

export interface UpdateClearerRequest {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
}

export interface CreateTradePlatformRequest {
  name: string;
  description?: string;
}

export interface UpdateTradePlatformRequest {
  id: string;
  name: string;
  description?: string;
  isActive: boolean;
}

export interface CreateExchangeRequest {
  code: string;
  name: string;
  countryId?: string;
}

export interface UpdateExchangeRequest {
  id: string;
  code: string;
  name: string;
  countryId?: string;
  isActive: boolean;
}

export interface CreateCurrencyRequest {
  code: string;
  name: string;
  symbol?: string;
}

export interface UpdateCurrencyRequest {
  id: string;
  code: string;
  name: string;
  symbol?: string;
  isActive: boolean;
}

export interface AllEntityChangesParams {
  page?: number;
  pageSize?: number;
  sort?: string;
  from?: string;
  to?: string;
  userName?: string[];
  entityType?: string;
  changeType?: string;
  q?: string;
}
