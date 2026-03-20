// Shared types used by ui-kit components

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
  hasPhoto: boolean;
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

export interface CountryDto {
  id: string;
  iso2: string;
  iso3: string | null;
  name: string;
  flagEmoji: string;
}

// Audit / Entity Changes
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

// Audit Logs
export interface AuditLogDto {
  id: string;
  userId: string | null;
  userName: string | null;
  action: string;
  path: string;
  method: string;
  statusCode: number;
  entityType: string | null;
  entityId: string | null;
  beforeJson: string | null;
  afterJson: string | null;
  ipAddress: string | null;
  userAgent: string | null;
  correlationId: string | null;
  createdAt: string;
}

export interface AuditParams {
  page?: number;
  pageSize?: number;
  sort?: string;
  from?: string;
  to?: string;
  userName?: string[];
  method?: string;
  statusCode?: number;
  entityType?: string;
  correlationId?: string;
  q?: string;
}

export interface PagedParams {
  page?: number;
  pageSize?: number;
  sort?: string;
  q?: string;
}
