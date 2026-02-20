# 02. Системный контекст (C4-диаграммы)

## C4 Level 1 -- Контекст системы

```mermaid
C4Context
    title Системный контекст: Broker Backoffice

    Person(operator, "Оператор бэк-офиса", "Управляет клиентами, пользователями, ролями")
    System(backoffice, "Broker Backoffice", "Веб-приложение для управления брокерским бэк-офисом")
    System_Ext(browser, "Браузер", "Chrome / Firefox / Safari")

    Rel(operator, browser, "Использует")
    Rel(browser, backoffice, "HTTPS")
```

> **Примечание:** Внешних интеграций (платежные системы, KYC-провайдеры, биржи) на данный момент не обнаружено. Система работает автономно.

## C4 Level 2 -- Контейнеры

```mermaid
flowchart TB
    subgraph Docker["Docker Compose"]
        direction TB

        subgraph web["broker-web :3000"]
            nginx["Nginx Alpine"]
            spa["React SPA<br/>(статика)"]
        end

        subgraph api["broker-api :5050"]
            dotnet["ASP.NET Core 8<br/>Web API"]
        end

        subgraph db["broker-mssql :1433"]
            mssql["MS SQL Server 2022"]
        end
    end

    browser["Браузер оператора"] -->|"HTTP :3000"| nginx
    nginx -->|"Статика"| spa
    nginx -->|"Проксирование /api/"| dotnet
    dotnet -->|"EF Core (TCP :1433)"| mssql

    style web fill:#e1f5fe
    style api fill:#e8f5e9
    style db fill:#fff3e0
```

### Сетевая карта

| Контейнер | Внешний порт | Внутренний порт | Протокол |
|-----------|-------------|-----------------|----------|
| broker-web | 3000 | 80 | HTTP (Nginx) |
| broker-api | 5050 | 8080 | HTTP (Kestrel) |
| broker-mssql | 1433 | 1433 | TDS (SQL) |

### Health Checks

| Контейнер | Механизм | Интервал |
|-----------|----------|----------|
| mssql | `sqlcmd SELECT 1` | 10s, 5 retries, start 30s |
| api | TCP check на порт 8080 | 10s, 5 retries, start 20s |
| web | `wget http://127.0.0.1:80/` | 10s, 3 retries, start 5s |

## C4 Level 3 -- Компоненты Backend

```mermaid
flowchart TB
    subgraph API["API Layer"]
        controllers["Controllers<br/>Auth, Users, Roles,<br/>Clients, Audit,<br/>Countries, Permissions"]
        middleware["Middleware<br/>CorrelationId,<br/>ExceptionHandling,<br/>AuditFilter"]
    end

    subgraph App["Application Layer"]
        commands["Commands<br/>Login, CreateUser,<br/>UpdateClient, ..."]
        queries["Queries<br/>GetUsers, GetClients,<br/>GetAuditLogs, ..."]
        validators["Validators<br/>FluentValidation"]
        pipeline["MediatR Pipeline<br/>ValidationBehavior"]
    end

    subgraph Domain["Domain Layer"]
        entities["Entities<br/>User, Role, Permission,<br/>Client, AuditLog, Country"]
        enums["Enums<br/>ClientType, KycStatus,<br/>RiskLevel, Gender, ..."]
    end

    subgraph Infra["Infrastructure Layer"]
        dbcontext["AppDbContext<br/>EF Core + SQL Server"]
        jwt["JwtTokenService<br/>HMAC SHA256"]
        seed["SeedData<br/>Permissions, Countries,<br/>Admin user"]
    end

    controllers --> pipeline
    pipeline --> validators
    pipeline --> commands
    pipeline --> queries
    commands --> dbcontext
    commands --> jwt
    queries --> dbcontext
    middleware --> controllers
    dbcontext --> entities

    style API fill:#e3f2fd
    style App fill:#f3e5f5
    style Domain fill:#fff8e1
    style Infra fill:#e8f5e9
```

## C4 Level 3 -- Компоненты Frontend

```mermaid
flowchart TB
    subgraph Entry["Точка входа"]
        main["main.tsx<br/>Провайдеры"]
        router["Router<br/>React Router v6"]
    end

    subgraph Pages["Страницы"]
        login["LoginPage"]
        dashboard["DashboardPage"]
        users["UsersPage"]
        roles["RolesPage"]
        clients["ClientsPage"]
        clientDetail["ClientDetailsPage"]
        audit["AuditPage"]
        settings["SettingsPage"]
    end

    subgraph Shared["Переиспользуемые"]
        layout["MainLayout<br/>Sidebar + AppBar"]
        pageContainer["PageContainer"]
        grid["FilteredDataGrid<br/>+ InlineFilters"]
        dialogs["Диалоги<br/>Create/Edit/Permissions"]
    end

    subgraph Data["Данные"]
        apiClient["Axios Client<br/>+ Token Refresh"]
        hooks["React Query Hooks<br/>useUsers, useClients, ..."]
        authCtx["AuthContext<br/>useAuth, usePermission"]
    end

    main --> router
    router --> login
    router --> layout
    layout --> dashboard & users & roles & clients & audit & settings
    clients --> clientDetail
    users & roles & clients & audit --> grid
    users & roles & clients --> dialogs
    grid --> hooks
    hooks --> apiClient
    layout --> authCtx

    style Entry fill:#e3f2fd
    style Pages fill:#f3e5f5
    style Shared fill:#fff8e1
    style Data fill:#e8f5e9
```
