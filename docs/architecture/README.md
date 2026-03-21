# Архитектурная документация: Broker Backoffice

> Последнее обновление: 2026-03-21

## Навигация

| Документ | Описание |
|----------|----------|
| [01. Обзор системы](01-overview.md) | Технологии, структура проекта, архитектурный стиль |
| [02. Системный контекст (C4)](02-system-context.md) | Диаграммы уровней L1, L2, L3 |
| [03. Backend](03-backend.md) | API, CQRS, middleware, валидация, обработка ошибок |
| [04. Frontend](04-frontend.md) | 3 SPA (backoffice, auth, config), роутинг, компоненты, кросс-SPA навигация |
| [05. Данные](05-data.md) | Схема БД, EF Core, миграции, сущности |
| [06. DevOps & Runbook](06-devops-runbook.md) | Docker, запуск, скрипты, CI/CD |
| [07. Стратегия тестирования](07-testing-strategy.md) | Unit/integration, фреймворки, покрытие |
| [08. Безопасность](08-security.md) | JWT, RBAC, permissions, токены |
| [09. Наблюдаемость](09-observability.md) | Логирование, health checks, correlation ID |
| [10. API Gateway](10-api-gateway.md) | Config Service, REST proxy, field-level access control, YAML-конфигурация, CRUD endpoints |
| [11. Модульный фронтенд](11-frontend-modules.md) | pnpm monorepo, 3 SPA, ui-kit, auth-module, NavigationProvider, динамический sidebar |
| [12. План миграции](12-migration-plan.md) | 9 фаз перехода к целевой архитектуре, зависимости, сроки |
| [ADR](adr/) | Архитектурные решения (Architecture Decision Records) |

## Краткое описание

**Broker Backoffice** -- внутренняя система управления брокерским бэк-офисом. Позволяет администраторам управлять клиентами, счетами, инструментами, пользователями, ролями и правами доступа, просматривать аудит-лог операций и поле-уровневую историю изменений. Включает конфигуратор меню, полей сущностей и upstream-сервисов.

**Архитектурный стиль:** монолит + auth-сервис + API Gateway (Clean Architecture), 3 SPA-фронтенда (backoffice, auth, config).

**Стек:**

- Backend: .NET 8, ASP.NET Core, EF Core, MediatR, FluentValidation, Serilog
- API Gateway: .NET 8, ASP.NET Core, YamlDotNet, REST proxy
- Frontend: React 18, TypeScript, Vite, MUI 6, TanStack React Query, React Router 6, pnpm monorepo + Turborepo
- БД: PostgreSQL 17
- Инфраструктура: Docker Compose, Nginx (3 SPA routing)
