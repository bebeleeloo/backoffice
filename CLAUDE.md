# CLAUDE.md — Broker Backoffice System

## 1. Project Overview

Broker Backoffice — internal admin panel for a brokerage firm. Manages clients, trading accounts, financial instruments, users, roles, permissions, and reference data (clearers, trade platforms, exchanges, currencies). Includes audit logging with field-level change tracking, dashboard analytics, and Excel export.

**Repository structure:**
```
/
├── backend/          # .NET 8 Core API (Clean Architecture + CQRS) — business logic
├── auth-service/     # .NET 8 Auth Service (separate microservice) — users, roles, permissions, auth
├── gateway/          # .NET 8 API Gateway — config, REST proxy, field-level access control
├── frontend/         # pnpm monorepo + Turborepo
│   ├── packages/
│   │   ├── ui-kit/           # @broker/ui-kit — shared components, theme, auth, layout
│   │   └── auth-module/      # @broker/auth-module — login, users, roles pages
│   └── apps/
│       ├── backoffice/       # Business SPA (clients, accounts, orders, etc.) :5173
│       ├── auth/             # Auth SPA (login, users, roles) :5174
│       └── config/           # Config SPA (menu editor, entity fields, upstreams) :5175
├── n8n/              # n8n workflows, import script, test data
│   ├── workflows/    # 3 JSON workflow files (health-check, client-onboarding, transaction-import)
│   ├── test-data/    # Sample CSV for transaction import
│   └── import-workflows.sh  # Automated n8n setup script
├── docs/             # Architecture documentation
├── scripts/          # Test, deployment, and data migration scripts
├── .github/workflows/ci.yml  # GitHub Actions CI pipeline
├── docker-compose.yml         # Services: postgres, auth, api, gateway, web, n8n-db, n8n
├── Dockerfile.api    # Multi-stage .NET build (core, port 8080)
├── Dockerfile.auth   # Multi-stage .NET build (auth service, port 8082)
├── Dockerfile.gateway # Multi-stage .NET build (gateway, port 8090)
├── Dockerfile.web    # Multi-stage pnpm + nginx build (3 SPAs, non-root, port 8080)
└── .env.example
```

## 2. Technology Stack

### Backend
- .NET 8, ASP.NET Core, C# 12
- EF Core 8 (PostgreSQL via Npgsql, Code-First migrations)
- MediatR (CQRS command/query dispatch)
- FluentValidation (request validation via MediatR pipeline)
- Serilog (structured logging with correlation IDs)
- ASP.NET Core Identity password hashing
- JWT Bearer authentication with refresh token rotation
- ASP.NET Core Rate Limiting (fixed window on login, auth, sensitive endpoints)
- ASP.NET Core Response Compression (Gzip, CompressionLevel.Fastest)
- ForwardedHeaders middleware (XForwardedFor + XForwardedProto) in all 3 services for correct client IP/scheme behind reverse proxy

### Frontend
- pnpm monorepo + Turborepo (5 packages: ui-kit, auth-module, backoffice, auth, config)
- 3 separate SPAs: backoffice, auth, config (cross-SPA navigation via NavigationProvider)
- React 18 + TypeScript (strict mode)
- Vite 5 (build tool, dev server)
- MUI v6 (Material UI components + DataGrid)
- React Query / TanStack Query (server state, caching)
- React Router v6 (routing, URL-based filter state, lazy loading)
- Axios (HTTP client with interceptors)
- notistack (snackbar/toast notifications via MutationCache)
- Recharts (dashboard charts)
- ExcelJS + file-saver (Excel export)
- Dayjs (date handling)
- ESLint 9 (flat config, typescript-eslint, react-hooks, react-refresh)

### Infrastructure
- Docker Compose (7 services: postgres, auth, api, gateway, web, n8n-db, n8n) with restart policies, resource limits, log rotation
- PostgreSQL 17
- n8n (workflow automation, separate PostgreSQL DB, connects to api/auth via internal Docker network)
- nginx (frontend reverse proxy + SPA fallback + gzip + security headers + HSTS + cache control)
- GitHub Actions CI (7 jobs: backend build/unit, backend integration, auth-service build/unit, auth-service integration, gateway build/integration, permissions-sync, frontend lint/vitest/build; NuGet/npm caching)

### Testing
- Backend: xUnit, FluentAssertions, NSubstitute, Testcontainers (PostgreSQL)
- Frontend: Vitest, React Testing Library, MSW, @faker-js/faker

## 3. Architecture

```
┌─────────────────┐     ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
│  3 React SPAs    │────▶│  nginx:8080  │────▶│ Gateway:8090 │────▶│  API:8080    │──▶ PostgreSQL
│  backoffice     │     │  SPA routing  │     │  Config, Proxy│     │  Core    │   :5432 (public.*)
│  auth           │     │  /api/ proxy │     │  .NET 8      │     │  .NET 8      │
│  config         │     └──────────────┘     └──────────────┘     └──────────────┘
└─────────────────┘          │                      │
     :3000                   │                      │              ┌──────────────┐
                             │                      └─────────────▶│  Auth:8082   │──▶ PostgreSQL
                             │                                     │  .NET 8      │   :5432 (auth.*)
                             │                                     └──────────────┘
```

**Three services, one database, separate schemas:** Auth service uses `auth.*` schema, core uses `public.*` (default). API Gateway reads YAML configs and proxies REST requests.

**nginx routes 3 SPAs + API:**
- `/login`, `/users*`, `/roles*` → Auth SPA (`auth/index.html`)
- `/config*` → Config SPA (`config/index.html`)
- `/api/` → Gateway (:8090) → routes to core or auth-service
- Everything else → Backoffice SPA (`backoffice/index.html`)

Backend follows Clean Architecture with 4 layers:
- **Domain** — Entities, enums, value objects. Zero dependencies.
- **Application** — CQRS handlers, DTOs, validators, interfaces. Depends only on Domain.
- **Infrastructure** — EF Core, JWT, audit tracking, seeding. Implements Application interfaces.
- **Api** — Controllers, middleware, filters, Program.cs. Composes everything.

Frontend is a pnpm monorepo with 3 SPAs sharing `@broker/ui-kit` and `@broker/auth-module` packages. Cross-SPA navigation via `NavigationProvider` (React Router for internal paths, `window.location.href` for cross-SPA).

