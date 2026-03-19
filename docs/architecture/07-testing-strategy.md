# 07. Стратегия тестирования

## Текущее состояние

**Общее количество тестов: 617**

| Набор | Тестов | Время | Технологии |
|-------|--------|-------|------------|
| Monolith unit | 273 | ~2с | xUnit, FluentAssertions, NSubstitute |
| Monolith integration | 145 | ~10с | Testcontainers (PostgreSQL), WebApplicationFactory |
| Auth service unit | 44 | ~1с | xUnit, FluentAssertions, NSubstitute |
| Auth service integration | 36 | ~5с | Testcontainers (PostgreSQL), WebApplicationFactory |
| Frontend | 119 | ~6с | Vitest, React Testing Library, MSW, faker |

---

## Frontend (119 тестов)

**Фреймворк:** Vitest 2.x + React Testing Library 16 + jsdom

**Include pattern:** `src/{hooks,auth,lib,utils,test}/**/*.test.{ts,tsx}`

### Утилиты и хуки (51 тест)

| Файл | Тестов | Что тестирует |
|------|--------|---------------|
| `src/utils/validateFields.test.ts` | 19 | Валидаторы: validateRequired, validateEmail |
| `src/utils/extractErrorMessage.test.ts` | 13 | Парсинг ошибок: Axios, ProblemDetails, fallback |
| `src/hooks/useConfirm.test.ts` | 6 | Хук useConfirm: promise-based подтверждение |
| `src/hooks/useDebounce.test.tsx` | 5 | Хук debounce: задержка, сброс таймера, custom delay |
| `src/auth/usePermission.test.ts` | 3 | Хук useHasPermission: наличие/отсутствие permission |
| `src/auth/usePermission.test.tsx` | 5 | Хук useHasPermission: рендеринг с провайдерами |

### Smoke-тесты страниц (45 тестов)

Каждая страница проверяется на 5 аспектов:
1. Рендерится заголовок страницы
2. Рендерится строка поиска (GlobalSearchBar)
3. Кнопка Create видна с нужным permission
4. Кнопка Create скрыта без permission
5. Кнопка Export видна

| Файл | Тестов | Страницы |
|------|--------|----------|
| `src/test/list-pages.test.tsx` | 25 | ClientsPage, AccountsPage, InstrumentsPage, UsersPage, RolesPage |
| `src/test/order-pages.test.tsx` | 10 | TradeOrdersPage, NonTradeOrdersPage |
| `src/test/transaction-pages.test.tsx` | 10 | TradeTransactionsPage, NonTradeTransactionsPage |

### Компоненты и диалоги (23 теста)

| Файл | Тестов | Что тестирует |
|------|--------|---------------|
| `src/test/transaction-details.test.tsx` | 11 | Детальные страницы Trade/NonTrade Transaction |
| `src/test/components.test.tsx` | 8 | ConfirmDialog, ErrorBoundary, UserAvatar, PageContainer |
| `src/test/edit-dialogs.test.tsx` | 4 | Regression: Edit-диалоги заполняют форму при закешированных данных |

### Инфраструктура тестирования

| Утилита | Путь | Назначение |
|---------|------|------------|
| renderWithProviders | `src/test/renderWithProviders.tsx` | Полный набор провайдеров (QueryClient, Theme, Auth, Router) |
| Factories | `src/test/factories/` | Билдеры тестовых данных (faker.js) |
| MSW handlers | `src/test/msw/` | Моки API-эндпоинтов |

### Coverage config

`vitest.config.ts` исключает из метрик покрытия:
- `src/test/**` — тестовая инфраструктура
- `src/types/**` — type augmentations

### Примечание: OOM-проблема

Ранее тяжёлые page-тесты с полным рендерингом MUI DataGrid в jsdom вызывали OOM (>4 GB V8 heap). Решение — smoke-тесты проверяют только структуру страницы (заголовок, кнопки, permission gating) без рендеринга DataGrid с данными.

---

## Monolith Unit-тесты (273 тестов)

**Проект:** `Broker.Backoffice.Tests.Unit` — xUnit + FluentAssertions + NSubstitute

Покрывают **все валидаторы** команд монолита:

| Группа | Валидаторы |
|--------|-----------|
| Clients | Create, Update, SetAccounts |
| Accounts | Create, Update, SetHolders |
| Instruments | Create, Update |
| Trade Orders | Create, Update |
| Non-Trade Orders | Create, Update |
| Trade Transactions | Create, Update |
| Non-Trade Transactions | Create, Update |
| Reference data | Clearer, Currency, Exchange, TradePlatform — Create/Update каждый |

---

## Monolith Integration-тесты (145 тестов)

**Проект:** `Broker.Backoffice.Tests.Integration` — Testcontainers (реальный PostgreSQL 16 в Docker)

