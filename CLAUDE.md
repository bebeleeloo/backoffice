# CLAUDE.md — Broker Backoffice System

## 1. Project Overview

Broker Backoffice — internal admin panel for a brokerage firm. Manages clients, trading accounts, financial instruments, users, roles, permissions, and reference data (clearers, trade platforms, exchanges, currencies). Includes audit logging with field-level change tracking, dashboard analytics, and Excel export.

**Repository structure:**
```
/
├── backend/          # .NET 8 API (Clean Architecture + CQRS)
├── frontend/         # React 18 SPA (TypeScript, MUI, React Query)
├── docs/             # Architecture documentation
├── scripts/          # Test and deployment scripts
├── screenshots/      # UI screenshots
├── .github/workflows/ci.yml  # GitHub Actions CI pipeline
├── docker-compose.yml
├── Dockerfile.api    # Multi-stage .NET build
├── Dockerfile.web    # Multi-stage Node + nginx build
└── .env.example
```

## 2. Technology Stack

### Backend
- .NET 8, ASP.NET Core, C# 12
- EF Core 8 (SQL Server, Code-First migrations)
- MediatR (CQRS command/query dispatch)
- FluentValidation (request validation via MediatR pipeline)
- Serilog (structured logging with correlation IDs)
- ASP.NET Core Identity password hashing
- JWT Bearer authentication with refresh token rotation
- ASP.NET Core Rate Limiting (fixed window on login, auth, sensitive endpoints)
- ASP.NET Core Response Compression (Gzip, CompressionLevel.Fastest)

### Frontend
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
- Docker Compose (3 services: mssql, api, web) with restart policies, resource limits, log rotation
- SQL Server 2022
- nginx (frontend reverse proxy + SPA fallback + gzip + security headers + HSTS + cache control)
- GitHub Actions CI (backend build + unit tests + integration tests, frontend tsc + eslint + vitest)

### Testing
- Backend: xUnit, FluentAssertions, NSubstitute, Testcontainers (MSSQL)
- Frontend: Vitest, React Testing Library, MSW, @faker-js/faker

## 3. Architecture

```
┌─────────────┐     ┌──────────────┐     ┌──────────────┐
│   Frontend   │────▶│   nginx:80   │────▶│  API:8080    │────▶ SQL Server
│  (React SPA) │     │  /api/ proxy │     │  .NET 8      │     :1433
└─────────────┘     └──────────────┘     └──────────────┘
     :3000                                    :5050
```

Backend follows Clean Architecture with 4 layers:
- **Domain** — Entities, enums, value objects. Zero dependencies.
- **Application** — CQRS handlers, DTOs, validators, interfaces. Depends only on Domain.
- **Infrastructure** — EF Core, JWT, audit tracking, seeding. Implements Application interfaces.
- **Api** — Controllers, middleware, filters, Program.cs. Composes everything.

Frontend follows feature-based organization with shared components.

