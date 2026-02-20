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
| `SA_PASSWORD` | Пароль SA для MS SQL | `Your_Strong_Password123` |
| `JWT_SECRET` | Секрет подписи JWT (min 32 chars) | `this-is-a-development-secret-key-min-32-chars!!` |
| `ADMIN_PASSWORD` | Пароль admin-пользователя | `Admin123!` |
| `DEFAULT_DEMO_PASSWORD` | Пароль для demo-пользователей | `Admin123!` |
| `SEED_DEMO_DATA` | Засеять тестовые данные | `false` |

### Docker-сервисы

| Сервис | Image | Порт | Зависит от |
|--------|-------|------|-----------|
| mssql | mcr.microsoft.com/mssql/server:2022-latest | 1433 | - |
| api | Dockerfile.api (multi-stage .NET 8) | 5050 -> 8080 | mssql (healthy) |
| web | Dockerfile.web (Node 20 build + Nginx) | 3000 -> 80 | api (healthy) |

### Dockerfile.api

Multi-stage сборка:
1. **Build stage**: .NET 8 SDK, `dotnet restore` + `dotnet publish -c Release`
2. **Runtime stage**: ASP.NET 8 runtime, non-root user `appuser`, порт 8080

### Dockerfile.web

Multi-stage сборка:
1. **Build stage**: Node 20 Alpine, `npm ci` + `npm run build` (tsc + vite)
2. **Runtime stage**: Nginx Alpine, копирование `dist/` + `nginx.conf`

### Nginx конфигурация

```
/ -> /usr/share/nginx/html (SPA, fallback index.html)
/api/ -> http://api:8080/api/ (reverse proxy к backend)
```

## Разработка без Docker

### Backend

```bash
cd backend
dotnet restore
dotnet run --project src/Broker.Backoffice.Api
# API доступен на http://localhost:5050
```

Требуется доступный MS SQL (можно использовать Docker-контейнер `mssql` отдельно).

### Frontend

```bash
cd frontend
npm install
npm run dev
# Dev-сервер на http://localhost:5173, /api проксируется на localhost:5050
```

## Скрипты

### npm-скрипты (frontend/package.json)

| Скрипт | Команда |
|--------|---------|
| `dev` | `vite` (dev server, port 5173) |
| `build` | `tsc -b && vite build` |
| `test` | `vitest run -c vitest.config.ts` |
| `test:ci` | `vitest run -c vitest.config.ts --coverage` |
| `build:ci` | `npm run test:ci && npm run build` |
| `lint` | `eslint .` |
| `generate:api` | `orval` (генерация API-клиента) |

### npm-скрипты (корневой package.json)

| Скрипт | Команда |
|--------|---------|
| `test` | `cd frontend && npm test` |
| `test:ci` | `cd frontend && npm run test:ci` |

### Shell-скрипты (scripts/)

| Скрипт | Назначение |
|--------|------------|
| `smoke.sh` | Smoke-тест: проверка health endpoints, базовая проверка API |
| `db_check.sh` | Проверка доступности SQL Server |
| `test.sh` | Запуск тестов |

## CI/CD

> **Не обнаружено.** В репозитории нет `.github/workflows/`, `.gitlab-ci.yml`, `Jenkinsfile` или аналогов.

### Рекомендуемый минимальный пайплайн

```yaml
# Пример для GitHub Actions
stages:
  - lint (eslint, dotnet format)
  - test:unit (npm test, dotnet test --filter Unit)
  - build (docker compose build)
  - test:integration (docker compose up + smoke.sh)
  - deploy (push images, deploy)
```

## Troubleshooting

### Порты заняты

```bash
# Проверить что занимает порт
lsof -i :3000  # web
lsof -i :5050  # api
lsof -i :1433  # mssql
```

### Node версия

Файл `frontend/.nvmrc` указывает Node 20. При использовании nvm:
```bash
cd frontend && nvm use
```

### OOM при тестах

Unit-тесты (`npm test`) настроены на минимальный набор (только hooks/auth). Тяжёлые page-тесты с MUI DataGrid исключены из default suite для предотвращения OOM в jsdom-окружении.

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