Все интеграционные тесты используют общий `[Collection("Integration")]` с `ICollectionFixture<CustomWebApplicationFactory>` — единый контейнер PostgreSQL на все тест-классы. Rate limiting отключён через `UseSetting`. JWT генерируется локально через `TestJwtTokenHelper` (без обращения к auth-service).

### Покрытие по сущностям

| Сущность | Тестов | Что покрыто |
|----------|--------|-------------|
| Health/Swagger | 3 | Liveness, readiness, Swagger JSON |
| Clients | 18 | CRUD, Update, GetAccounts, SetClientAccounts, InvalidAccountId, Filters (status+clientType+pepStatus+q), DateFilter, SortByDisplayName, DuplicateEmail, DeleteLinkedToAccount, RouteBodyIdMismatch, StaleRowVersion |
| Accounts | 15 | CRUD, Update, SetAccountHolders, InvalidClientId, Filters (status+accountType+q), SortByClearerName, DuplicateNumber, RouteBodyIdMismatch |
| Instruments | 11 | CRUD, Update, Filters (type+status+isMarginEligible+q), DuplicateSymbol, RouteBodyIdMismatch |
| Trade Orders | 14 | CRUD, Update, Filters (status+side+orderType+q), SortByInstrumentSymbol, InvalidAccount, LimitWithoutPrice, StopWithoutStopPrice, GTDWithoutExpiration, RouteBodyIdMismatch |
| Non-Trade Orders | 11 | CRUD, Update, Filters (status+nonTradeType+q), InvalidCurrencyId, InvalidAccountId, RouteBodyIdMismatch |
| Trade Transactions | 16 | CRUD, Update, StaleRowVersion, GetByOrder, InvalidOrder, Filters (status+side+q), SideMismatch, InvalidInstrumentId/OrderId, RouteBodyIdMismatch |
| Non-Trade Transactions | 16 | CRUD, Update, StaleRowVersion, GetByOrder, InvalidOrder, Filters (status+q+amountMin/Max), WithoutOrder, InvalidCurrencyId/OrderId, RouteBodyIdMismatch |
| Reference data | ~28 | Clearers, Currencies, Exchanges, TradePlatforms — List/ListAll/Create/Update/Delete/DuplicateName/DuplicateOnUpdate |
| Dashboard | 1 | Stats endpoint |
| Audit | 4 | List, GetById, Filters (isSuccess+method+q) |
| Entity Changes | 4 | List, ListAll, Filters (entityType+changeType) |
| Countries | 1 | List endpoint |
| Permission denial | 1 | 403 для ограниченного пользователя |
| Concurrency | 2 | 409 stale RowVersion — Account, Instrument |

## Auth Service Unit-тесты (44 тестов)

**Проект:** `Broker.Auth.Tests.Unit` — xUnit + FluentAssertions + NSubstitute

Покрывают валидаторы auth-service:

| Группа | Валидаторы |
|--------|-----------|
| Auth | Login, ChangePassword, UpdateProfile |
| Users | Create, Update |
| Roles | Create, Update, FullName MaxLength |

## Auth Service Integration-тесты (36 тестов)

**Проект:** `Broker.Auth.Tests.Integration` — Testcontainers (реальный PostgreSQL 16 в Docker)

| Сущность | Тестов | Что покрыто |
|----------|--------|-------------|
| Health | 1 | Liveness |
| Auth | ~8 | Login, refresh, me, change-password, update-profile, photo CRUD + unauth + cache-control |
| Users | ~12 | CRUD, GetById, Update, Delete, duplicate-username/email, photo, route mismatch |
| Roles | ~10 | CRUD, GetById, Update, Delete, duplicate-name, set-permissions, system-role-protection |
| Permissions | 1 | List endpoint |

### Coverage config

`backend/coverage.runsettings` исключает из метрик покрытия:
- `SeedDemoData.cs` (~3700 LOC) — демо-данные, не выполняются в тестах
- `SeedCountries.cs` — статический список стран
- `Migrations/` — автогенерированные миграции EF Core

---

## CI Pipeline

GitHub Actions: 5 параллельных job-ов на каждый push в main (~1.5 мин):

1. **backend** — build + NuGet audit + 273 unit-тестов (монолит)
2. **backend-integration** — 145 интеграционных тестов (монолит, Testcontainers PostgreSQL)
3. **auth-service** — build + NuGet audit + 44 unit-тестов
4. **auth-service-integration** — 36 интеграционных тестов (Testcontainers PostgreSQL)
5. **frontend** — tsc + eslint + 119 vitest-тестов + production build

---

## Рекомендации на будущее

### Краткосрочные
- Добавить coverage threshold в CI (fail при падении ниже порога)
- Тесты для `src/api/hooks.ts` (моки axios, проверка params)

### Среднесрочные
- Playwright/Cypress E2E для критических путей (login, CRUD клиента/счёта, назначение permissions)

### Долгосрочные
- Contract testing (Pact) между frontend и backend
- Visual regression (Chromatic / Percy) для UI-компонентов
