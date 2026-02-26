# Broker Backoffice

Монорепозиторий: .NET 8 API + React-фронтенд.

## Архитектура

```
backend/
  src/
    Broker.Backoffice.Api            — ASP.NET Core Web API (Swagger, health, middleware)
    Broker.Backoffice.Application    — MediatR, FluentValidation, абстракции
    Broker.Backoffice.Domain         — Доменные сущности, value objects
    Broker.Backoffice.Infrastructure — EF Core, внешние сервисы
  tests/
    Broker.Backoffice.Tests.Unit         — xUnit + FluentAssertions
    Broker.Backoffice.Tests.Integration  — WebApplicationFactory + Testcontainers
frontend/                           — Vite + React + TypeScript + MUI
docs/architecture/                  — Архитектурная документация, C4-диаграммы, ADR
```

## Быстрый старт (Docker)

```bash
cp .env.example .env              # создать локальный .env (в git не попадает)
docker compose up --build -d      # собрать и запустить все сервисы
```

| Сервис   | URL                          |
|----------|------------------------------|
| API      | http://localhost:5050/api/v1  |
| Swagger  | http://localhost:5050/swagger |
| Фронтенд | http://localhost:3000        |
| MSSQL    | localhost:1433               |

База данных создаётся и миграции применяются автоматически при старте API. Ручных SQL-действий не требуется.

## Учётные данные

При первом запуске создаётся администратор:

| Поле     | Значение    |
|----------|-------------|
| Логин    | `admin`     |
| Пароль   | `Admin123!` |

Пароль можно переопределить через `ADMIN_PASSWORD` в `.env`.

**Пример запроса (обратите внимание на `!`):**

```bash
# Используйте одинарные кавычки, чтобы shell не раскрывал '!'
curl -s http://localhost:5050/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin","password":"Admin123!"}'
```

> В zsh символ `!` внутри двойных кавычек вызывает history expansion.
> Используйте одинарные кавычки (как выше) или `set +H` для отключения.

## Переменные окружения

Все секреты хранятся в `.env` (игнорируется git). Скопируйте `.env.example` для начала:

| Переменная       | Описание                             | Значение по умолчанию             |
|------------------|--------------------------------------|-----------------------------------|
| `SA_PASSWORD`    | Пароль SA для SQL Server (избегайте `;` — ломает connection string) | `Your_Strong_Password123` |
| `JWT_SECRET`     | HMAC-SHA256 ключ для подписи JWT     | dev-ключ (32+ символов)           |
| `ADMIN_PASSWORD` | Пароль администратора при seed        | `Admin123!`                       |

## Скрипты

```bash
# Полный smoke-тест: пересборка с нуля, проверка health/auth/API/фронтенда
./scripts/smoke.sh --clean

# Быстрый smoke-тест: только проверки, без пересборки (сервисы должны работать)
./scripts/smoke.sh --fast

# Проверка БД: таблицы, seed-данные, admin-пользователь/роль
./scripts/db_check.sh

# Запуск backend unit-тестов в Docker (локальный dotnet не нужен)
./scripts/test.sh unit

# Запуск всех backend-тестов
./scripts/test.sh all
```

## Локальная разработка

### Backend

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project src/Broker.Backoffice.Api
```

### Frontend

```bash
cd frontend
npm install
npm run dev
```

## Миграции базы данных

```bash
cd backend

# Создать миграцию
dotnet ef migrations add MigrationName \
  --project src/Broker.Backoffice.Infrastructure \
  --startup-project src/Broker.Backoffice.Api

# Применить миграцию
dotnet ef database update \
  --project src/Broker.Backoffice.Infrastructure \
  --startup-project src/Broker.Backoffice.Api
```

## Тесты

```bash
cd backend

# Unit-тесты
dotnet test tests/Broker.Backoffice.Tests.Unit

