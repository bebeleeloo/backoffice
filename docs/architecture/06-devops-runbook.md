# 06. DevOps & Runbook

## Запуск локально (Docker Compose)

### Предварительные требования

- Docker Desktop (с Docker Compose v2)
- Node 20+ (для фронтенд-разработки без Docker)
- .NET 8 SDK (для бэкенд-разработки без Docker)

### Быстрый старт

```bash
# 1. Клонировать репозиторий
git clone <repo-url> new-back && cd new-back

# 2. Создать .env (или скопировать пример)
cp .env.example .env
# Отредактировать пароли при необходимости

# 3. Собрать и запустить
docker compose up --build -d

# 4. Проверить статус
docker compose ps

# 5. Открыть приложение
open http://localhost:3000
```

### Переменные окружения (.env)

| Переменная | Назначение | Пример |
|-----------|------------|--------|
| `PG_PASSWORD` | Пароль PostgreSQL | `Your_Strong_Password123` |
| `JWT_SECRET` | Секрет подписи JWT (min 32 chars) | `this-is-a-development-secret-key-min-32-chars!!` |
| `ADMIN_PASSWORD` | Пароль admin-пользователя | `Admin123!` |
| `DEFAULT_DEMO_PASSWORD` | Пароль для demo-пользователей | `Admin123!` |
| `SEED_DEMO_DATA` | Засеять тестовые данные | `false` |

### Docker-сервисы

| Сервис | Image | Порт | Зависит от |
|--------|-------|------|-----------|
| postgres | postgres:16-alpine | 5432 | - |
| auth | Dockerfile.auth (multi-stage .NET 8) | 8082 -> 8080 | postgres (healthy) |
| api | Dockerfile.api (multi-stage .NET 8) | 5050 -> 8080 | postgres (healthy), auth (healthy) |
| gateway | Dockerfile.gateway (.NET 8) | 8090 -> 8090 | postgres (healthy) |
| web | Dockerfile.web (Node 20 build + Nginx) | 3000 -> 8080 | api (healthy) |

### Dockerfile.api

Multi-stage сборка:
1. **Build stage**: .NET 8 SDK, `dotnet restore` + `dotnet publish -c Release`
2. **Runtime stage**: ASP.NET 8 runtime, non-root user `appuser`, порт 8080

### Dockerfile.web

Multi-stage сборка (3 SPA-приложения через pnpm + Turborepo):
1. **Build stage**: Node 20 Alpine, `corepack enable && corepack prepare pnpm@latest`, `pnpm install --frozen-lockfile`, `pnpm turbo build` (все 3 приложения)
2. **Runtime stage**: Nginx Alpine
   - `COPY apps/backoffice/dist → /usr/share/nginx/html/backoffice/`
   - `COPY apps/auth/dist → /usr/share/nginx/html/auth/`
   - `COPY apps/config/dist → /usr/share/nginx/html/config/`
   - `COPY logo.svg → /usr/share/nginx/html/logo.svg`
   - `COPY frontend/nginx.conf → /etc/nginx/conf.d/default.conf`

### Nginx конфигурация

Маршрутизация по 3 SPA-приложениям + API через gateway:

```
/login, /users*, /roles*           → try_files /auth/index.html         (Auth SPA)
/config*                           → try_files /config/index.html       (Config SPA)
/ (всё остальное)                  → try_files $uri $uri/ /backoffice/index.html (Backoffice SPA)
/api/                              → proxy_pass gateway:8090            (API Gateway)
~^/(backoffice|auth|config)/assets/ → immutable cache 1y               (хэшированные ассеты)
```

## Разработка без Docker

### Backend

```bash
cd backend
dotnet restore
dotnet run --project src/Broker.Backoffice.Api
# API доступен на http://localhost:5050
```

Требуется доступный PostgreSQL (можно использовать Docker-контейнер `postgres` отдельно).

### Frontend

```bash
cd frontend
pnpm install
pnpm turbo dev
# 3 dev-сервера: backoffice :5173, auth :5174, config :5175
# /api проксируется на localhost:8090 (gateway)
```

Для запуска отдельного приложения:
```bash
pnpm turbo dev --filter=@broker/backoffice
```

## Скрипты

### pnpm/turbo-скрипты (frontend/)

pnpm monorepo с Turborepo — скрипты запускаются из корня `frontend/`:

| Скрипт | Команда |
|--------|---------|
| `dev` | `pnpm turbo dev` (все 3 приложения) |
| `build` | `pnpm turbo build` |
| `test` | `pnpm turbo test` |
| `lint` | `pnpm turbo lint` |

Для запуска отдельного приложения используется фильтр:
```bash
pnpm turbo dev --filter=@broker/backoffice
pnpm turbo dev --filter=@broker/auth
pnpm turbo dev --filter=@broker/config
```

### Shell-скрипты (scripts/)

| Скрипт | Назначение |
|--------|------------|
| `smoke.sh` | Smoke-тест: проверка health endpoints, базовая проверка API |
| `db_check.sh` | Проверка доступности PostgreSQL |
| `test.sh` | Запуск тестов |

## CI/CD

**GitHub Actions** (`.github/workflows/ci.yml`) — 5 параллельных job на каждый push/PR в main:

| Job | Шаги | Время |
|-----|------|-------|
| `backend` | checkout → .NET 8 SDK → NuGet cache → build → NuGet audit → 273 unit-тестов | ~30с |
| `backend-integration` | checkout → .NET 8 SDK → NuGet cache → 145 интеграционных тестов (Testcontainers PostgreSQL) | ~1.5 мин |
| `auth-service` | checkout → .NET 8 SDK → NuGet cache → build → NuGet audit → 25 unit-тестов | ~20с |
| `auth-service-integration` | checkout → .NET 8 SDK → NuGet cache → 13 интеграционных тестов (Testcontainers PostgreSQL) | ~30с |
| `frontend` | checkout → Node 22 → pnpm install → pnpm turbo build → pnpm turbo lint → 109 vitest-тестов | ~1 мин |

## Troubleshooting

### Порты заняты

```bash
# Проверить что занимает порт
lsof -i :3000  # web
lsof -i :5050  # api
lsof -i :5432  # postgres
```

### Node версия

pnpm monorepo — Node 20+ требуется для сборки. CI использует Node 22. Менеджер пакетов: pnpm (устанавливается через `corepack enable`).

### Миграции БД

Миграции применяются автоматически при старте API. Для ручного применения:
```bash
cd backend
dotnet ef database update --project src/Broker.Backoffice.Infrastructure --startup-project src/Broker.Backoffice.Api
```

### Пересборка Docker

```bash
docker compose down
docker compose up --build -d
```
