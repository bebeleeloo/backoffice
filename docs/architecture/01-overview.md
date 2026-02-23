# 01. Обзор системы

## Назначение

Broker Backoffice -- веб-приложение для внутренних операторов брокерской компании. Основные функции:

- **Управление клиентами** (Individual / Corporate): создание, редактирование, KYC, инвестиционные профили, адреса
- **Управление счетами** (Accounts): создание, редактирование, привязка клиентов-холдеров (many-to-many)
- **Управление пользователями** системы: CRUD, привязка ролей
- **Управление инструментами** (Instruments): Stock, Bond, ETF и др. с привязкой к биржам и валютам
- **Управление ролями и правами**: RBAC с гранулярными permissions
- **Дашборд**: счётчики и графики (распределение по типам, статусам, классам активов)
- **Настройки**: профиль пользователя (смена пароля, email), CRUD справочников (Clearers, Trade Platforms, Exchanges, Currencies)
- **Аудит-лог**: запись всех мутаций с поле-уровневой историей изменений, фильтрация

## Структура репозитория

```
new-back/
  docker-compose.yml          # Оркестрация всех сервисов
  Dockerfile.api              # Сборка backend
  Dockerfile.web              # Сборка frontend (multi-stage + nginx)
  .env / .env.example         # Переменные окружения
  scripts/                    # Утилитарные скрипты (smoke, db_check, test)
  package.json                # Прокси: npm test -> frontend

  backend/
    Broker.Backoffice.sln
    Directory.Build.props      # .NET 8, strict mode, warnings as errors
    src/
      Broker.Backoffice.Api/           # ASP.NET Core Web API (контроллеры, middleware)
      Broker.Backoffice.Application/   # CQRS handlers, validators, DTOs
      Broker.Backoffice.Domain/        # Доменные сущности, enums, value objects
      Broker.Backoffice.Infrastructure/# EF Core, JWT, seed data, persistence
    tests/
      Broker.Backoffice.Tests.Unit/
      Broker.Backoffice.Tests.Integration/

  frontend/
    package.json               # React SPA
    vite.config.ts             # Dev server + proxy /api -> localhost:5050
    vitest.config.ts           # Unit-тесты (hooks/auth/lib/utils)
    src/
      main.tsx                 # Точка входа: провайдеры (QueryClient, Theme, Auth, Router)
      router/                  # React Router v6 (protected routes)
      api/                     # Axios client + React Query hooks
      auth/                    # AuthContext, useAuth, usePermission, RequireAuth
      pages/                   # Страницы (Dashboard, Login, Users, Roles, Clients, Accounts, Audit, Settings)
      layouts/                 # MainLayout (sidebar + AppBar)
      components/              # Переиспользуемые компоненты (PageContainer, grid/*, dialogs)
      theme/                   # MUI theme + compact list variant
      test/                    # Утилиты тестирования (factories, MSW handlers, renderWithProviders)
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
| БД | MS SQL Server | 2022 |

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
| Графики | Recharts | 2.x |
| HTTP клиент | Axios | 1.7 |
| Тесты | Vitest + React Testing Library | 2.0 / 16.0 |
| Моки API | MSW | 2.0 |
| Тестовые данные | Faker.js | 9.0 |

### Инфраструктура

| Компонент | Технология |
|-----------|------------|
| Контейнеризация | Docker / Docker Compose |
| Web-сервер (prod) | Nginx Alpine |
| Node (сборка) | 20 Alpine |
| CI/CD | Не обнаружено (нет .github/workflows, .gitlab-ci.yml) |

## Архитектурный стиль

**Модульный монолит** на основе Clean Architecture:

```
Domain (центр)  <-  Application  <-  Infrastructure  <-  API
```

- **Domain** -- чистые сущности, enums. Нет зависимостей от внешних пакетов.
- **Application** -- команды/запросы (CQRS через MediatR), валидаторы, DTOs. Зависит только от Domain.
- **Infrastructure** -- EF Core контекст, JWT сервис, seed data. Зависит от Application + Domain.
- **API** -- контроллеры, middleware, DI композиция. Зависит от всех слоев.

Frontend -- отдельный SPA, взаимодействует с backend исключительно через REST API `/api/v1/*`.
