# Архитектурная документация: Broker Backoffice

> Последнее обновление: 2026-02-21

## Навигация

| Документ | Описание |
|----------|----------|
| [01. Обзор системы](01-overview.md) | Технологии, структура проекта, архитектурный стиль |
| [02. Системный контекст (C4)](02-system-context.md) | Диаграммы уровней L1, L2, L3 |
| [03. Backend](03-backend.md) | API, CQRS, middleware, валидация, обработка ошибок |
| [04. Frontend](04-frontend.md) | React, роутинг, компоненты, состояние, API-клиент |
| [05. Данные](05-data.md) | Схема БД, EF Core, миграции, сущности |
| [06. DevOps & Runbook](06-devops-runbook.md) | Docker, запуск, скрипты, CI/CD |
| [07. Стратегия тестирования](07-testing-strategy.md) | Unit/integration, фреймворки, покрытие |
| [08. Безопасность](08-security.md) | JWT, RBAC, permissions, токены |
| [09. Наблюдаемость](09-observability.md) | Логирование, health checks, correlation ID |
| [ADR](adr/) | Архитектурные решения (Architecture Decision Records) |

## Краткое описание

**Broker Backoffice** -- внутренняя система управления брокерским бэк-офисом. Позволяет администраторам управлять клиентами, счетами, пользователями, ролями и правами доступа, просматривать аудит-лог операций.

**Архитектурный стиль:** модульный монолит (Clean Architecture) с SPA-фронтендом.

**Стек:**

- Backend: .NET 8, ASP.NET Core, EF Core, MediatR, FluentValidation, Serilog
- Frontend: React 18, TypeScript, Vite, MUI 6, TanStack React Query, React Router 6
- БД: Microsoft SQL Server 2022
- Инфраструктура: Docker Compose, Nginx
