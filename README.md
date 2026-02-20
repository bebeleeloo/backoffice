# Broker Backoffice

Production-ready monorepo: .NET 8 API + React frontend.

## Architecture

```
backend/
  src/
    Broker.Backoffice.Api            — ASP.NET Core Web API (Swagger, health, middleware)
    Broker.Backoffice.Application    — MediatR, FluentValidation, abstractions
    Broker.Backoffice.Domain         — Domain entities, value objects
    Broker.Backoffice.Infrastructure — EF Core, external services
  tests/
    Broker.Backoffice.Tests.Unit         — xUnit + FluentAssertions
    Broker.Backoffice.Tests.Integration  — WebApplicationFactory + Testcontainers
frontend/                           — Vite + React + TypeScript + MUI
```

## Quick Start (Docker)

```bash
cp .env.example .env              # create local env (gitignored)
docker compose up --build -d      # build & start all services
```

| Service  | URL                          |
|----------|------------------------------|
| API      | http://localhost:5050/api/v1  |
| Swagger  | http://localhost:5050/swagger |
| Frontend | http://localhost:3000         |
| MSSQL    | localhost:1433               |

Database is created and migrations are applied automatically on API startup. No manual SQL steps required.

## Login Credentials

Default admin account seeded on first startup:

| Field    | Value       |
|----------|-------------|
| Username | `admin`     |
| Password | `Admin123!` |

Override via `ADMIN_PASSWORD` in `.env`.

**curl example (note the `!` character):**

```bash
# Use single quotes around the JSON to prevent shell expansion of '!'
curl -s http://localhost:5050/api/v1/auth/login \
  -H 'Content-Type: application/json' \
  -d '{"username":"admin","password":"Admin123!"}'
```

> In zsh the `!` triggers history expansion inside double quotes.
> Either use single quotes (as above), or `set +H` to disable it for the session.

## Environment Variables

All secrets are in `.env` (gitignored). Copy `.env.example` to get started:

| Variable         | Description                          | Default in .env.example           |
|------------------|--------------------------------------|-----------------------------------|
| `SA_PASSWORD`    | SQL Server SA password (avoid `;` — it breaks the connection string) | `Your_Strong_Password123` |
| `JWT_SECRET`     | HMAC-SHA256 key for JWT signing      | dev key (32+ chars)               |
| `ADMIN_PASSWORD` | Seeded admin password                | `Admin123!`                       |

## Scripts

```bash
# Full smoke test: rebuild from scratch, check health/auth/API/frontend
./scripts/smoke.sh --clean

# Fast smoke test: checks only, no rebuild (services must be running)
./scripts/smoke.sh --fast

# Database check: verify tables, seed data, admin user/role
./scripts/db_check.sh

# Run backend unit tests in Docker (no local dotnet needed)
./scripts/test.sh unit

# Run all backend tests
./scripts/test.sh all
```

## Local Development

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

## Database Migrations

```bash
cd backend

# Create migration
dotnet ef migrations add MigrationName \
  --project src/Broker.Backoffice.Infrastructure \
  --startup-project src/Broker.Backoffice.Api

# Apply migration
dotnet ef database update \
  --project src/Broker.Backoffice.Infrastructure \
  --startup-project src/Broker.Backoffice.Api
```

## Tests

```bash
cd backend

# Unit tests
dotnet test tests/Broker.Backoffice.Tests.Unit

# Integration tests (requires Docker for Testcontainers)
dotnet test tests/Broker.Backoffice.Tests.Integration
```

## Health Checks

- `GET /health/live` — Liveness (app is running)
- `GET /health/ready` — Readiness (app + SQL Server)