**Security headers** (nginx — applied in server block AND all location blocks):
- `Content-Security-Policy`: default-src 'self', unsafe-inline for styles (MUI), data:/blob: for images
- `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `X-XSS-Protection`, `Referrer-Policy`
- `Strict-Transport-Security`: max-age=63072000; includeSubDomains
- `Permissions-Policy`: camera=(), microphone=(), geolocation=()
- `server_tokens off` — hides nginx version
- All 7 headers duplicated in every location block: `/logo.svg`, `/(backoffice|auth|config)/assets/`, `/login`, `/users`, `/roles`, `/config`, `/` (nginx `add_header` in child blocks overrides parent)

**Cache control** (nginx):
- `/(backoffice|auth|config)/assets/*` (Vite hashed files): `Cache-Control: public, immutable`, expires 1y
- `/logo.svg`: `Cache-Control: public, max-age=86400`
- SPA location blocks (`/login`, `/users`, `/roles`, `/config`, `/`): `Cache-Control: no-cache`

**CORS:**
- Configured via `Cors:Origins` in appsettings / env vars (`Cors__Origins__0`, etc.)
- When origins are configured, only those origins are allowed
- Falls back to `AllowAnyOrigin` when no origins configured (dev convenience)
- docker-compose sets `Cors__Origins__0: "http://localhost:3000"`

**Container security:**
- `Dockerfile.web` runs nginx as non-root `nginx` user on port 8080
- docker-compose maps `3000:8080` for the web service

**Middleware pipeline order:**

| # | Backend (Core API) | Auth Service | Gateway |
|---|-------------------|--------------|---------|
| 1 | ForwardedHeaders | ForwardedHeaders | ForwardedHeaders |
| 2 | CorrelationId | CorrelationId | ResponseCompression |
| 3 | ResponseCompression | ResponseCompression | Swagger (if Dev) |
| 4 | SerilogRequestLogging | SerilogRequestLogging | CorrelationId |
| 5 | ExceptionHandling | ExceptionHandling | SerilogRequestLogging |
| 6 | Swagger (if Dev) | Swagger (if Dev) | ExceptionHandling |
| 7 | CORS | CORS | CORS |
| 8 | RateLimiter | RateLimiter | RateLimiter |
| 9 | Authentication | BasicAuth | Authentication |
| 10 | Authorization | Authentication | Authorization |
| 11 | MapControllers | Authorization | MapControllers |
| 12 | Health endpoints | MapControllers | Health endpoints |
| 13 | — | Health endpoints | MapReverseProxy (YARP) |

Notes:
- ForwardedHeaders always first (required for X-Forwarded-For/Proto behind reverse proxy)
- Auth service: BasicAuth middleware after RateLimiter, before Authentication (converts Basic Auth to JSON for n8n)
- Gateway: rate limiting with "config" policy (10 req/min) on config mutation endpoints
- Backend: rate limiting policies defined but only applied in auth-service controllers

**ForwardedHeaders configuration:**
- All 3 services use explicit private network ranges: `10.0.0.0/8`, `172.16.0.0/12`, `192.168.0.0/16`, `127.0.0.0/8`
- `ForwardLimit = 2` (nginx → gateway → service)

## 4. Backend Structure

```
backend/src/
├── Broker.Backoffice.Domain/
│   ├── Common/              # Entity<TId>, AuditableEntity
│   ├── Identity/            # Permissions.cs (string constants only; entities moved to auth-service)
│   ├── Clients/             # Client, ClientAddress, InvestmentProfile + enums
│   ├── Accounts/            # Account, AccountHolder, Clearer, TradePlatform + enums
│   ├── Instruments/         # Instrument, Exchange, Currency + enums
│   ├── Orders/              # Order, TradeOrder, NonTradeOrder + enums
│   ├── Transactions/        # Transaction, TradeTransaction, NonTradeTransaction + enums
│   ├── Audit/               # AuditLog, EntityChange
│   └── Countries/           # Country
│
├── Broker.Backoffice.Application/
│   ├── Abstractions/        # IAppDbContext, ICurrentUser, IAuthServiceClient, IAuditContext
│   ├── Behaviors/           # ValidationBehavior (MediatR pipeline)
│   ├── Common/              # PagedQuery, PagedResult, QueryableExtensions, LikeHelper
│   ├── Clients/             # CRUD commands/queries + DTOs
│   ├── Accounts/            # CRUD commands/queries + DTOs
│   ├── Instruments/         # CRUD commands/queries + DTOs
│   ├── Orders/
│   │   ├── TradeOrders/     # CRUD commands/queries + DTOs for trade orders
│   │   └── NonTradeOrders/  # CRUD commands/queries + DTOs for non-trade orders
│   ├── Transactions/
│   │   ├── TradeTransactions/    # CRUD commands/queries + DTOs for trade transactions
│   │   └── NonTradeTransactions/ # CRUD commands/queries + DTOs for non-trade transactions
│   ├── Clearers/            # CRUD + GetAll / GetActive
│   ├── Currencies/          # CRUD + GetAll / GetActive
│   ├── Exchanges/           # CRUD + GetAll / GetActive
│   ├── TradePlatforms/      # CRUD + GetAll / GetActive
│   ├── AuditLogs/           # GetAuditLogs, GetAuditLogById
│   ├── EntityChanges/       # GetEntityChanges (per entity), GetAllEntityChanges (global)
│   ├── Dashboard/           # GetDashboardStats (calls IAuthServiceClient for user stats)
│   └── Countries/           # GetCountries
│
├── Broker.Backoffice.Infrastructure/
│   ├── Persistence/
│   │   ├── AppDbContext.cs           # EF Core context + change tracking override
│   │   ├── Configurations/           # IEntityTypeConfiguration<T> per entity
│   │   ├── ChangeTracking/           # EntityTrackingRegistry, ChangeTrackingContext
│   │   ├── Migrations/              # EF Core code-first migrations
│   │   ├── SeedData.cs              # Countries, ref data (clearers, trade platforms, currencies, exchanges)
│   │   └── SeedDemoData.cs          # Demo clients, accounts, instruments, orders, transactions
│   ├── Services/                     # AuthServiceClient, CurrentUser, DateTimeProvider
│   ├── Auth/                         # HasPermissionAttribute, PermissionPolicyProvider
│   ├── Middleware/                    # ExceptionHandling, CorrelationId
│   ├── Filters/                      # AuditActionFilter
│   └── DependencyInjection.cs
│
└── Broker.Backoffice.Api/
    ├── Controllers/         # One controller per aggregate
    └── Program.cs           # Composition root
```

### Auth Service Structure

```
auth-service/
├── src/
│   ├── Broker.Auth.Domain/         # Entity, AuditableEntity, Identity/* (User, Role, Permission, etc.)
│   ├── Broker.Auth.Application/    # Auth/, Users/, Roles/, Permissions/, AuditLogs/ handlers
│   ├── Broker.Auth.Infrastructure/ # AuthDbContext (schema: auth), JWT, PasswordHasher, Seed, Configs
│   │   └── Services/RefreshTokenCleanupService.cs  # BackgroundService: cleanup expired tokens (24h interval)
│   └── Broker.Auth.Api/            # Controllers, Middleware (CorrelationId, BasicAuth, ExceptionHandling), Program.cs (:8082)
├── tests/
│   ├── Broker.Auth.Tests.Unit/
│   └── Broker.Auth.Tests.Integration/
└── Dockerfile.auth
```

### API Gateway Structure

```
gateway/
├── src/
│   └── Broker.Gateway.Api/
│       ├── Controllers/ConfigController.cs    # Menu, entities, upstreams endpoints + CRUD
│       ├── Services/
│       │   ├── ConfigLoader.cs                # YAML loading/saving (YamlDotNet), FileSystemWatcher, debounce
│       │   ├── YamlProxyConfigProvider.cs      # YARP IProxyConfigProvider — dynamic route/cluster config
│       │   ├── MenuService.cs                 # Menu filtering by user permissions
│       │   └── EntityConfigService.cs         # Entity field filtering by role
│       ├── Config/                            # YAML model classes (MenuItemConfig, EntityConfig, UpstreamConfig)
│       ├── Middleware/CorrelationIdMiddleware.cs
│       └── Program.cs                         # :8090, YARP, CORS, JWT auth
├── config/
│   ├── menu.yaml        # Sidebar menu structure + permissions
│   ├── entities.yaml    # Entity field visibility by role
│   └── upstreams.yaml   # Backend service routing
├── tests/
│   └── Broker.Gateway.Tests.Integration/  # Integration tests (Testcontainers)
└── Dockerfile.gateway
```

API Gateway serves config (menu, entities, upstreams), proxies REST via YARP to backend services, and provides CRUD endpoints for config editing (`settings.manage` permission). YARP routes reload dynamically when `upstreams.yaml` changes or `POST /reload` is called (`ConfigLoader.OnUpstreamsChanged` event → `YamlProxyConfigProvider.Update()`). Menu PUT validates recursion depth (max 3 levels). `ConfigLoader.OnUpstreamsChanged` is invoked outside lock to prevent deadlocks. Middleware order: CorrelationId → ExceptionHandling → routing.

Auth service owns: users, roles, permissions, authentication (login, refresh, logout, change password, reset password, profile), user photos. Includes `RefreshTokenCleanupService` (BackgroundService) that deletes expired/revoked refresh tokens every 24 hours (30-day retention). `BasicAuthMiddleware` converts Basic Auth headers to JSON body on `/auth/login` for n8n integration (logs warning on non-HTTPS). Login performs minimal query first, verifies password (with dummy hash for missing users to prevent timing oracle), then lazy-loads full user graph only on success. Logout endpoint (`POST /auth/logout`) is idempotent — returns 204 even if token not found. Password change, reset, user deactivation, and user deletion all revoke active refresh tokens.

Core validates JWT locally (no roundtrip to auth). Dashboard gets user stats via `IAuthServiceClient` → `GET /api/v1/users/stats` (`[AllowAnonymous]` — internal service-to-service call).

### n8n Workflow Automation

n8n 2.12.3 runs as a Docker service with its own PostgreSQL database. Connects to Broker API via gateway (`http://gateway:8090`). Uses Basic Auth credentials (converted to JWT by `BasicAuthMiddleware` in auth-service).

**3 workflows** (`n8n/workflows/`):
1. **Broker Health Check** (`health-check.json`) — Cron every 5 minutes, checks `/health/ready` on gateway for both API and auth service, reports healthy/unhealthy status
2. **Client Onboarding** (`client-onboarding.json`) — Webhook-triggered, creates client with `kycStatus: InProgress`, generates approval/rejection URLs via `$execution.resumeUrl`, waits for manual approval, then updates kycStatus to Approved/Rejected with optimistic concurrency (rowVersion)
3. **Transaction Import** (`transaction-import.json`) — Webhook accepts raw CSV body, parses rows, checks for duplicates via `externalId`, looks up accounts/instruments/currencies, creates trade or non-trade transactions, returns summary with imported/skipped counts

**Import script** (`n8n/import-workflows.sh`):
- Waits for n8n container health (30s timeout)
- Creates "Broker API Auth" credential (httpBasicAuth, uses `BROKER_API_USERNAME` / `ADMIN_PASSWORD` env vars)
- Imports all workflows via `n8n import:workflow` CLI
- Activates all 3 workflows and restarts n8n container

**Test data** (`n8n/test-data/transactions.csv`): 3 sample rows (2 trade, 1 non-trade) for testing the import workflow.

**Demo data seeding** (controlled by `SEED_DEMO_DATA=true` env var or `ASPNETCORE_ENVIRONMENT=Development`):
- Auth service seeds: 10 demo users (3 Managers, 3 Viewers, 4 Operators), 3 demo roles, portrait photos from randomuser.me
- Core seeds: 100 clients, 150 accounts, 891 instruments, 2001 orders, transactions

### Key Backend Conventions

**Entity base classes:**
- `Entity<TId>` — Abstract generic base with ID and equality by ID
- `AuditableEntity` — Extends `Entity<Guid>`, adds CreatedAt/By, UpdatedAt/By, RowVersion
- Aggregate roots (Client, Account, Instrument, Order, Transaction, User, Role) inherit `AuditableEntity`
- Reference entities (Country, Currency, Exchange, Clearer, TradePlatform) have no base class

**CQRS file organization:**
- One file per command/query containing: record, validator, handler
- Example: `CreateClient.cs` contains `CreateClientCommand`, `CreateClientCommandValidator`, `CreateClientCommandHandler`
- Commands are `sealed record` implementing `IRequest<TResponse>`
- Handlers are `internal sealed class` implementing `IRequestHandler<TCommand, TResponse>`

**DTO conventions:**
- Separate `ListItemDto` (grid) and `Dto` (detail) per aggregate
- `sealed record` for all DTOs
- Manual mapping via static `ToDto()` methods or inline LINQ `.Select()`
- No AutoMapper

**Pagination:**
- List queries extend `PagedQuery` (Page, PageSize, Sort, Q)
- `PagedQuery` clamps values: Page min 1, PageSize 1–10000 (protects against OOM)
- Return `PagedResult<T>` with Items, TotalCount, Page, PageSize, TotalPages
- `QueryableExtensions.ToPagedResultAsync()` handles Skip/Take/Count
- `QueryableExtensions.SortBy()` handles dynamic sorting via expression trees; supports `"field asc"`/`"field desc"` and legacy `-field` formats
- For computed/navigation property fields (e.g. DisplayName, ClearerName, ExchangeCode), query handlers use private `ApplySort()` methods with explicit switch cases instead of generic `SortBy()`

**Filtering conventions:**
- Text: `EF.Functions.Like(field, LikeHelper.ContainsPattern(value))` — escapes `%`, `_`, `\` wildcards via `LikeHelper` in `Application/Common/LikeHelper.cs`
- Multi-value enum: `request.Status.Contains(entity.Status)`
- Date range: `>= from`, `< to.AddDays(1)` (inclusive end date)
- Numeric range: `>= min`, `<= max` (optional min/max)
- Boolean: `== value`
- Global search `Q`: searches multiple text fields with OR

**Error handling:**
- `KeyNotFoundException` → 404
- `UnauthorizedAccessException` → 401
- `InvalidOperationException` → 409 (business rule violation, uniqueness)
- `ValidationException` (FluentValidation) → 400 with grouped errors
- `DbUpdateConcurrencyException` → 409
- All others → 500
- Response format: RFC 7807 ProblemDetails

**Rate limiting:**
- ASP.NET Core built-in rate limiter (no external packages)
- Fixed window policy "login": 5 requests per 1 minute per client (configurable via `RateLimiting:LoginPermitLimit`)
- Fixed window policy "auth": 20 requests per 1 minute (refresh token, update profile)
- Fixed window policy "sensitive": 5 requests per 5 minutes (change password)
- Applied via `[EnableRateLimiting("policy")]` on AuthController methods
- Returns 429 Too Many Requests when exceeded
- Integration tests override limit to 10000 via `UseSetting`

**Password policy:**
- Minimum 8 characters, must contain: uppercase letter, lowercase letter, digit, special character
- Enforced in FluentValidation on: CreateUser, ChangePassword, ResetUserPassword
- On password change/reset: all user's refresh tokens are revoked (`RevokedAt = utcNow`)
- On user deactivation (UpdateUser, `IsActive` true→false): all refresh tokens are revoked
- On user deletion (DeleteUser): all refresh tokens are revoked before removal

**Pre-delete referential integrity checks:**
- DeleteAccount: rejects if account has linked Orders (`InvalidOperationException` → 409)
- DeleteInstrument: rejects if instrument has linked TradeOrders, TradeTransactions, or NonTradeTransactions
- DeleteTradeOrder / DeleteNonTradeOrder: rejects if order has linked Transactions
- DeleteClient: rejects if client has linked Accounts (via AccountHolder)

**Swagger:**
- Enabled only in Development environment (`app.Environment.IsDevelopment()`)
- Disabled in production for all 3 services (backend, auth-service, gateway)

**User photos:**
- Stored as binary in DB (Photo byte[], PhotoContentType varchar(50))
- Endpoints: `GET/PUT/DELETE /users/{id}/photo` and `GET/PUT/DELETE /auth/photo`
- `GET /users/{id}/photo` is `[AllowAnonymous]` — required because `<img src>` cannot send JWT Authorization headers
- `GET /auth/photo` is `[Authorize]` — own photo requires authentication
- PUT photo accepts `IFormFile` multipart upload, max 2 MB, validates MIME type (jpeg/png/gif/webp)
- PUT photo controllers validate `IFormFile` null/empty before processing (returns 400 ProblemDetails)
- Returns raw image bytes with `Content-Type` header (not base64 in JSON)
- `Cache-Control: private, max-age=3600` on GET response
- Photo/PhotoContentType excluded from audit change tracking (`EntityTrackingRegistry`)
- Photo upload/delete handlers set `IAuditContext` EntityType + EntityId (no BeforeJson/AfterJson for binary data)
- Demo data seeds portrait photos from randomuser.me for all users

**Concurrency control:**
- `RowVersion` uint on AuditableEntity (mapped to PostgreSQL xmin system column)
- Passed from client, set as OriginalValue before SaveChanges
- EF Core throws DbUpdateConcurrencyException on mismatch

## 5. Frontend Structure

```
frontend/
├── packages/
│   ├── ui-kit/                          # @broker/ui-kit — shared across all SPAs
│   │   └── src/
│   │       ├── components/              # PageContainer, FilteredDataGrid, ConfirmDialog, UserAvatar,
│   │       │                            # Breadcrumbs, DetailField, ExportButton, GlobalSearchBar,
│   │       │                            # ErrorBoundary, RouteLoadingFallback, EntityHistoryDialog,
│   │       │                            # AuditDetailDialog, ChangeHistoryComponents, grid/*
│   │       ├── layouts/MainLayout.tsx   # Dark collapsible sidebar + content area
│   │       ├── navigation/             # NavigationProvider, useAppNavigation (cross-SPA nav)
│   │       ├── auth/                   # AuthProvider, useAuth, useHasPermission, RequireAuth
│   │       ├── api/                    # Axios client, configApi (useMenu, useEntityConfig)
│   │       ├── theme/                  # createAppTheme, ThemeContext, SIDEBAR_COLORS, STAT_GRADIENTS
│   │       ├── hooks/                  # useDebounce, useConfirm
│   │       ├── utils/                  # exportToExcel, extractErrorMessage, validateFields
│   │       └── icons.ts               # iconMap: string → MUI Icon component
│   │
│   └── auth-module/                     # @broker/auth-module — auth pages
│       └── src/
│           ├── pages/                  # LoginPage, UsersPage, UserDialogs, RolesPage,
│           │                           # RoleDetailsPage, RoleDialogs, ProfileTab
│           └── api/                    # types.ts, hooks.ts (useUsers, useRoles, useLogin, etc.)
│
├── apps/
│   ├── backoffice/                      # Business SPA (:5173)
│   │   └── src/
│   │       ├── pages/                  # Dashboard, Clients, Accounts, Instruments, Orders,
│   │       │                           # Transactions, Audit, Settings, NotFoundPage
│   │       ├── api/                    # types.ts, hooks.ts (useClients, useAccounts, etc.)
│   │       └── router/index.tsx        # Routes wrapped in NavigationProvider
│   │
│   ├── auth/                            # Auth SPA (:5174)
│   │   └── src/
│   │       ├── pages/NotFoundPage.tsx
│   │       └── router/index.tsx        # /login, /users, /roles (from @broker/auth-module)
│   │
│   └── config/                          # Config SPA (:5175)
│       └── src/
│           ├── pages/                  # ConfigDashboardPage, MenuEditorPage,
│           │                           # EntityFieldsPage, UpstreamsPage, NotFoundPage
│           ├── api/                    # types.ts, hooks.ts (useMenuRaw, useSaveMenu, etc.)
│           └── router/index.tsx        # /config, /config/menu, /config/entities, /config/upstreams
│
├── nginx.conf                           # Shared nginx routing 3 SPAs + API proxy
├── pnpm-workspace.yaml
├── turbo.json
└── tsconfig.base.json
```

### Key Frontend Conventions

**Page pattern (all list pages follow this):**
1. `useSearchParams()` → `readParams(sp)` to parse URL state
2. React Query hook for paginated data
3. `columns: GridColDef[]` with typed row
4. `filterDefs: Map<string, () => ReactNode>` mapping field → filter component
5. `sortModel: GridSortModel` derived from URL `sort` param via `useMemo`, passed to `FilteredDataGrid`
6. `exportColumns: ExcelColumn<T>[]` for Excel export
7. `fetchAll: () => Promise<T[]>` fetches with pageSize=10000 for export
8. Permission-gated action buttons via `useHasPermission()`
9. `PageContainer` wrapper with variant="list" for compact theme

**Detail page pattern:**
- `PageContainer` with `breadcrumbs` prop for navigation (e.g., Clients > John Doe)
- `Breadcrumbs` component with `BreadcrumbItem[]` — last item is text, others are RouterLinks
- No Back button — breadcrumbs replace it
- `DetailField` component for label+value pairs (uppercase labels, auto-hides when value is null/undefined/empty)
- Order/Transaction detail pages show status tooltip on hover (descriptions from `orderConstants.ts` / `transactionConstants.ts`)
- AccountDetailsPage has "Trade Order" / "Non-Trade Order" buttons (gated by `orders.create`) that open create dialogs with account pre-populated
- Order detail pages have Trade/Non-Trade Transaction sections with create buttons (gated by `transactions.create`)

**List page UX:**
- `FilteredDataGrid` shows `CustomNoRowsOverlay` (SearchOffIcon + "No results found") when grid is empty
- All list pages have a "Clear all filters" icon button (FilterListOffIcon) when any filter is active
- Clear filters resets URL search params: `setSearchParams(new URLSearchParams())`
- **Page-level History button** (outlined, HistoryIcon) in PageContainer actions — navigates to `/audit?entityType=...` (gated by `audit.read`). Present on: Clients, Accounts, Instruments, TradeOrders, NonTradeOrders, TradeTransactions, NonTradeTransactions, Roles
- **Per-row History button** (HistoryIcon) in actions column — opens `EntityHistoryDialog` for specific entity (gated by `audit.read`). Present on all list pages including Users

**API client:**
- Base URL: `VITE_API_URL` env var, defaults to `/api/v1`
- Automatic Bearer token from localStorage
- Correlation ID header on every request
- 401 interceptor: refresh token → retry, on failure → remove auth tokens and redirect to /login
- Token cleanup uses targeted `localStorage.removeItem("accessToken"/"refreshToken")` (not `localStorage.clear()`) to preserve user preferences (theme, sidebar state)
- `cleanParams()` strips undefined/null/empty values before sending

**React Query conventions:**
- Query keys: `["entity", params]` for lists, `["entity", id]` for details
- Mutations invalidate related query keys
- Mutation hooks include `meta: { successMessage: "..." }` for global toast notifications
- `useLogin` uses `meta: { skipErrorToast: true }` (LoginPage shows errors inline)
- Global `MutationCache` in main.tsx handles `onError` (error toast) and `onSuccess` (success toast)
- Reference data (countries, clearers, etc.): `staleTime: 10 * 60 * 1000`
- Auth: `useMe(enabled)` re-fetches user profile

**Toast notifications (notistack):**
- `SnackbarProvider` wraps app in main.tsx (maxSnack=3, bottom-right, 4s auto-hide)
- Errors: automatic via `MutationCache.onError` → `extractErrorMessage()` parses ProblemDetails
- Success: automatic via `MutationCache.onSuccess` when `meta.successMessage` is set
- Dialog `handleSubmit` uses `try/catch` to prevent unhandled rejections (error toast via MutationCache)
- Client-side validation errors: call `enqueueSnackbar()` directly

**Confirmation dialogs:**
- `ConfirmDialog` component: MUI Dialog with Cancel + red Delete button, `isLoading` prop
- `useConfirm()` hook: returns `{ confirm, confirmDialogProps }`, `confirm()` returns `Promise<boolean>`
- All delete actions in list pages use `useConfirm()` instead of native `confirm()`

**Inline form validation:**
- `validateFields.ts` provides `validateRequired()` and `validateEmail()` helpers
- Type: `FieldErrors = Record<string, string | undefined>`
- Pattern: `const [errors, setErrors] = useState<FieldErrors>({})` in each dialog
- On submit: validate required fields, if errors → `setErrors(errs); return;`
- On TextFields: `error={!!errors.fieldName}` + `helperText={errors.fieldName}`
- On change: `setErrors(prev => ({ ...prev, fieldName: undefined }))` clears error for that field
- No Zod/Yup — simple inline validation with MUI error/helperText props

**Dialog state reset pattern (prevOpen):**
- Dialogs reset form state when `open` transitions from false → true
- Uses render-time state comparison (NOT `useEffect`) to avoid ESLint `react-hooks/set-state-in-effect` rule
- Pattern: `const [prevOpen, setPrevOpen] = useState(open); if (open && !prevOpen) { setForm(empty()); setErrors({}); } if (open !== prevOpen) setPrevOpen(open);`
- Applied in all Create/Edit dialogs across Clients, Accounts, Instruments, Orders, Transactions

**Route lazy loading:**
- Page components loaded via `React.lazy()` with `.then(m => ({ default: m.PageName }))` for named exports
- `withSuspense()` helper wraps each lazy route in `<Suspense fallback={<RouteLoadingFallback />}>`
- Eager-loaded: `LoginPage`, `MainLayout`, `RequireAuth`, `NotFoundPage`
- Lazy-loaded: all 20 authenticated page routes (Dashboard, Clients, Accounts, Trade Orders, Non-Trade Orders, Trade Transactions, Non-Trade Transactions, etc.)

**Theme & visual design:**
- Fintech professional style with Teal/Emerald palette
- Primary: `#0D9488` (teal-600), Secondary: `#059669` (emerald-600)
- `SIDEBAR_COLORS` exported from `theme/index.ts` — dark sidebar tokens (bg: `#0F172A`, text: `#CBD5E1`, active indicator: teal)
- `STAT_GRADIENTS` exported from `theme/index.ts` — 4 teal/emerald gradients for dashboard stat cards
- Gradient primary buttons (`containedPrimary`), rounded cards (12px), subtle shadows
- `AppThemeProvider` in `theme/ThemeContext.tsx` wraps the app (above SnackbarProvider in main.tsx)
- Preference stored in `localStorage` key `"themeMode"`: `"light"` | `"dark"` | `"system"` (default: `"light"`)
- `"system"` follows OS via `prefers-color-scheme` media query listener
- `useThemeMode()` — read/write preference and resolved mode
- `useListTheme()` — get scoped list theme (replaces static `listTheme` import)
- `createAppTheme(mode)` / `createAppListTheme(base)` in `theme/index.ts` — factory functions
- Settings > Appearance tab (`AppearanceTab.tsx`) — ToggleButtonGroup with Light/Dark/System

**Cross-SPA navigation:**
- 3 SPAs (backoffice, auth, config) share same origin — JWT tokens in localStorage persist across SPAs
- `NavigationProvider` wraps MainLayout in each SPA with `internalPaths: string[]`
- `useAppNavigation().navigateTo(path)` — if path matches internalPaths → React Router `navigate()`, else → `window.location.href` (full page reload)
- Sidebar clicks use `navigateTo()` for automatic internal/external routing
- Logout calls `POST /auth/logout` (best-effort server-side token revocation), clears localStorage, then uses `window.location.href = "/login"` (cross-SPA to auth app)
- `RequireAuth` redirects via `useEffect` → `window.location.href = "/login?returnTo=" + encodeURIComponent(path)`
- `LoginPage` reads `returnTo` from URL search params, redirects after login via `window.location.href`
- nginx routes: `/login`, `/users*`, `/roles*` → auth SPA; `/config*` → config SPA; rest → backoffice SPA

**Sidebar / Layout:**
- No AppBar — sidebar is the only navigation element
- Dark sidebar (`#0F172A`) persists in both light and dark mode
- Collapsible: 260px (expanded) ↔ 72px (collapsed, icons + tooltips)
- Collapse state persisted in `localStorage` key `"sidebarCollapsed"`
- Collapse toggle: ChevronLeft/ChevronRight button below logo
- Menu items: rounded (borderRadius 1.5), left teal border on active, hover highlight
- Sub-menus (Orders, Transactions): collapse in expanded mode, navigate to first child in collapsed mode
- User section: `UserAvatar` component (photo or initial) + name + logout; in collapsed mode only avatar with tooltip
- Mobile: floating hamburger button (fixed, top-left), temporary drawer always expanded
- ErrorBoundary wraps `<Outlet />` — page crashes don't break sidebar

**User avatars:**
- `UserAvatar` component (`components/UserAvatar.tsx`): reusable MUI `<Avatar>` wrapper
- Props: `userId`, `name`, `hasPhoto`, `size?`, `sx?`
- When `hasPhoto`: renders `<Avatar src="/api/v1/users/{id}/photo">` (anonymous endpoint, no auth needed)
- Fallback: first letter of name (MUI Avatar default behavior when src fails)
- Used in: sidebar (36px), users list DataGrid (32px), edit user dialog (64px), profile settings (96px)
- Photo upload/delete available in: profile settings (own photo via `/auth/photo`), edit user dialog (via `/users/{id}/photo`)

**Login page:**
- Split-screen: dark gradient left panel (45%, hidden on mobile) + form right panel (55%)
- Left panel: logo, "Broker Backoffice" title, "Internal Management System" subtitle, decorative circles
- Right panel: "Welcome back" heading, username/password fields, gradient Sign In button (InputLabelProps shrink=true on TextFields)
- Mobile: only form panel with compact logo header

**State management:**
- Server state: React Query (no Redux/Zustand)
- URL state: `useSearchParams` for filters/pagination/sort
- Local state: `useState` for dialogs, form inputs

**URL filter conventions:**
- Single values: `?status=Active&page=1`
- Multi-values: `?type=Stock&type=Bond` (repeated keys)
- Sort: `?sort=name asc` or `?sort=createdAt desc`
- Date range: `?createdFrom=2024-01-01&createdTo=2024-12-31`

## 6. Domain Model

### Aggregates

**Client** (AuditableEntity) — Individual or Corporate client
- Owns: ClientAddress[] (cascade), InvestmentProfile? (cascade)
- References: ResidenceCountry, CitizenshipCountry
- Linked via: AccountHolder → Account

**Account** (AuditableEntity) — Trading account
- Owns: AccountHolder[] (cascade, composite key: AccountId+ClientId+Role)
- References: Clearer?, TradePlatform?

**Instrument** (AuditableEntity) — Financial instrument (Stock, Bond, ETF, etc.)
- References: Exchange?, Currency?, Country?

**Order** (AuditableEntity) — Base order aggregate (Trade or NonTrade)
- Owns: TradeOrder? (cascade), NonTradeOrder? (cascade)
- References: Account
- Fields: OrderNumber (unique), Category, Status, OrderDate, Comment, ExternalId
- Children: TradeOrder (Side, OrderType, TimeInForce, Quantity, Price, StopPrice, etc.)
- Children: NonTradeOrder (NonTradeType, Amount, CurrencyId, InstrumentId?, ReferenceNumber, etc.)

**Transaction** (AuditableEntity) — Base transaction aggregate (Trade or NonTrade)
- Owns: TradeTransaction? (cascade), NonTradeTransaction? (cascade)
- References: Order? (optional FK), Instrument
- Fields: TransactionNumber (unique), Status, TransactionDate, Comment, ExternalId
- Children: TradeTransaction (Side, Quantity, Price, Commission, SettlementDate, Venue)
- Children: NonTradeTransaction (Amount, CurrencyId, InstrumentId?, ReferenceNumber, Description, ProcessedAt)

**User** (AuditableEntity) — System user
- Owns: UserRole[], UserPermissionOverride[], UserRefreshToken[], DataScope[]
- Photo: binary storage (Photo byte[], PhotoContentType string) — stored in DB, served as raw image bytes

**Role** (AuditableEntity) — Authorization role
- Owns: RolePermission[]

**Permission** (AuditableEntity) — Granular permission
- Static list defined in `Permissions.cs`

### Reference Entities (no base class, CRUD via Settings)
- Country (300+ seeded, ISO2/ISO3/FlagEmoji)
- Currency (15 major currencies seeded)
- Exchange (15 exchanges seeded)
- Clearer (4 seeded)
- TradePlatform (4 seeded)

### Enums
All enums use `[JsonConverter(typeof(JsonStringEnumConverter))]` for string serialization.

## 7. CQRS Patterns

### Command pattern
```csharp
// File: CreateClient.cs — contains all three
public sealed record CreateClientCommand(...) : IRequest<ClientDto>;
public sealed class CreateClientCommandValidator : AbstractValidator<CreateClientCommand> { }
internal sealed class CreateClientCommandHandler(IAppDbContext db, ...) : IRequestHandler<CreateClientCommand, ClientDto> { }
```

### Query pattern
```csharp
public sealed record GetClientsQuery : PagedQuery, IRequest<PagedResult<ClientListItemDto>>
{
    public List<ClientStatus>? Status { get; init; }
    // ...filter properties
}
```

### Pipeline
Request → ValidationBehavior (FluentValidation) → Handler → Response

### Handlers access `IAppDbContext` directly
No repository layer. All data access via DbContext DbSets with LINQ.

### Mutation pattern
1. Validate (automatic via pipeline, including business rules like Price required for Limit orders)
2. Check existence (`?? throw new KeyNotFoundException`)
3. Check FK references exist — **always** validate via `AnyAsync` (Account, Instrument, Currency etc. → `KeyNotFoundException`), even if value hasn't changed from existing entity
4. Check cross-entity consistency (`throw new InvalidOperationException` if mismatch, e.g. trade transaction Side must match order Side)
5. Check uniqueness (`throw new InvalidOperationException` if duplicate)
6. For deletes: check no child entities reference this aggregate (`throw new InvalidOperationException` → 409)
7. Set audit context BeforeJson (for updates/deletes — capture state before modification)
8. Modify entity
9. SaveChangesAsync (triggers change tracking)
10. Set audit context EntityType, EntityId, AfterJson (for creates/updates — capture state after save)
11. Return `await mediator.Send(new GetXxxByIdQuery(...), ct)` — re-fetch via mediator, never instantiate handlers directly

Note: All mutation handlers (aggregates, reference data, photo, profile) must inject `IAuditContext` and set at minimum EntityType + EntityId. Reference data handlers set BeforeJson/AfterJson directly since they have no field-level change tracking.

## 8. Permission Model

### 31 permissions in 10 groups:
| Group | Permissions |
|-------|------------|
| Users | users.read, users.create, users.update, users.delete |
| Roles | roles.read, roles.create, roles.update, roles.delete |
| Permissions | permissions.read |
| Audit | audit.read |
| Clients | clients.read, clients.create, clients.update, clients.delete |
| Accounts | accounts.read, accounts.create, accounts.update, accounts.delete |
| Instruments | instruments.read, instruments.create, instruments.update, instruments.delete |
| Orders | orders.read, orders.create, orders.update, orders.delete |
| Transactions | transactions.read, transactions.create, transactions.update, transactions.delete |
| Settings | settings.manage |

### Authorization flow:
1. Login → resolve effective permissions (role perms + user overrides)
2. JWT includes `permission` claims (one per permission code)
3. Controller methods decorated with `[HasPermission(Permissions.XxxYyy)]`
4. `PermissionPolicyProvider` creates dynamic authorization policies
5. `PermissionAuthorizationHandler` checks claim existence

### Frontend:
- `useHasPermission("clients.create")` returns boolean
- UI elements conditionally rendered based on permissions

## 9. Audit Logging

### Two-level system:

**Level 1: AuditLog (request-level)**
- Captured by `AuditActionFilter` on mutating HTTP methods
- Records: user, action, path, method, status code, before/after JSON, IP, user agent, correlation ID
- All mutation handlers set `IAuditContext` (EntityType, EntityId, BeforeJson/AfterJson) so AuditLog rows identify the affected entity
- Aggregate root handlers (Client, Account, etc.) and reference data handlers (Clearer, Currency, Exchange, TradePlatform) all set audit context
- Photo upload/delete handlers set EntityType + EntityId only (no BeforeJson/AfterJson for binary data)
- ChangePassword sets EntityType + EntityId only (password hash must not appear in logs)

**Level 2: EntityChange (field-level)**
- Captured in `AppDbContext.SaveChangesAsync()` override via EF Core ChangeTracker
- Records: operationId, entityType, entityId, displayName, changeType, fieldName, oldValue, newValue
- Grouped by operationId for atomic operations
- Deduplicates "delete + recreate" patterns for child entities (addresses, holders)
- Tracked entities configured in `EntityTrackingRegistry` (Client, Account, Instrument, Order, Transaction, User, Role + children)
- Reference data entities (Clearer, Currency, Exchange, TradePlatform) are NOT in EntityTrackingRegistry — they only get Level 1 AuditLog rows with BeforeJson/AfterJson
- Excludes: RowVersion, timestamps, PasswordHash, navigation properties

## 10. Coding Conventions

### C# / Backend
- `sealed record` for commands, queries, DTOs
- `internal sealed class` for handlers
- Primary constructors for DI injection
- `CancellationToken ct` as last parameter in all async methods
- `string?` nullable reference types enabled
- Naming: PascalCase for everything, `I` prefix for interfaces
- No regions, no partial classes (except Program for test factory)
- One logical unit per file (command + validator + handler together)

### TypeScript / Frontend
- Strict TypeScript, no `any`
- `interface` for object shapes, string literal unions for enums
- `useMemo` for computed values (columns, filterDefs, exportColumns)
- `useCallback` for handlers passed as props
- Named exports only (no default exports)
- Files: PascalCase for components, camelCase for utilities
- ESLint 9 flat config (`eslint.config.js`): TS recommended + react-hooks + react-refresh
- 404 wildcard route inside authenticated layout shows NotFoundPage

**Dashboard:**
- 4 gradient stat cards (teal/emerald gradients, white text, hover lift effect) — Clients, Accounts, Orders, Users
- Each card links to its list page via `CardActionArea`
- 4 charts: 3 status pie charts (Recharts) + 1 category bar chart
- Status colors: emerald (Active), red (Blocked/Rejected/Failed), amber (Pending*), cyan (InProgress/New), gray (Closed/Cancelled)
- Responsive grid: 1→2→4 columns for stat cards, 1→2 for charts

### Shared
- API route format: `/api/v1/{entity}` (kebab-case for multi-word: `entity-changes`, `trade-platforms`)
- JSON property names: camelCase
- IDs: UUID/Guid
- Dates: ISO 8601 strings in JSON, DateTime in C#, string in TypeScript
- Enums: string serialization both sides

## 11. Performance Patterns

- Server-side pagination on all list endpoints (never load full datasets to grid)
- EF Core `.Select()` projections in queries (no full entity materialization for lists)
- Composite database indexes on frequently filtered columns (Category+Status, Side+OrderType)
- Date column indexes: `Order.OrderDate`, `Transaction.TransactionDate`, `AuditLog.CorrelationId`
- React Query caching with appropriate stale times
- Reference data cached 10 minutes (countries, clearers, exchanges, currencies)
- Debounced text filters (300ms) to avoid excessive API calls
- Multi-stage Docker builds for minimal image size
- Separate ListItemDto (grid) and Dto (detail) to minimize payload
- Gzip compression: nginx (`gzip on`, min_length 1024, comp_level 5) + backend (`AddResponseCompression` with `GzipCompressionProvider`)
- Route-level code splitting via `React.lazy()` — each page loads as a separate chunk
- nginx cache headers: immutable 1y for hashed `/assets/*`, no-cache for HTML/SPA routes

## 12. Testing Strategy

### Core Unit Tests (273 tests, ~2s)

- xUnit with `[Fact]` and `[Theory]`/`[InlineData]`
- FluentValidation.TestHelper for validators
- NSubstitute for mocking interfaces
- Location: `backend/tests/Broker.Backoffice.Tests.Unit/`
- Validators covered: Clients (Create/Update, SetAccounts), Accounts (Create/Update, SetHolders), Instruments (Create/Update), Orders (TradeOrder Create/Update, NonTradeOrder Create/Update), Transactions (TradeTransaction Create/Update, NonTradeTransaction Create/Update), Reference data (Clearer, Currency, Exchange, TradePlatform — Create/Update each)

### Auth Service Unit Tests (49 tests, ~1s)

- Same stack: xUnit, FluentValidation.TestHelper, NSubstitute
- Location: `auth-service/tests/Broker.Auth.Tests.Unit/`
- Validators covered: Auth (Login, Logout, ChangePassword, UpdateProfile), Users (Create/Update), Roles (Create/Update, FullName MaxLength)

### Core Integration Tests (156 tests, ~10s)
- Testcontainers (real PostgreSQL 17 in Docker)
- `CustomWebApplicationFactory` extends `WebApplicationFactory<Program>`
- `IntegrationTestBase` uses `TestJwtTokenHelper` to generate JWT tokens directly (no auth service dependency)
- `[Collection("Integration")]` for shared fixture (on base class)
- Real HTTP calls, real database, real migrations
- `IAuthServiceClient` mocked in factory (returns TotalUsers=5, ActiveUsers=4)
- Requires `backend/global.json` pinning SDK to 8.0 (avoids .NET 10 SDK incompatibility)
- Location: `backend/tests/Broker.Backoffice.Tests.Integration/`
- Coverage: Health, Swagger, Clients (CRUD + Update + GetAccounts + SetClientAccounts + InvalidAccountId + Filters + DateFilter + SortByDisplayName + DuplicateEmail + DeleteLinkedToAccount + RouteBodyIdMismatch + StaleRowVersion), Accounts (CRUD + Update + SetAccountHolders + InvalidClientId + Filters + SortByClearerName + DuplicateNumber + DeleteLinkedToOrders + RouteBodyIdMismatch), Instruments (CRUD + Update + Filters + DuplicateSymbol + DeleteLinkedToOrders + DeleteLinkedToTransactions + RouteBodyIdMismatch), TradeOrders (CRUD + Update + Filters + SortByInstrumentSymbol + InvalidAccount + LimitWithoutPrice + StopWithoutStopPrice + GTDWithoutExpiration + DeleteLinkedToTransactions + RouteBodyIdMismatch), NonTradeOrders (CRUD + Update + Filters + InvalidCurrencyId + InvalidAccountId + DeleteLinkedToTransactions + RouteBodyIdMismatch), TradeTransactions (CRUD + Update + StaleRowVersion + GetByOrder + InvalidOrder + Filters + SideMismatch + InvalidInstrumentId + InvalidOrderId + RouteBodyIdMismatch), NonTradeTransactions (CRUD + Update + StaleRowVersion + GetByOrder + InvalidOrder + Filters + WithoutOrder + InvalidCurrencyId + InvalidOrderId + RouteBodyIdMismatch), Clearers/Currencies/Exchanges/TradePlatforms (CRUD + DuplicateName), Dashboard (stats), Audit (list + getById + Filters), EntityChanges (list + listAll + Filters), Countries (list), Permission denial (403 for limited users), Concurrency (409 for stale RowVersion)

### Auth Service Integration Tests (45 tests, ~5s)
- Same Testcontainers pattern as core
- Location: `auth-service/tests/Broker.Auth.Tests.Integration/`
- Coverage: Health, Auth (login, refresh, me, change-password, update-profile, logout, photo CRUD + unauth + cache-control), Users (CRUD + GetById + Update + Delete + duplicate-username/email + photo + route mismatch + reset-password), Roles (CRUD + GetById + Update + Delete + duplicate-name + set-permissions + system-role-protection), Permissions (list), BasicAuth (login via Basic header)

### Integration test patterns
- All update tests must include `Id` in the request body (controllers check `id != command.Id`)
- Reference data (Clearers, TradePlatforms, Exchanges, Currencies) Create returns 200 OK (not 201)
- Aggregate CRUD (Clients, Accounts, Instruments, Orders, Transactions, Users, Roles) Create returns 201 Created
- Currency `Code` column is 3 chars max (ISO 4217); test codes must be ≤ 3 chars
- Prerequisites helper methods (e.g., `CreatePrerequisitesAsync()`) create Account + Instrument/Currency for Order/Transaction tests
- Core integration tests use `TestJwtTokenHelper.GenerateAdminToken()` (all 31 permissions) or `AuthenticateWithPermissions()` for limited permission tests

### Gateway Integration Tests (32 tests, ~1s)
- Testcontainers (real PostgreSQL 17 in Docker)
- `CustomWebApplicationFactory` with in-memory YAML configs
- Location: `gateway/tests/Broker.Gateway.Tests.Integration/`
- **Not included in CI pipeline** — gateway CI job only runs build + vulnerability check (known gap)
- Coverage: Health (live, ready), Menu (GET filtered/raw, PUT valid/invalid, auth 401, perm 403), Entities (GET filtered/raw, PUT valid/invalid, GET by name, 404), Upstreams (GET, PUT valid/invalid URI/empty routes, perm 403, reload), EntityChanges (GET by entityType/all, filters, pagination, sort, changeType), Audit (PUT creates audit, before/after JSON)

### Frontend Tests (167 tests, ~6s)
- Vitest with jsdom environment
- React Testing Library + user-event
- MSW for network-level API mocking
- Test factories with @faker-js/faker
- `renderWithProviders()` wraps with QueryClient, Theme, Auth, Router
- Tests split across monorepo packages: `@broker/ui-kit` (51 tests), `@broker/backoffice` (58 tests), `@broker/auth-module` (38 tests), `@broker/config-app` (20 tests)
- Run all: `pnpm turbo test`
- Utility/hook tests (ui-kit): `validateFields.test.ts`, `extractErrorMessage.test.ts`, `useConfirm.test.ts`, `usePermission.test.tsx`, `useDebounce.test.tsx`
- Page smoke tests (backoffice): 7 list pages (Clients, Accounts, Instruments, TradeOrders, NonTradeOrders, TradeTransactions, NonTradeTransactions) — title, search bar, create button permission gating, export button
- Auth-module tests: UsersPage, RolesPage, LoginPage, ProfileTab, RoleDetailsPage, UserDialogs, RoleDialogs — rendering, validation, permission gating
- Config SPA tests: MenuEditorPage, UpstreamsPage, EntityFieldsPage, NotFoundPage — rendering, loading states, permission gating, empty states
- Component tests (ui-kit): `ConfirmDialog`, `ErrorBoundary`, `UserAvatar`, `PageContainer`
- Coverage excludes `src/test/**` and `src/types/**` (test infra/type augmentations)

### Scripts
- `scripts/test.sh [unit|integration|all]` — backend tests in Docker
- `scripts/db_check.sh` — database integrity validation
- `scripts/smoke.sh [--clean|--fast]` — end-to-end smoke test

## 13. Running Locally

### With Docker (recommended):
```bash
cp .env.example .env    # Edit secrets
docker compose up --build -d
# Frontend: http://localhost:3000 (3 SPAs via nginx)
# Gateway: http://localhost:8090
# API/Swagger: http://localhost:5050/swagger
# Login: admin / Admin123!
# Services: postgres, auth (:8082), api (:5050), gateway (:8090), web (:3000), n8n (:5678)
```

### Frontend dev (hot reload):
```bash
cd frontend && pnpm install && pnpm turbo dev
# Backoffice: :5173, Auth: :5174, Config: :5175
# Each proxies /api to localhost:8090 (gateway)
```

### Backend dev:
```bash
cd backend/src/Broker.Backoffice.Api
dotnet run
# Runs on :5050, needs PostgreSQL on :5432
```

### Environment variables:
| Variable | Required | Description |
|----------|----------|-------------|
| PG_PASSWORD | Yes | PostgreSQL password |
| JWT_SECRET | Yes | JWT signing key (min 32 chars) |
| ADMIN_PASSWORD | No | Initial admin password (default: Admin123!) |
| SEED_DEMO_DATA | No | Seed demo data (default: false) |
| N8N_DB_PASSWORD | No | n8n PostgreSQL password (default: n8n_password) |
| N8N_PASSWORD | No | n8n web UI password (default: Admin123!) |

### Health checks:
- `/health/live` — Liveness (always 200)
- `/health/ready` — Readiness (PostgreSQL connectivity)

## 14. Development Philosophy

- **Clean Architecture** — Domain has no dependencies; Application defines interfaces; Infrastructure implements them
- **CQRS without Event Sourcing** — Commands mutate, queries read, no shared models
- **No repository abstraction** — Handlers use `IAppDbContext` directly (thin layer over EF Core)
- **Manual mapping** — No AutoMapper; explicit LINQ projections keep control and visibility
- **URL as state** — All grid filters, pagination, and sort live in URL search params
- **Permission-first UI** — Every action button is gated by permission check
- **Audit everything** — Both request-level and field-level change tracking
- **Concurrency safety** — RowVersion-based optimistic concurrency on all aggregate roots

## 15. Rules for AI Agents

### When adding a new entity/aggregate:
1. Create domain entity in `Domain/{Entity}/` with enums
2. If it's an aggregate root, extend `AuditableEntity`
3. Add DbSet to `IAppDbContext` and `AppDbContext`
4. Create `IEntityTypeConfiguration<T>` in `Infrastructure/Persistence/Configurations/`
5. Add EF Core migration: `dotnet ef migrations add MigrationName`
6. Create Application layer: DTOs, Commands (Create/Update/Delete), Queries (GetAll/GetById), Validators
7. Follow one-file-per-operation pattern: command + validator + handler in single file
8. Create API Controller with `[HasPermission]` attributes and `[ServiceFilter(typeof(AuditActionFilter))]` on mutations
9. Register entity in `EntityTrackingRegistry` for audit change tracking
10. Add permission codes to `Permissions.cs` and seed them
11. Frontend: add types, hooks, page, dialogs, route

### When adding a new API endpoint:
- Controller attributes: `[ApiController]`, `[ApiVersion("1.0")]` (from `Asp.Versioning`), `[Route("api/v1/[controller]")]`
- Paginated lists return `PagedResult<T>`
- Create returns 201 with `CreatedAtAction`
- Update returns 200 with updated DTO
- Delete returns 204 `NoContent`
- Use `[HasPermission(Permissions.XxxYyy)]` for authorization
- Use `[ServiceFilter(typeof(AuditActionFilter))]` on POST/PUT/DELETE

### When adding a frontend page:
- Follow existing page pattern: readParams → useQuery → columns → filterDefs → sortModel → exportColumns
- Use `PageContainer` with variant="list" for grids
- Use `FilteredDataGrid` with filterDefs Map, pass `sortModel` and `onSortModelChange`
- Derive `sortModel` from URL `sort` param: `useMemo(() => { if (!params.sort) return []; const [field, dir] = params.sort.split(" "); return [{ field, sort: dir as GridSortDirection }]; }, [params.sort])`
- Import `GridSortDirection` from `@mui/x-data-grid`
- Filters go in URL search params, not local state
- Add export support: ExcelColumn[] + fetchAll function + ExportButton
- Gate actions with `useHasPermission()`
- Add route in `router/index.tsx` using `React.lazy()` + `withSuspense()` under RequireAuth
- Delete actions: use `useConfirm()` + `<ConfirmDialog />` (not native `confirm()`)
- Dialog `handleSubmit`: wrap `mutateAsync` in `try/catch` (error toast via MutationCache)
- Detail pages: use `PageContainer` with `breadcrumbs` prop, no Back button, `<Card>` without `variant="outlined"` (uses theme shadow)
- Dialog forms: add inline validation with `FieldErrors` state + `validateRequired`/`validateEmail` from `utils/validateFields.ts`
- Add **page-level History button** in PageContainer actions: `{canAudit && <Button variant="outlined" startIcon={<HistoryIcon />} onClick={() => navigate("/audit?entityType=EntityName")}>History</Button>}`
- Add **per-row History button** in actions column: `{canAudit && <IconButton size="small" onClick={() => setHistoryEntityId(row.id)}><HistoryIcon fontSize="small" /></IconButton>}` + `<EntityHistoryDialog entityType="..." entityId={historyEntityId ?? ""} open={historyEntityId !== null} onClose={() => setHistoryEntityId(null)} />`
- Actions column width: 150 (4 buttons: view, history, edit, delete)

### When modifying existing code:
- Preserve file organization pattern (command + validator + handler in one file)
- Maintain DTO separation (ListItemDto for grids, Dto for details)
- Keep manual LINQ mapping — do not introduce AutoMapper
- Always include RowVersion in update commands
- Set audit context (BeforeJson/AfterJson) in mutation handlers
- Invalidate relevant React Query keys after mutations
- Add `meta: { successMessage: "..." }` to new mutation hooks
- Wrap dialog `handleSubmit` with `try/catch` around `mutateAsync`
- Use `LikeHelper.ContainsPattern()` for all `EF.Functions.Like()` calls (escapes SQL LIKE wildcards)

### Sorting conventions:
- Frontend sends sort as `"field asc"` or `"field desc"` via URL `?sort=field asc`
- `SortBy()` in `QueryableExtensions` parses both `"field asc/desc"` and legacy `-field` formats; uses reflection with `BindingFlags.IgnoreCase` to find entity properties
- `SortBy()` operates on the **entity** IQueryable (before `.Select()` projection), so it can only sort by properties that exist on the domain entity
- For DTO fields that don't exist on the entity (computed fields, navigation properties), use a private `ApplySort()` method in the query handler with explicit switch cases
- Handlers with `ApplySort()`: GetClients (displayName, country fields), GetAccounts (clearerName, tradePlatformName), GetInstruments (exchangeCode, currencyCode, countryName), GetTradeOrders, GetNonTradeOrders, GetTradeTransactions, GetNonTradeTransactions, GetAllEntityChanges
- `ApplySort()` must parse `"field asc/desc"` format: `var parts = sort.Split(' ', 2, ...); var field = parts[0]; var desc = parts[1] == "desc"`
- Every sortable DataGrid column `field` must be handled in either `SortBy()` (entity property) or `ApplySort()` switch — unhandled fields silently return unsorted data

### Reference data (Clearers, TradePlatforms, Exchanges, Currencies):
- Two query endpoints: `GET /entity` (active only, for dropdowns) and `GET /entity/all` (for settings CRUD)
- Active-only endpoints use domain-specific permission (e.g., AccountsRead for clearers)
- All/CRUD endpoints use SettingsManage permission
- Frontend CRUD is in `pages/settings/` directory
- All CRUD handlers inject `IAuditContext` and set EntityType/EntityId/BeforeJson/AfterJson (Create: AfterJson only; Update: Before + After; Delete: BeforeJson only)
- Not tracked by `EntityTrackingRegistry` (no field-level EntityChange records) — audit is via request-level AuditLog with JSON snapshots

### Do not:
- Add AutoMapper or any mapping library
- Add a generic repository layer
- Change enum serialization from strings to integers
- Use default exports in frontend
- Store filter state in React state instead of URL params
- Skip FluentValidation on commands
- Skip audit context setup in mutation handlers
- Introduce Redux, Zustand, or other state management
- Use `any` type in TypeScript
- Use native `confirm()` / `alert()` — use `ConfirmDialog` / notistack toasts instead
- Skip `meta.successMessage` on new mutation hooks
- Skip `try/catch` on `mutateAsync` in dialog submit handlers
- Skip inline validation on required fields in dialog forms
- Add Zod/Yup — use `validateFields.ts` helpers for simple inline validation
- Use Back buttons in detail pages — use breadcrumbs instead
- Use `variant="outlined"` on Cards in detail pages — use default shadow-based cards
- Add an AppBar — the sidebar is the only navigation element
- Change sidebar color tokens — use `SIDEBAR_COLORS` from `theme/index.ts`
- Use `localStorage.clear()` on logout/401 — use targeted `removeItem` for auth tokens only
- Use raw `$"%{value}%"` in LIKE patterns — use `LikeHelper.ContainsPattern()` to escape wildcards
- Instantiate query handlers directly (`new XxxHandler(db).Handle(...)`) — use `mediator.Send()` instead
- Use `AllowAnyOrigin()` in CORS without checking configured origins first
- Add sortable DataGrid columns whose `field` names don't match entity properties without adding them to `ApplySort()` — sorting will silently fail
- Use the old `-field` sort format in new code — use `"field asc"`/`"field desc"` format
- Use `useEffect` with `setState` for dialog reset — use render-time `prevOpen` pattern (ESLint `react-hooks/set-state-in-effect` rule)
- Delete aggregates without checking for linked child entities — add pre-delete `AnyAsync` checks
- Skip refresh token revocation on password change/reset/deactivation/deletion

