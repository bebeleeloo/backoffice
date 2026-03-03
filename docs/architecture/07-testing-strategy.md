# 07. Стратегия тестирования

## Текущее состояние

**Общее количество тестов: 637**

| Набор | Тестов | Время | Технологии |
|-------|--------|-------|------------|
| Backend unit | 326 | ~2с | xUnit, FluentAssertions, NSubstitute |
| Backend integration | 192 | ~15с | Testcontainers (MSSQL), WebApplicationFactory |
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

## Backend Unit-тесты (326 тестов)

**Проект:** `Broker.Backoffice.Tests.Unit` — xUnit + FluentAssertions + NSubstitute

Покрывают **все валидаторы** всех команд:

| Группа | Валидаторы |
|--------|-----------|
| Auth | Login, ChangePassword, UpdateProfile |
| Users | Create, Update |
| Clients | Create, Update, SetAccounts |
| Accounts | Create, Update, SetHolders |
| Instruments | Create, Update |
| Trade Orders | Create, Update |
| Non-Trade Orders | Create, Update |
| Trade Transactions | Create, Update |
| Non-Trade Transactions | Create, Update |
| Roles | Create, Update |
| Reference data | Clearer, Currency, Exchange, TradePlatform — Create/Update каждый |

---

## Backend Integration-тесты (192 теста)

**Проект:** `Broker.Backoffice.Tests.Integration` — Testcontainers (реальный MSSQL 2022 в Docker)

Все интеграционные тесты используют общий `[Collection("Integration")]` с `ICollectionFixture<CustomWebApplicationFactory>` — единый контейнер SQL Server на все тест-классы. Rate limiting отключён через `UseSetting`.

### Покрытие по сущностям

| Сущность | Тестов | Что покрыто |
|----------|--------|-------------|
| Health/Swagger | 3 | Liveness, readiness, Swagger JSON |
| Auth | ~12 | Login, refresh, me, change-password, update-profile, photo CRUD, unauth 401, no-file 400, duplicate email 409 |
| Clients | 18 | CRUD, Update, GetAccounts, SetClientAccounts, InvalidAccountId, Filters (status+clientType+pepStatus+q), DateFilter, SortByDisplayName, DuplicateEmail, DeleteLinkedToAccount, RouteBodyIdMismatch, StaleRowVersion |
| Accounts | 15 | CRUD, Update, SetAccountHolders, InvalidClientId, Filters (status+accountType+q), SortByClearerName, DuplicateNumber, RouteBodyIdMismatch |
| Users | 18 | CRUD, Update, Filters (isActive+q), DuplicateUsername/Email, RouteBodyIdMismatch, Photo upload/get/delete/anonymous |
| Roles | 11 | CRUD, GetById, Update, Filters (isSystem+q), SetPermissions, DuplicateName, DeleteSystem 409, RouteBodyIdMismatch |
| Instruments | 11 | CRUD, Update, Filters (type+status+isMarginEligible+q), DuplicateSymbol, RouteBodyIdMismatch |
| Trade Orders | 14 | CRUD, Update, Filters (status+side+orderType+q), SortByInstrumentSymbol, InvalidAccount, LimitWithoutPrice, StopWithoutStopPrice, GTDWithoutExpiration, RouteBodyIdMismatch |
| Non-Trade Orders | 11 | CRUD, Update, Filters (status+nonTradeType+q), InvalidCurrencyId, InvalidAccountId, RouteBodyIdMismatch |
| Trade Transactions | 16 | CRUD, Update, StaleRowVersion, GetByOrder, InvalidOrder, Filters (status+side+q), SideMismatch, InvalidInstrumentId/OrderId, RouteBodyIdMismatch |
| Non-Trade Transactions | 16 | CRUD, Update, StaleRowVersion, GetByOrder, InvalidOrder, Filters (status+q+amountMin/Max), WithoutOrder, InvalidCurrencyId/OrderId, RouteBodyIdMismatch |
| Reference data | ~28 | Clearers, Currencies, Exchanges, TradePlatforms — List/ListAll/Create/Update/Delete/DuplicateName/DuplicateOnUpdate |
| Dashboard | 1 | Stats endpoint |
| Audit | 4 | List, GetById, Filters (isSuccess+method+q) |
| Entity Changes | 4 | List, ListAll, Filters (entityType+changeType) |
| Permissions/Countries | 2 | List endpoints |
| Permission denial | 1 | 403 для ограниченного пользователя |
| Concurrency | 8 | 409 stale RowVersion — Account, Instrument, Role, Client, TradeOrder, User, TradeTransaction, NonTradeTransaction |

### Coverage config

`backend/coverage.runsettings` исключает из метрик покрытия:
- `SeedDemoData.cs` (~3700 LOC) — демо-данные, не выполняются в тестах
- `SeedCountries.cs` — статический список стран
- `Migrations/` — автогенерированные миграции EF Core

---

## CI Pipeline

GitHub Actions: 3 параллельных job-а на каждый push в main (~1.5 мин):

1. **backend** — build + NuGet audit + 326 unit-тестов
2. **backend-integration** — 192 интеграционных теста (Testcontainers MSSQL)
3. **frontend** — tsc + eslint + 119 vitest-тестов + production build

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