# Интеграционные тесты (требуется Docker для Testcontainers)
dotnet test tests/Broker.Backoffice.Tests.Integration
```

## Доменные сущности

| Сущность    | CRUD | Тестовых данных | Описание                                         |
|-------------|------|-----------------|--------------------------------------------------|
| User        | CRUD | 10              | Пользователи системы, привязка к ролям           |
| Role        | CRUD | 3               | Роли (Manager, Viewer, Operator) + Admin          |
| Permission  | R    | 27              | Разрешения: clients/accounts/instruments/orders/users/roles/audit/permissions/settings |
| Client      | CRUD | 100             | Клиенты (Individual/Corporate), KYC, адреса, инвест-профиль |
| Account     | CRUD | 150             | Торговые счета, типы маржи, тарифы, холдеры       |
| Instrument  | CRUD | 300             | Торговые инструменты: Stock, Bond, ETF, Option, Future, Forex, CFD, MutualFund, Warrant, Index |
| Exchange    | R    | 15              | Биржи (NYSE, NASDAQ, LSE, TSE, HKEX и др.)       |
| Currency    | R    | 15              | Валюты (USD, EUR, GBP, JPY и др.)                |
| Country     | R    | 250             | Страны мира (ISO 3166)                           |
| TradeOrder  | CRUD | —               | Торговые поручения (Buy/Sell, Market/Limit/Stop)  |
| NonTradeOrder | CRUD | —             | Неторговые поручения (Deposit/Withdrawal/Dividend) |
| AuditLog    | R    | —               | Журнал аудита всех мутаций                       |

### Instrument — поля

| Поле              | Тип          | Описание                        |
|-------------------|--------------|---------------------------------|
| Symbol            | string(20)   | Тикер, уникальный               |
| Name              | string(255)  | Полное название                  |
| ISIN              | string(12)?  | International Securities ID      |
| CUSIP             | string(9)?   | Committee on Uniform Securities ID |
| Type              | enum         | Stock/Bond/ETF/Option/Future/Forex/CFD/MutualFund/Warrant/Index |
| AssetClass        | enum         | Equities/FixedIncome/Derivatives/ForeignExchange/Commodities/Funds |
| Status            | enum         | Active/Inactive/Delisted/Suspended |
| Exchange          | FK?          | Биржа                           |
| Currency          | FK?          | Валюта торгов                   |
| Country           | FK?          | Страна                          |
| Sector            | enum?        | 12 секторов (Technology, Healthcare, ...) |
| LotSize           | int          | Размер лота (default 1)         |
| TickSize          | decimal?     | Минимальный шаг цены            |
| MarginRequirement | decimal?     | Требование маржи (%)            |
| IsMarginEligible  | bool         | Доступен для маржинальной торговли |
| ListingDate       | DateTime?    | Дата листинга                   |
| ExpirationDate    | DateTime?    | Дата экспирации (опционы/фьючерсы) |
| IssuerName        | string?      | Эмитент                         |
| ExternalId        | string(64)?  | Внешний идентификатор            |

## API-эндпоинты

| Метод  | Путь                                | Разрешение          |
|--------|-------------------------------------|---------------------|
| POST   | `/api/v1/auth/login`                | —                   |
| GET    | `/api/v1/auth/me`                   | authenticated       |
| GET    | `/api/v1/users`                     | users.read          |
| GET    | `/api/v1/users/:id`                 | users.read          |
| POST   | `/api/v1/users`                     | users.create        |
| PUT    | `/api/v1/users/:id`                 | users.update        |
| DELETE | `/api/v1/users/:id`                 | users.delete        |
| GET    | `/api/v1/roles`                     | roles.read          |
| GET    | `/api/v1/roles/:id`                 | roles.read          |
| POST   | `/api/v1/roles`                     | roles.create        |
| PUT    | `/api/v1/roles/:id`                 | roles.update        |
| DELETE | `/api/v1/roles/:id`                 | roles.delete        |
| PUT    | `/api/v1/roles/:id/permissions`     | roles.update        |
| GET    | `/api/v1/permissions`               | permissions.read    |
| GET    | `/api/v1/clients`                   | clients.read        |
| GET    | `/api/v1/clients/:id`               | clients.read        |
| POST   | `/api/v1/clients`                   | clients.create      |
| PUT    | `/api/v1/clients/:id`               | clients.update      |
| DELETE | `/api/v1/clients/:id`               | clients.delete      |
| GET    | `/api/v1/clients/:id/accounts`      | clients.read        |
| PUT    | `/api/v1/clients/:id/accounts`      | clients.update      |
| GET    | `/api/v1/accounts`                  | accounts.read       |
| GET    | `/api/v1/accounts/:id`              | accounts.read       |
| POST   | `/api/v1/accounts`                  | accounts.create     |
| PUT    | `/api/v1/accounts/:id`              | accounts.update     |
| DELETE | `/api/v1/accounts/:id`              | accounts.delete     |
| PUT    | `/api/v1/accounts/:id/holders`      | accounts.update     |
| GET    | `/api/v1/instruments`               | instruments.read    |
| GET    | `/api/v1/instruments/:id`           | instruments.read    |
| POST   | `/api/v1/instruments`               | instruments.create  |
| PUT    | `/api/v1/instruments/:id`           | instruments.update  |
| DELETE | `/api/v1/instruments/:id`           | instruments.delete  |
| GET    | `/api/v1/exchanges`                 | instruments.read    |
| GET    | `/api/v1/currencies`                | instruments.read    |
| GET    | `/api/v1/countries`                 | clients.read        |
| GET    | `/api/v1/clearers`                  | accounts.read       |
| GET    | `/api/v1/trade-platforms`           | accounts.read       |
| GET    | `/api/v1/audit`                     | audit.read          |
| GET    | `/api/v1/audit/:id`                 | audit.read          |
| GET    | `/api/v1/trade-orders`              | orders.read         |
| GET    | `/api/v1/trade-orders/:id`          | orders.read         |
| POST   | `/api/v1/trade-orders`              | orders.create       |
| PUT    | `/api/v1/trade-orders/:id`          | orders.update       |
| DELETE | `/api/v1/trade-orders/:id`          | orders.delete       |
| GET    | `/api/v1/non-trade-orders`          | orders.read         |
| GET    | `/api/v1/non-trade-orders/:id`      | orders.read         |
| POST   | `/api/v1/non-trade-orders`          | orders.create       |
| PUT    | `/api/v1/non-trade-orders/:id`      | orders.update       |
| DELETE | `/api/v1/non-trade-orders/:id`      | orders.delete       |
| GET    | `/api/v1/entity-changes`            | audit.read          |

## Health Checks

- `GET /health/live` — Liveness (приложение запущено)
- `GET /health/ready` — Readiness (приложение + SQL Server)
