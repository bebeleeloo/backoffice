# 01. Обзор системы

## Назначение

Broker Backoffice -- веб-приложение для внутренних операторов брокерской компании. Основные функции:

- **Управление клиентами** (Individual / Corporate): создание, редактирование, KYC, инвестиционные профили, адреса
- **Управление счетами** (Accounts): создание, редактирование, привязка клиентов-холдеров (many-to-many)
- **Управление пользователями** системы: CRUD, привязка ролей
- **Управление инструментами** (Instruments): Stock, Bond, ETF и др. с привязкой к биржам и валютам
- **Управление поручениями** (Orders): торговые (Buy/Sell, Market/Limit/Stop) и неторговые (Deposit/Withdrawal/Dividend и др.)
- **Управление ролями и правами**: RBAC с гранулярными permissions
- **Дашборд**: счётчики и графики (распределение по типам, статусам, классам активов)
- **Настройки**: профиль пользователя (смена пароля, email), CRUD справочников (Clearers, Trade Platforms, Exchanges, Currencies)
- **Аудит-лог**: запись всех мутаций с поле-уровневой историей изменений, фильтрация

## Структура репозитория

```
new-back/
  docker-compose.yml          # Оркестрация всех сервисов (postgres, auth, api, web)
  Dockerfile.api              # Сборка backend (монолит)
  Dockerfile.auth             # Сборка auth-service
  Dockerfile.web              # Сборка frontend (multi-stage + nginx)
  .env / .env.example         # Переменные окружения
  scripts/                    # Утилитарные скрипты (smoke, db_check, test)
  package.json                # Прокси: npm test -> frontend

  backend/                     # Монолит (клиенты, счета, инструменты, ордера, транзакции, аудит)
    Broker.Backoffice.sln
    Directory.Build.props      # .NET 8, strict mode, warnings as errors
    src/
      Broker.Backoffice.Api/           # ASP.NET Core Web API (контроллеры, middleware)
      Broker.Backoffice.Application/   # CQRS handlers, validators, DTOs
      Broker.Backoffice.Domain/        # Доменные сущности, enums, value objects
      Broker.Backoffice.Infrastructure/# EF Core, JWT validation, seed data, persistence
    tests/
      Broker.Backoffice.Tests.Unit/
      Broker.Backoffice.Tests.Integration/

  auth-service/                # Микросервис аутентификации и управления пользователями/ролями
    Broker.Auth.sln
    src/
      Broker.Auth.Api/                 # Auth endpoints, Users, Roles, Permissions controllers
      Broker.Auth.Application/         # CQRS handlers, validators, DTOs
      Broker.Auth.Domain/              # Identity entities (User, Role, Permission)
      Broker.Auth.Infrastructure/      # EF Core (auth.* schema), JWT issuance, seed data
    tests/
      Broker.Auth.Tests.Unit/
      Broker.Auth.Tests.Integration/

  gateway/                     # API Gateway (.NET 8, YARP, YAML-конфигурация)
    gateway.sln
    src/
      Broker.Gateway.Api/             # Controllers, Services (ConfigLoader, YARP), Middleware
    config/
      menu.yaml                      # Sidebar меню: структура, permissions
      entities.yaml                  # Видимость полей сущностей по ролям
      upstreams.yaml                 # Upstream-маршруты (api, auth)

  frontend/                    # pnpm monorepo + Turborepo (3 SPA)
    pnpm-workspace.yaml        # packages/* + apps/*
    turbo.json                 # build, dev, test, lint
    tsconfig.base.json         # Общий TS-конфиг
    packages/
      ui-kit/                  # @broker/ui-kit — компоненты, тема, auth, API client, навигация
      auth-module/             # @broker/auth-module — LoginPage, UsersPage, RolesPage
    apps/
      backoffice/              # Основное SPA — клиенты, счета, инструменты, ордера, дашборд, аудит
      auth/                    # Auth SPA — логин, пользователи, роли (порт 5174)
      config/                  # Config SPA — конфигурация меню, сущностей, upstreams (порт 5175)
```

## Технологический стек

### Backend

| Компонент | Технология | Версия |
|-----------|------------|--------|
| Runtime | .NET | 8.0 |
| Web framework | ASP.NET Core | 8.0 |
| ORM | Entity Framework Core | 8.x |
| CQRS / Mediator | MediatR | auto |
| Валидация | FluentValidation | auto |
| Логирование | Serilog | auto |
| Аутентификация | JWT Bearer | built-in |
| Хеширование паролей | ASP.NET Identity PasswordHasher | built-in |
| БД | PostgreSQL | 17 |

### Frontend

| Компонент | Технология | Версия |
|-----------|------------|--------|
| UI фреймворк | React | 18.3 |
| Язык | TypeScript | 5.6 |
| Сборка | Vite | 5.4 |
| UI Kit | MUI (Material UI) | 6.1 |
| Таблицы | MUI X Data Grid | 7.18 |
| Даты | MUI X Date Pickers + DayJS | 7.18 |
| Роутинг | React Router | 6.26 |
| Серверное состояние | TanStack React Query | 5.56 |
| Графики | Recharts | 3.x |
| HTTP клиент | Axios | 1.7 |
| Тесты | Vitest + React Testing Library | 2.0 / 16.0 |
| Моки API | MSW | 2.0 |
| Тестовые данные | Faker.js | 10.x |

### Инфраструктура

| Компонент | Технология |
|-----------|------------|
| Контейнеризация | Docker / Docker Compose |
| Web-сервер (prod) | Nginx Alpine |
| Node (сборка) | 20 Alpine (Docker), 22 (CI) |
| CI/CD | GitHub Actions (7 параллельных job: backend unit, backend integration, auth-service unit, auth-service integration, gateway build, permissions-sync, frontend) |

## Архитектурный стиль

**Монолит + выделенный auth-сервис.** Аутентификация и управление пользователями/ролями/правами вынесены в отдельный микросервис (`auth-service`). Остальная бизнес-логика (клиенты, счета, инструменты, ордера, транзакции, аудит) остаётся в монолите (`backend`).

Оба сервиса следуют Clean Architecture:

```
Domain (центр)  <-  Application  <-  Infrastructure  <-  API
```

- **Domain** -- чистые сущности, enums. Нет зависимостей от внешних пакетов.
- **Application** -- команды/запросы (CQRS через MediatR), валидаторы, DTOs. Зависит только от Domain.
- **Infrastructure** -- EF Core контекст, seed data. Зависит от Application + Domain.
- **API** -- контроллеры, middleware, DI композиция. Зависит от всех слоев.

**Разделение ответственности:**
- **auth-service** -- JWT-выдача, refresh token rotation, логин, Users/Roles/Permissions CRUD, фото пользователей. Использует схему `auth.*` в БД.
- **Монолит (backend)** -- JWT-валидация (локальная проверка claims, без обращения к auth-service), бизнес-операции. Использует схему `public.*` в БД. Вызывает auth-service через `IAuthServiceClient` только для получения статистики пользователей (dashboard).
- **`Permissions.cs`** (строковые константы прав) дублируется в обоих сервисах.

Frontend -- 3 SPA (backoffice, auth, config) в pnpm monorepo, взаимодействуют с backend через REST API `/api/v1/*`. API Gateway (YARP) маршрутизирует запросы к соответствующему сервису. Nginx раздаёт 3 SPA по path.
