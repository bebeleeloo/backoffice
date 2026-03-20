// Re-export shared types from ui-kit
export {
  type AuthResponse, type UserProfile, type PagedResult, type CountryDto,
  type FieldChangeDto, type EntityChangeGroupDto, type OperationDto,
  type EntityChangesParams, type GlobalOperationDto, type AllEntityChangesParams,
} from "@broker/ui-kit";

// Re-export auth types from auth-module
export {
  type UserDto, type RoleDto, type PermissionDto,
  type CreateUserRequest, type UpdateUserRequest,
  type CreateRoleRequest, type UpdateRoleRequest,
  type UsersParams, type RolesParams,
  type ChangePasswordRequest, type UpdateProfileRequest,
  type SetRolePermissionsRequest,
} from "@broker/auth-module";

// PagedParams re-defined here because local interfaces extend it
export interface PagedParams {
  page?: number;
  pageSize?: number;
  sort?: string;
  q?: string;
}

// AuditLogDto - local definition (has isSuccess field used by backoffice)
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
  rowVersion: number;
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
  rowVersion: number;
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
  rowVersion: number;
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
  rowVersion: number;
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
  rowVersion: number;
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
  rowVersion: number;
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
  rowVersion: number;
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
  rowVersion: number;
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
  rowVersion: number;
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