**Security headers** (nginx — applied in server block AND all location blocks):
- `Content-Security-Policy`: default-src 'self', unsafe-inline for styles (MUI), data:/blob: for images
- `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `X-XSS-Protection`, `Referrer-Policy`
- `Strict-Transport-Security`: max-age=63072000; includeSubDomains
- `Permissions-Policy`: camera=(), microphone=(), geolocation=()
- `server_tokens off` — hides nginx version
- All 7 headers duplicated in `/assets/`, `/index.html`, and `/` location blocks (nginx `add_header` in child blocks overrides parent)

**Cache control** (nginx):
- `/assets/*` (Vite hashed files): `Cache-Control: public, immutable`, expires 1y
- `/index.html` and `/`: `Cache-Control: no-cache` (SPA always gets fresh HTML)

## 4. Backend Structure

```
backend/src/
├── Broker.Backoffice.Domain/
│   ├── Common/              # Entity<TId>, AuditableEntity
│   ├── Identity/            # User, Role, Permission, UserRole, RolePermission
│   ├── Clients/             # Client, ClientAddress, InvestmentProfile + enums
│   ├── Accounts/            # Account, AccountHolder, Clearer, TradePlatform + enums
│   ├── Instruments/         # Instrument, Exchange, Currency + enums
│   ├── Orders/              # Order, TradeOrder, NonTradeOrder + enums
│   ├── Transactions/        # Transaction, TradeTransaction, NonTradeTransaction + enums
│   ├── Audit/               # AuditLog, EntityChange
│   └── Countries/           # Country
│
├── Broker.Backoffice.Application/
│   ├── Abstractions/        # IAppDbContext, ICurrentUser, IJwtTokenService, IAuditContext
│   ├── Behaviors/           # ValidationBehavior (MediatR pipeline)
│   ├── Common/              # PagedQuery, PagedResult, QueryableExtensions, LikeHelper
│   ├── Auth/                # Login, RefreshToken, GetMe, ChangePassword, UpdateProfile
│   ├── Clients/             # CRUD commands/queries + DTOs
│   ├── Accounts/            # CRUD commands/queries + DTOs
│   ├── Instruments/         # CRUD commands/queries + DTOs
│   ├── Orders/
│   │   ├── TradeOrders/     # CRUD commands/queries + DTOs for trade orders
│   │   └── NonTradeOrders/  # CRUD commands/queries + DTOs for non-trade orders
│   ├── Transactions/
│   │   ├── TradeTransactions/    # CRUD commands/queries + DTOs for trade transactions
│   │   └── NonTradeTransactions/ # CRUD commands/queries + DTOs for non-trade transactions
│   ├── Users/               # CRUD commands/queries + DTOs + Photo upload/delete/get
│   ├── Roles/               # CRUD commands/queries + DTOs + SetRolePermissions
│   ├── Clearers/            # CRUD + GetAll / GetActive
│   ├── Currencies/          # CRUD + GetAll / GetActive
│   ├── Exchanges/           # CRUD + GetAll / GetActive
│   ├── TradePlatforms/      # CRUD + GetAll / GetActive
│   ├── AuditLogs/           # GetAuditLogs, GetAuditLogById
│   ├── EntityChanges/       # GetEntityChanges (per entity), GetAllEntityChanges (global)
│   ├── Dashboard/           # GetDashboardStats
│   ├── Countries/           # GetCountries
│   └── Permissions/         # GetPermissions
│
├── Broker.Backoffice.Infrastructure/
│   ├── Persistence/
│   │   ├── AppDbContext.cs           # EF Core context + change tracking override
│   │   ├── Configurations/           # IEntityTypeConfiguration<T> per entity
│   │   ├── ChangeTracking/           # EntityTrackingRegistry, ChangeTrackingContext
│   │   ├── Migrations/              # EF Core code-first migrations
│   │   ├── SeedData.cs              # Countries, ref data, admin user, permissions
│   │   └── SeedDemoData.cs          # Demo users (with portrait photos), clients, accounts, instruments, orders, transactions
│   ├── Services/                     # JwtTokenService, CurrentUser, DateTimeProvider
│   ├── Auth/                         # HasPermissionAttribute, PermissionPolicyProvider
│   ├── Middleware/                    # ExceptionHandling, CorrelationId
│   ├── Filters/                      # AuditActionFilter
│   └── DependencyInjection.cs
│
└── Broker.Backoffice.Api/
    ├── Controllers/         # One controller per aggregate
    └── Program.cs           # Composition root
```

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
- `QueryableExtensions.SortBy()` handles dynamic sorting via expression trees

**Filtering conventions:**
- Text: `EF.Functions.Like(field, LikeHelper.ContainsPattern(value))` — escapes `%`, `_`, `[` wildcards via `LikeHelper` in `Application/Common/LikeHelper.cs`
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

**User photos:**
- Stored as binary in DB (Photo byte[], PhotoContentType nvarchar(50))
- Endpoints: `GET/PUT/DELETE /users/{id}/photo` and `GET/PUT/DELETE /auth/photo`
- GET photo is `[AllowAnonymous]` — required because `<img src>` cannot send JWT Authorization headers
- PUT photo accepts `IFormFile` multipart upload, max 2 MB, validates MIME type (jpeg/png/gif/webp)
- Returns raw image bytes with `Content-Type` header (not base64 in JSON)
- `Cache-Control: private, max-age=3600` on GET response
- Photo/PhotoContentType excluded from audit change tracking (`EntityTrackingRegistry`)
- Demo data seeds portrait photos from randomuser.me for all users

**Concurrency control:**
- `RowVersion` byte[] on AuditableEntity
- Passed from client, set as OriginalValue before SaveChanges
- EF Core throws DbUpdateConcurrencyException on mismatch

## 5. Frontend Structure

```
frontend/
├── public/
│   ├── logo.svg              # App logo (SVG) — sidebar, login page, favicon
│   └── login-bg.jpg          # Login page background image (legacy, not used)
├── index.html                # Entry HTML (favicon → /logo.svg)
└── src/
    ├── api/
    │   ├── client.ts           # Axios instance, interceptors, token refresh
    │   ├── hooks.ts            # React Query hooks for all entities
    │   └── types.ts            # TypeScript interfaces matching backend DTOs
    ├── auth/
    │   ├── AuthContext.tsx      # Auth state provider (user, permissions, login/logout)
    │   └── usePermission.ts    # useHasPermission() hook
    ├── components/
    │   ├── UserAvatar.tsx        # Reusable avatar (photo from API or initials fallback)
    │   ├── Breadcrumbs.tsx      # MUI breadcrumb navigation for detail pages
    │   ├── DetailField.tsx      # Reusable label+value field for detail pages
    │   ├── ErrorBoundary.tsx    # React error boundary with MUI fallback UI
    │   ├── PageContainer.tsx    # Page wrapper (title, actions, subheader, variant, breadcrumbs)
    │   ├── ExportButton.tsx     # Excel export button with loading state
    │   ├── ConfirmDialog.tsx    # MUI confirmation dialog for delete actions
    │   ├── RouteLoadingFallback.tsx # Centered spinner for lazy-loaded routes
    │   ├── grid/
    │   │   ├── FilteredDataGrid.tsx   # DataGrid + inline filter row + empty state overlay
    │   │   ├── GridFilterRow.tsx      # Filter row rendering (native scroll sync, theme-aware)
    │   │   ├── InlineTextFilter.tsx   # Debounced text input (300ms)
    │   │   ├── CompactMultiSelect.tsx # Checkbox multi-select dropdown
    │   │   ├── CompactCountrySelect.tsx
    │   │   ├── DateRangePopover.tsx   # From/To date picker
    │   │   ├── NumericRangePopover.tsx # Min/Max numeric range
    │   │   └── InlineBooleanFilter.tsx
    │   ├── GlobalSearchBar.tsx         # Debounced auto-search input (300ms)
    │   ├── EntityHistoryDialog.tsx    # Change history per entity
    │   ├── AuditDetailDialog.tsx      # Audit log entry detail
    │   └── ChangeHistoryComponents.tsx
    ├── layouts/
    │   └── MainLayout.tsx      # Dark collapsible sidebar (no AppBar) + content area
    ├── pages/
    │   ├── LoginPage.tsx        # Split-screen login (branded left panel + form right)
    │   ├── DashboardPage.tsx
    │   ├── ClientsPage.tsx / ClientDetailsPage.tsx / ClientDialogs.tsx
    │   ├── AccountsPage.tsx / AccountDetailsPage.tsx / AccountDialogs.tsx
    │   ├── InstrumentsPage.tsx / InstrumentDetailsPage.tsx / InstrumentDialogs.tsx
    │   ├── TradeOrdersPage.tsx / TradeOrderDetailsPage.tsx / TradeOrderDialogs.tsx
    │   ├── NonTradeOrdersPage.tsx / NonTradeOrderDetailsPage.tsx / NonTradeOrderDialogs.tsx
    │   ├── TradeTransactionsPage.tsx / TradeTransactionDetailsPage.tsx / TradeTransactionDialogs.tsx
    │   ├── NonTradeTransactionsPage.tsx / NonTradeTransactionDetailsPage.tsx / NonTradeTransactionDialogs.tsx
    │   ├── UsersPage.tsx / UserDialogs.tsx
    │   ├── RolesPage.tsx / RoleDetailsPage.tsx / RoleDialogs.tsx
    │   ├── AuditPage.tsx
    │   ├── NotFoundPage.tsx     # 404 page (wildcard route)
    │   └── settings/           # ProfileTab, AppearanceTab, ReferenceDataTab, CRUD dialogs
    ├── router/
    │   └── index.tsx            # Route definitions with RequireAuth + React.lazy()
    ├── theme/
    │   ├── index.ts             # createAppTheme(mode), createAppListTheme(base), SIDEBAR_COLORS, STAT_GRADIENTS
    │   └── ThemeContext.tsx      # AppThemeProvider, useThemeMode, useListTheme — dark/light/system
    ├── hooks/
    │   ├── useDebounce.ts
    │   └── useConfirm.ts       # Promise-based confirmation dialog hook
    ├── types/
    │   └── react-query.d.ts    # Type augmentation for mutation meta
    ├── utils/
    │   ├── exportToExcel.ts     # ExcelJS-based export utility
    │   ├── extractErrorMessage.ts # Axios/ProblemDetails error parser for toasts
    │   ├── orderConstants.ts    # Order status descriptions for tooltips
    │   ├── transactionConstants.ts # Transaction status descriptions for tooltips
    │   └── validateFields.ts    # Inline form validation helpers (validateRequired, validateEmail)
    └── test/
        ├── setupTests.ts
        ├── renderWithProviders.tsx
        ├── msw/                 # MSW handlers per entity
        └── factories/           # Test data factories with faker
```

### Key Frontend Conventions

**Page pattern (all list pages follow this):**
1. `useSearchParams()` → `readParams(sp)` to parse URL state
2. React Query hook for paginated data
3. `columns: GridColDef[]` with typed row
4. `filterDefs: Map<string, () => ReactNode>` mapping field → filter component
5. `exportColumns: ExcelColumn<T>[]` for Excel export
6. `fetchAll: () => Promise<T[]>` fetches with pageSize=10000 for export
7. Permission-gated action buttons via `useHasPermission()`
8. `PageContainer` wrapper with variant="list" for compact theme

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
3. Check FK references exist (`AnyAsync` for Account, Instrument, Currency etc. → `KeyNotFoundException`)
4. Check cross-entity consistency (`throw new InvalidOperationException` if mismatch, e.g. trade transaction Side must match order Side)
5. Check uniqueness (`throw new InvalidOperationException` if duplicate)
6. Modify entity
7. Set audit context (BeforeJson/AfterJson)
8. SaveChangesAsync (triggers change tracking)
9. Return DTO

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

**Level 2: EntityChange (field-level)**
- Captured in `AppDbContext.SaveChangesAsync()` override via EF Core ChangeTracker
- Records: operationId, entityType, entityId, displayName, changeType, fieldName, oldValue, newValue
- Grouped by operationId for atomic operations
- Deduplicates "delete + recreate" patterns for child entities (addresses, holders)
- Tracked entities configured in `EntityTrackingRegistry` (Client, Account, Instrument, Order, Transaction, User, Role + children)
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

### Backend Unit Tests (326 tests, ~2s)

- xUnit with `[Fact]` and `[Theory]`/`[InlineData]`
- FluentValidation.TestHelper for validators
- NSubstitute for mocking interfaces
- Location: `backend/tests/Broker.Backoffice.Tests.Unit/`
- Validators covered: Auth (Login, ChangePassword, UpdateProfile), Users (Create/Update), Clients (Create/Update, SetAccounts), Accounts (Create/Update, SetHolders), Instruments (Create/Update), Orders (TradeOrder Create/Update, NonTradeOrder Create/Update), Transactions (TradeTransaction Create/Update, NonTradeTransaction Create/Update), Roles (Create/Update), Reference data (Clearer, Currency, Exchange, TradePlatform — Create/Update each)

### Backend Integration Tests (122 tests, ~28s)
- Testcontainers (real MSSQL 2022 in Docker)
- `CustomWebApplicationFactory` extends `WebApplicationFactory<Program>`
- `[Collection("Integration")]` for shared fixture
- Real HTTP calls, real database, real migrations
- Each test authenticates independently via `AuthenticateAsync()` helper
- Rate limiting disabled via `UseSetting("RateLimiting:LoginPermitLimit", "10000")`
- Requires `backend/global.json` pinning SDK to 8.0 (avoids .NET 10 SDK incompatibility)
- Location: `backend/tests/Broker.Backoffice.Tests.Integration/`
- Coverage: all API endpoints — Health, Swagger, Auth (login, refresh, me, change-password, update-profile, photo upload/get/delete + unauth), Clients (CRUD + Update + GetAccounts), Accounts (CRUD + Update), Users (CRUD + Update + Photo upload/get/delete/anonymous), Roles (CRUD + GetById + Update + SetPermissions), Instruments (CRUD + Update + DuplicateSymbol), TradeOrders (CRUD + Update + InvalidAccount), NonTradeOrders (CRUD + Update), TradeTransactions (CRUD + SideMismatch), NonTradeTransactions (CRUD + WithoutOrder), Clearers/Currencies/Exchanges/TradePlatforms (List/ListAll/Create/Update/Delete/DuplicateName), Dashboard (stats), Audit (list + getById), EntityChanges (list + listAll), Permissions (list), Countries (list)

### Integration test patterns
- All update tests must include `Id` in the request body (controllers check `id != command.Id`)
- Reference data (Clearers, TradePlatforms, Exchanges, Currencies) Create returns 200 OK (not 201)
- Aggregate CRUD (Clients, Accounts, Instruments, Orders, Transactions, Users, Roles) Create returns 201 Created
- Currency `Code` column is 3 chars max (ISO 4217); test codes must be ≤ 3 chars
- Prerequisites helper methods (e.g., `CreatePrerequisitesAsync()`) create Account + Instrument/Currency for Order/Transaction tests

### Frontend Tests (76 tests, ~4s)
- Vitest with jsdom environment
- React Testing Library + user-event
- MSW for network-level API mocking
- Test factories with @faker-js/faker
- `renderWithProviders()` wraps with QueryClient, Theme, Auth, Router
- Test scope: `src/{hooks,auth,lib,utils}/**/*.test.{ts,tsx}`
- Location: `frontend/src/test/` (page/component tests), inline `*.test.ts` next to source (utility/hook tests)
- Utility/hook tests: `validateFields.test.ts`, `extractErrorMessage.test.ts`, `useConfirm.test.ts`, `usePermission.test.tsx`

### Scripts
- `scripts/test.sh [unit|integration|all]` — backend tests in Docker
- `scripts/db_check.sh` — database integrity validation
- `scripts/smoke.sh [--clean|--fast]` — end-to-end smoke test

## 13. Running Locally

### With Docker (recommended):
```bash
cp .env.example .env    # Edit secrets
docker compose up --build -d
# Frontend: http://localhost:3000
# API/Swagger: http://localhost:5050/swagger
# Login: admin / Admin123!
# Services auto-restart (unless-stopped), memory-limited (mssql: 2G, api: 512M, web: 256M)
```

### Frontend dev (hot reload):
```bash
cd frontend && npm install && npm run dev
# Runs on :5173, proxies /api to :5050
```

### Backend dev:
```bash
cd backend/src/Broker.Backoffice.Api
dotnet run
# Runs on :5050, needs MSSQL on :1433
```

### Environment variables:
| Variable | Required | Description |
|----------|----------|-------------|
| SA_PASSWORD | Yes | SQL Server SA password (complex) |
| JWT_SECRET | Yes | JWT signing key (min 32 chars) |
| ADMIN_PASSWORD | No | Initial admin password (default: Admin123!) |
| SEED_DEMO_DATA | No | Seed demo data (default: false) |

### Health checks:
- `/health/live` — Liveness (always 200)
- `/health/ready` — Readiness (SQL Server connectivity)

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
- Follow existing page pattern: readParams → useQuery → columns → filterDefs → exportColumns
- Use `PageContainer` with variant="list" for grids
- Use `FilteredDataGrid` with filterDefs Map
- Filters go in URL search params, not local state
- Add export support: ExcelColumn[] + fetchAll function + ExportButton
- Gate actions with `useHasPermission()`
- Add route in `router/index.tsx` using `React.lazy()` + `withSuspense()` under RequireAuth
- Delete actions: use `useConfirm()` + `<ConfirmDialog />` (not native `confirm()`)
- Dialog `handleSubmit`: wrap `mutateAsync` in `try/catch` (error toast via MutationCache)
- Detail pages: use `PageContainer` with `breadcrumbs` prop, no Back button, `<Card>` without `variant="outlined"` (uses theme shadow)
- Dialog forms: add inline validation with `FieldErrors` state + `validateRequired`/`validateEmail` from `utils/validateFields.ts`

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

### Reference data (Clearers, TradePlatforms, Exchanges, Currencies):
- Two query endpoints: `GET /entity` (active only, for dropdowns) and `GET /entity/all` (for settings CRUD)
- Active-only endpoints use domain-specific permission (e.g., AccountsRead for clearers)
- All/CRUD endpoints use SettingsManage permission
- Frontend CRUD is in `pages/settings/` directory

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
