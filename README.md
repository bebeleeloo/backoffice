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

## Health Checks

- `GET /health/live` — Liveness (приложение запущено)
- `GET /health/ready` — Readiness (приложение + SQL Server)