// Dashboard
export interface DashboardStatsDto {
  totalClients: number;
  clientsByStatus: Record<string, number>;
  clientsByType: Record<string, number>;
  totalAccounts: number;
  accountsByStatus: Record<string, number>;
  accountsByType: Record<string, number>;
  totalOrders: number;
  ordersByStatus: Record<string, number>;
  ordersByCategory: Record<string, number>;
  totalUsers: number;
  activeUsers: number;
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

// Orders
export type OrderCategory = "Trade" | "NonTrade";
export type OrderStatus =
  | "New"
  | "PendingApproval"
  | "Approved"
  | "Rejected"
  | "InProgress"
  | "PartiallyFilled"
  | "Filled"
  | "Completed"
  | "Cancelled"
  | "Failed";
export type TradeSide = "Buy" | "Sell" | "ShortSell" | "BuyToCover";
export type TradeOrderType = "Market" | "Limit" | "Stop" | "StopLimit";
export type TimeInForce = "Day" | "GTC" | "IOC" | "FOK" | "GTD";
export type NonTradeOrderType =
  | "Deposit"
  | "Withdrawal"
  | "Dividend"
  | "CorporateAction"
  | "Fee"
  | "Interest"
  | "Transfer"
  | "Adjustment";

export interface TradeOrderListItemDto {
  id: string;
  accountNumber: string;
  orderNumber: string;
  status: OrderStatus;
  orderDate: string;
  instrumentSymbol: string;
  instrumentName: string;
  side: TradeSide;
  orderType: TradeOrderType;
  timeInForce: TimeInForce;
  quantity: number;
  price: number | null;
  executedQuantity: number;
  averagePrice: number | null;
  commission: number | null;
  executedAt: string | null;
  externalId: string | null;
  createdAt: string;
  rowVersion: number;
}

export interface TradeOrderDto {
  id: string;
  accountId: string;
  accountNumber: string;
  orderNumber: string;
  status: OrderStatus;
  orderDate: string;
  comment: string | null;
  externalId: string | null;
  instrumentId: string;
  instrumentSymbol: string;
  instrumentName: string;
  side: TradeSide;
  orderType: TradeOrderType;
  timeInForce: TimeInForce;
  quantity: number;
  price: number | null;
  stopPrice: number | null;
  executedQuantity: number;
  averagePrice: number | null;
  commission: number | null;
  executedAt: string | null;
  expirationDate: string | null;
  createdAt: string;
  rowVersion: number;
}

export interface NonTradeOrderListItemDto {
  id: string;
  accountNumber: string;
  orderNumber: string;
  status: OrderStatus;
  orderDate: string;
  nonTradeType: NonTradeOrderType;
  amount: number;
  currencyCode: string;
  instrumentSymbol: string | null;
  instrumentName: string | null;
  referenceNumber: string | null;
  processedAt: string | null;
  externalId: string | null;
  createdAt: string;
  rowVersion: number;
}

export interface NonTradeOrderDto {
  id: string;
  accountId: string;
  accountNumber: string;
  orderNumber: string;
  status: OrderStatus;
  orderDate: string;
  comment: string | null;
  externalId: string | null;
  nonTradeType: NonTradeOrderType;
  amount: number;
  currencyId: string;
  currencyCode: string;
  instrumentId: string | null;
  instrumentSymbol: string | null;
  instrumentName: string | null;
  referenceNumber: string | null;
  description: string | null;
  processedAt: string | null;
  createdAt: string;
  rowVersion: number;
}

export interface CreateTradeOrderRequest {
  accountId: string;
  instrumentId: string;
  orderDate: string;
  side: TradeSide;
  orderType: TradeOrderType;
  timeInForce: TimeInForce;
  quantity: number;
  price?: number;
  stopPrice?: number;
  commission?: number;
  expirationDate?: string;
  comment?: string;
  externalId?: string;
}

export interface UpdateTradeOrderRequest {
  id: string;
  accountId: string;
  instrumentId: string;
  orderDate: string;
  status: OrderStatus;
  side: TradeSide;
  orderType: TradeOrderType;
  timeInForce: TimeInForce;
  quantity: number;
  price?: number;
  stopPrice?: number;
  executedQuantity: number;
  averagePrice?: number;
  commission?: number;
  executedAt?: string;
  expirationDate?: string;
  comment?: string;
  externalId?: string;
  rowVersion: number;
}

export interface CreateNonTradeOrderRequest {
  accountId: string;
  orderDate: string;
  nonTradeType: NonTradeOrderType;
  amount: number;
  currencyId: string;
  instrumentId?: string;
  referenceNumber?: string;
  description?: string;
  comment?: string;
  externalId?: string;
}

export interface UpdateNonTradeOrderRequest {
  id: string;
  accountId: string;
  orderDate: string;
  status: OrderStatus;
  nonTradeType: NonTradeOrderType;
  amount: number;
  currencyId: string;
  instrumentId?: string;
  referenceNumber?: string;
  description?: string;
  processedAt?: string;
  comment?: string;
  externalId?: string;
  rowVersion: number;
}

export interface TradeOrdersParams extends PagedParams {
  status?: OrderStatus[];
  side?: TradeSide[];
  orderType?: TradeOrderType[];
  timeInForce?: TimeInForce[];
  accountId?: string[];
  instrumentId?: string[];
  orderNumber?: string;
  externalId?: string;
  orderDateFrom?: string;
  orderDateTo?: string;
  createdFrom?: string;
  createdTo?: string;
  executedFrom?: string;
  executedTo?: string;
  quantityMin?: string;
  quantityMax?: string;
  priceMin?: string;
  priceMax?: string;
  executedQuantityMin?: string;
  executedQuantityMax?: string;
  averagePriceMin?: string;
  averagePriceMax?: string;
  commissionMin?: string;
  commissionMax?: string;
}

export interface NonTradeOrdersParams extends PagedParams {
  status?: OrderStatus[];
  nonTradeType?: NonTradeOrderType[];
  accountId?: string[];
  instrumentId?: string[];
  orderNumber?: string;
  currencyCode?: string;
  referenceNumber?: string;
  externalId?: string;
  orderDateFrom?: string;
  orderDateTo?: string;
  createdFrom?: string;
  createdTo?: string;
  processedFrom?: string;
  processedTo?: string;
  amountMin?: string;
  amountMax?: string;
}

// Transactions
export type TransactionStatus = "Pending" | "Settled" | "Failed" | "Cancelled";

export interface TradeTransactionListItemDto {
  id: string;
  transactionNumber: string;
  orderNumber: string | null;
  accountNumber: string | null;
  status: TransactionStatus;
  transactionDate: string;
  instrumentSymbol: string;
  instrumentName: string;
  side: TradeSide;
  quantity: number;
  price: number;
  commission: number | null;
  settlementDate: string | null;
  venue: string | null;
  externalId: string | null;
  createdAt: string;
  rowVersion: number;
}

export interface TradeTransactionDto {
  id: string;
  orderId: string | null;
  orderNumber: string | null;
  accountNumber: string | null;
  transactionNumber: string;
  status: TransactionStatus;
  transactionDate: string;
  instrumentId: string;
  instrumentSymbol: string;
  instrumentName: string;
  side: TradeSide;
  quantity: number;
  price: number;
  commission: number | null;
  settlementDate: string | null;
  venue: string | null;
  comment: string | null;
  externalId: string | null;
  createdAt: string;
  rowVersion: number;
}

export interface NonTradeTransactionListItemDto {
  id: string;
  transactionNumber: string;
  orderNumber: string | null;
  accountNumber: string | null;
  status: TransactionStatus;
  transactionDate: string;
  amount: number;
  currencyCode: string;
  instrumentSymbol: string | null;
  referenceNumber: string | null;
  processedAt: string | null;
  externalId: string | null;
  createdAt: string;
  rowVersion: number;
}

export interface NonTradeTransactionDto {
  id: string;
  orderId: string | null;
  orderNumber: string | null;
  accountNumber: string | null;
  transactionNumber: string;
  status: TransactionStatus;
  transactionDate: string;
  amount: number;
  currencyId: string;
  currencyCode: string;
  instrumentId: string | null;
  instrumentSymbol: string | null;
  instrumentName: string | null;
  referenceNumber: string | null;
  description: string | null;
  processedAt: string | null;
  comment: string | null;
  externalId: string | null;
  createdAt: string;
  rowVersion: number;
}

export interface CreateTradeTransactionRequest {
  orderId?: string;
  instrumentId: string;
  transactionDate: string;
  side: TradeSide;
  quantity: number;
  price: number;
  commission?: number;
  settlementDate?: string;
  venue?: string;
  comment?: string;
  externalId?: string;
}

export interface UpdateTradeTransactionRequest {
  id: string;
  orderId?: string;
  instrumentId: string;
  transactionDate: string;
  status: TransactionStatus;
  side: TradeSide;
  quantity: number;
  price: number;
  commission?: number;
  settlementDate?: string;
  venue?: string;
  comment?: string;
  externalId?: string;
  rowVersion: number;
}

export interface CreateNonTradeTransactionRequest {
  orderId?: string;
  transactionDate: string;
  amount: number;
  currencyId: string;
  instrumentId?: string;
  referenceNumber?: string;
  description?: string;
  comment?: string;
  externalId?: string;
}

export interface UpdateNonTradeTransactionRequest {
  id: string;
  orderId?: string;
  transactionDate: string;
  status: TransactionStatus;
  amount: number;
  currencyId: string;
  instrumentId?: string;
  referenceNumber?: string;
  description?: string;
  processedAt?: string;
  comment?: string;
  externalId?: string;
  rowVersion: number;
}

export interface TradeTransactionsParams extends PagedParams {
  status?: TransactionStatus[];
  side?: TradeSide[];
  accountId?: string[];
  instrumentId?: string[];
  transactionNumber?: string;
  orderNumber?: string;
  externalId?: string;
  transactionDateFrom?: string;
  transactionDateTo?: string;
  createdFrom?: string;
  createdTo?: string;
  settlementDateFrom?: string;
  settlementDateTo?: string;
  quantityMin?: string;
  quantityMax?: string;
  priceMin?: string;
  priceMax?: string;
  commissionMin?: string;
  commissionMax?: string;
}

export interface NonTradeTransactionsParams extends PagedParams {
  status?: TransactionStatus[];
  accountId?: string[];
  instrumentId?: string[];
  transactionNumber?: string;
  orderNumber?: string;
  currencyCode?: string;
  referenceNumber?: string;
  externalId?: string;
  transactionDateFrom?: string;
  transactionDateTo?: string;
  createdFrom?: string;
  createdTo?: string;
  processedFrom?: string;
  processedTo?: string;
  amountMin?: string;
  amountMax?: string;
}
