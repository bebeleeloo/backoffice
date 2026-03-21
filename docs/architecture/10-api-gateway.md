# 10. API Gateway (Config Service)

## Назначение

API Gateway -- центральный сервис-прослойка между внешними потребителями (Frontend, n8n, внешние интеграции) и внутренними сервисами (Monolith, Auth Service, будущие сервисы). Объединяет три ключевые функции:

1. **Конфигурация видимости** -- YAML-конфиг определяет какие сущности, поля, действия и эндпоинты доступны для каждой роли и каждого потребителя
2. **Reverse proxy** -- принимает REST от внешних клиентов, маршрутизирует к внутренним сервисам через YARP (настраивается per-upstream в `upstreams.yaml`)
3. **Field-level access control** -- фильтрует поля в ответах и запросах на основании конфигурации и роли текущего пользователя

### Зачем нужен

| Проблема | Решение через Gateway |
|----------|-----------------------|
| Фронтенд и n8n напрямую вызывают разные сервисы | Единая точка входа, один base URL |
| Нельзя скрыть поле/сущность без деплоя кода | YAML-конфиг: изменил -> перезагрузил -> готово |
| Разные роли должны видеть разные поля (PII, compliance) | Field-level RBAC на уровне gateway |
| Межсервисное взаимодействие через nginx -- ручная маршрутизация | YARP reverse proxy автоматически из YAML конфига |
| nginx вручную маршрутизирует /api/ по сервисам | Gateway маршрутизирует автоматически из конфига |
| n8n нужен чистый Swagger для построения workflows | Gateway генерирует Swagger из конфига |
| Добавление нового сервиса требует правок nginx, docker-compose | Достаточно добавить upstream в YAML |

---

## Целевая архитектура

```
                         ┌──────────────────────────────────────────────┐
                         │              API Gateway (.NET 8)            │
                         │                                              │
  Frontend ──REST───────▶│  ┌──────────┐ ┌───────────┐ ┌────────────┐ │
                         │  │  YAML    │ │   YARP    │ │  Swagger   │ │
  n8n      ──REST───────▶│  │  Config  │ │  Reverse  │ │ Generator  │ │
                         │  └──────────┘ │  Proxy    │ └────────────┘ │
  External ──REST───────▶│  ┌──────────┐ └───────────┘ ┌────────────┐ │
                         │  │  Field   │ ┌───────────┐ │  Admin UI  │ │
                         │  │  Filter  │ │  Access   │ │  (React)   │ │
                         │  └──────────┘ │  Control  │ └────────────┘ │
                         │               └───────────┘                │
                         └──────┬───────────────┬───────────────┬──────┘
                                │ REST          │ REST          │ REST
                                ▼               ▼               ▼
                         ┌───────────┐   ┌───────────┐   ┌───────────┐
                         │ Monolith  │   │   Auth    │   │  Future   │
                         │  :8080    │   │  :8082    │   │  Service  │
                         │   REST    │   │   REST    │   │   :XXXX   │
                         └───────────┘   └───────────┘   └───────────┘
                                │               │
                                ▼               ▼
                         ┌────────────────────────────┐
                         │    PostgreSQL 17 (:5432)    │
                         │  public.* │ auth.*          │
                         └────────────────────────────┘
```

### Потоки данных

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant GW as API Gateway
    participant M as Monolith (gRPC/REST)
    participant A as Auth (gRPC/REST)

    FE->>GW: GET /api/v1/clients?status=Active
    Note over GW: 1. Извлечь JWT → роль "manager"
    Note over GW: 2. Загрузить YAML-конфиг для Client
    Note over GW: 3. Проверить: endpoint разрешён для manager?
    Note over GW: 4. Проверить: query params (status) разрешены?
    alt upstream.protocol = grpc
        GW->>M: gRPC ClientService.ListClients(status=Active, field_mask=[...])
        M-->>GW: ListClientsResponse (protobuf)
    else upstream.protocol = rest
        GW->>M: GET /api/v1/clients?status=Active (HTTP)
        M-->>GW: JSON response (все поля)
    end
    Note over GW: 5. Отфильтровать поля по конфигу для "manager"
    Note over GW: 6. Вернуть JSON (camelCase)
    GW-->>FE: JSON (только разрешённые поля)
```

```mermaid
sequenceDiagram
    participant FE as Frontend
    participant GW as API Gateway
    participant A as Auth (gRPC/REST)

    FE->>GW: GET /api/v1/config/Client
    Note over GW: Вернуть конфиг полей для роли из JWT
    GW-->>FE: { fields: [...], actions: {...} }
    Note over FE: Построить грид, фильтры, формы по конфигу
```

---

## Сетевая карта (целевая)

| Контейнер | Внешний порт | Внутренний порт | Протокол | Назначение |
|-----------|-------------|-----------------|----------|------------|
| broker-gateway | 8080 | 8080 | HTTP/REST | Единственная внешняя точка входа API |
| broker-gateway | 8081 | 8081 | HTTP | Admin UI (конфигурация) |
| broker-api | -- (не экспонируется) | 8080 + 50051 | REST + gRPC | Бизнес-логика |
| broker-auth | -- (не экспонируется) | 8082 + 50052 | REST + gRPC | Аутентификация, пользователи |
| broker-web | 3000 | 8080 | HTTP (nginx) | Frontend SPA |
| broker-postgres | 5432 | 5432 | TCP | База данных |
| broker-n8n | 5678 | 5678 | HTTP | Workflow automation |

> **Важно:** После внедрения Gateway монолит и auth-service перестают экспонировать HTTP-порты наружу. Весь внешний REST-трафик идёт через Gateway. REST-эндпоинты на backend-сервисах сохраняются — Gateway может вызывать upstream как по gRPC, так и по REST (настраивается per-upstream в YAML). gRPC рекомендуется для новых сервисов и высоконагруженных вызовов, REST — для простоты и обратной совместимости.

---

## Структура проекта

```
gateway/
├── Broker.Gateway.sln
├── Directory.Build.props               # .NET 8, strict mode
│
├── proto/                              # Shared proto definitions (git submodule или shared folder)
│   └── broker/v1/
│       ├── clients.proto
│       ├── accounts.proto
│       ├── instruments.proto
│       ├── orders.proto
│       ├── transactions.proto
│       ├── auth.proto
│       ├── users.proto
│       ├── roles.proto
│       ├── permissions.proto
│       ├── references.proto            # Clearers, Currencies, Exchanges, TradePlatforms
│       ├── audit.proto
│       ├── dashboard.proto
│       └── common.proto                # PagedRequest, PagedResponse, FieldMask, Timestamp
│
├── src/
│   ├── Broker.Gateway/                 # Основной проект Gateway
│   │   ├── Program.cs                  # Composition root
│   │   ├── Configuration/
│   │   │   ├── GatewayConfig.cs        # Типизированная модель YAML-конфига
│   │   │   ├── EntityConfig.cs         # Конфигурация сущности
│   │   │   ├── FieldConfig.cs          # Конфигурация поля
│   │   │   ├── UpstreamConfig.cs       # Конфигурация gRPC-upstream
│   │   │   ├── AccessProfileConfig.cs  # Профили доступа (frontend, n8n, external)
│   │   │   ├── ConfigLoader.cs         # Загрузка и hot-reload YAML
│   │   │   └── ConfigValidator.cs      # Валидация конфига при загрузке
│   │   │
│   │   ├── Routing/
│   │   │   ├── DynamicRouteBuilder.cs  # Построение REST-эндпоинтов из YAML
│   │   │   └── RouteMapping.cs         # REST path → gRPC method mapping
│   │   │
│   │   ├── Proxy/
│   │   │   ├── ProxyMiddleware.cs      # Основной middleware (REST → gRPC или REST → REST)
│   │   │   ├── GrpcUpstreamClient.cs   # Вызов upstream по gRPC (protobuf)
│   │   │   ├── RestUpstreamClient.cs   # Вызов upstream по REST (JSON passthrough)
│   │   │   ├── IUpstreamClient.cs      # Абстракция upstream-клиента
│   │   │   ├── FieldFilter.cs          # Фильтрация полей по конфигу и роли
│   │   │   ├── QueryParamValidator.cs  # Валидация query params
│   │   │   └── FieldMaskBuilder.cs     # Построение gRPC FieldMask из конфига
│   │   │
│   │   ├── Auth/
│   │   │   ├── JwtMiddleware.cs        # Извлечение и валидация JWT
│   │   │   ├── AccessProfileResolver.cs # Определение профиля (frontend/n8n/external)
│   │   │   └── RoleResolver.cs         # Извлечение роли из JWT claims
│   │   │
│   │   ├── Swagger/
│   │   │   ├── DynamicSwaggerGenerator.cs   # Генерация OpenAPI spec из YAML
│   │   │   └── SwaggerFieldFilter.cs        # Фильтрация полей в Swagger по профилю
│   │   │
│   │   ├── HealthChecks/
│   │   │   ├── UpstreamHealthCheck.cs       # Проверка gRPC/REST-подключений
│   │   │   └── ConfigHealthCheck.cs         # Валидность конфига
│   │   │
│   │   ├── Middleware/
│   │   │   ├── CorrelationIdMiddleware.cs
│   │   │   ├── ExceptionHandlingMiddleware.cs
│   │   │   └── RequestLoggingMiddleware.cs
│   │   │
│   │   └── appsettings.json
│   │
├── admin/                              # Admin UI (React SPA, тот же стек что основной frontend)
│   ├── package.json
│   ├── vite.config.ts
│   ├── tsconfig.json
│   ├── index.html
│   └── src/
│       ├── api/
│       │   ├── client.ts               # Axios instance (gateway admin API)
│       │   ├── hooks.ts                # React Query hooks (entities, fields, profiles, upstreams)
│       │   └── types.ts                # TypeScript interfaces
│       ├── pages/
│       │   ├── EntitiesPage.tsx         # Список сущностей + toggle enabled
│       │   ├── EntityFieldsPage.tsx     # Настройка полей сущности (DataGrid)
│       │   ├── AccessProfilesPage.tsx   # Профили доступа
│       │   ├── UpstreamsPage.tsx        # gRPC/REST-подключения + health status
│       │   └── ConfigDiffPage.tsx       # История изменений конфига
│       ├── components/
│       │   ├── FieldEditor.tsx          # Редактор одного поля (drawer/dialog)
│       │   ├── YamlPreview.tsx          # Превью YAML с подсветкой
│       │   └── ConfigValidation.tsx     # Результаты валидации
│       └── main.tsx
│
├── config/
│   ├── gateway.yaml                    # Основной конфиг (entities, fields, access)
│   ├── menu.yaml                       # Навигационное меню (секции, пункты, permissions)
│   ├── entities.yaml                   # Конфигурация сущностей (поля, UI, валидация)
│   ├── upstreams.yaml                  # gRPC-подключения к сервисам
│   └── profiles.yaml                  # Профили доступа (frontend, n8n, external)
│
├── tests/
│   ├── Broker.Gateway.Tests.Unit/
│   │   ├── FieldFilterTests.cs
│   │   ├── ConfigLoaderTests.cs
│   │   ├── RequestTranslatorTests.cs
│   │   ├── ResponseTranslatorTests.cs
│   │   └── QueryParamValidatorTests.cs
│   └── Broker.Gateway.Tests.Integration/
│       ├── ProxyIntegrationTests.cs
│       ├── SwaggerGenerationTests.cs
│       └── AccessControlTests.cs
│
└── Dockerfile.gateway                  # Multi-stage .NET build
```

---

## YAML-конфигурация

### Структура конфига

Конфигурация разделена на три файла для удобства управления:

#### `config/upstreams.yaml` -- подключения к сервисам

```yaml
upstreams:
  monolith:
    protocol: grpc                # grpc | rest (выбор протокола для upstream)
    grpcAddress: broker-api:50051
    restAddress: http://broker-api:8080
    protos:                       # используется при protocol: grpc
      - broker.v1.ClientService
      - broker.v1.AccountService
      - broker.v1.InstrumentService
      - broker.v1.TradeOrderService
      - broker.v1.NonTradeOrderService
      - broker.v1.TradeTransactionService
      - broker.v1.NonTradeTransactionService
      - broker.v1.ClearerService
      - broker.v1.CurrencyService
      - broker.v1.ExchangeService
      - broker.v1.TradePlatformService
      - broker.v1.DashboardService
      - broker.v1.AuditService
      - broker.v1.EntityChangeService
      - broker.v1.CountryService
    healthCheck:
      enabled: true
      intervalSeconds: 10
    timeout: 30s
    retryPolicy:
      maxRetries: 3
      backoff: exponential

  auth:
    protocol: rest                # auth-service пока работает по REST
    grpcAddress: broker-auth:50052
    restAddress: http://broker-auth:8082
    protos:                       # будут использоваться после перехода на grpc
      - broker.v1.AuthService
      - broker.v1.UserService
      - broker.v1.RoleService
      - broker.v1.PermissionService
    healthCheck:
      enabled: true
      intervalSeconds: 10
    timeout: 10s
    retryPolicy:
      maxRetries: 3
      backoff: exponential
```

#### `config/profiles.yaml` -- профили доступа

```yaml
profiles:
  frontend:
    description: "Браузер оператора, роль из JWT"
    auth: jwt
    roleSource: token_claims    # роль определяется из permission claims в JWT
    rateLimit:
      requestsPerMinute: 120
    cors:
      origins: ["http://localhost:3000"]

  n8n:
    description: "Workflow automation (n8n)"
    auth: basic                 # или api_key
    credentials:
      username: "${N8N_GATEWAY_USER:-n8n}"
      password: "${N8N_GATEWAY_PASSWORD}"
    role: service               # фиксированная роль, не из JWT
    rateLimit:
      requestsPerMinute: 200
    allowedEntities:            # можно ограничить доступные сущности
      - Client
      - Account
      - User

  external:
    description: "Внешние интеграции (API-ключ)"
    auth: api_key
    headerName: X-Api-Key
    role: external
    rateLimit:
      requestsPerMinute: 60
    allowedEntities:
      - Client
      - Account
```

#### `config/gateway.yaml` -- сущности и поля

```yaml
# Глобальные настройки
settings:
  defaultPageSize: 25
  maxPageSize: 10000
  enableAuditProxy: true        # прокси аудит-эндпоинтов

# Конфигурация сущностей
entities:
  Client:
    enabled: true
    upstream: monolith
    service: broker.v1.ClientService
    basePath: /api/v1/clients

    endpoints:
      list:
        method: GET
        path: /
        rpc: ListClients
        roles: [admin, manager, viewer, operator]
      get:
        method: GET
        path: /{id}
        rpc: GetClient
        roles: [admin, manager, viewer, operator]
      create:
        method: POST
        path: /
        rpc: CreateClient
        roles: [admin, manager]
      update:
        method: PUT
        path: /{id}
        rpc: UpdateClient
        roles: [admin, manager]
      delete:
        method: DELETE
        path: /{id}
        rpc: DeleteClient
        roles: [admin]

    fields:
      id:
        protoField: id
        restName: id
        type: uuid
        ui:
          grid: false
          detail: false
          form: false
        access:
          read: ["*"]
          write: []               # id никогда не редактируется

      clientType:
        protoField: client_type
        restName: clientType
        type: enum
        enumValues: [Individual, Corporate]
        ui:
          grid: true
          detail: true
          form: true
          gridOrder: 1
          section: General
          filterType: multiSelect
        access:
          read: ["*"]
          write: [admin, manager]
        validation:
          required: true

      firstName:
        protoField: first_name
        restName: firstName
        type: string
        ui:
          grid: true
          detail: true
          form: true
          gridOrder: 2
          section: General
          filterType: text
        access:
          read: ["*"]
          write: [admin, manager]
        validation:
          required: true
          maxLength: 100

      lastName:
        protoField: last_name
        restName: lastName
        type: string
        ui:
          grid: true
          detail: true
          form: true
          gridOrder: 3
          section: General
          filterType: text
        access:
          read: ["*"]
          write: [admin, manager]
        validation:
          required: true
          maxLength: 100

      email:
        protoField: email
        restName: email
        type: string
        ui:
          grid: true
          detail: true
          form: true
          gridOrder: 5
          section: Contact
          filterType: text
        access:
          read: ["*"]
          write: [admin, manager]
        validation:
          required: true
          format: email

      phone:
        protoField: phone
        restName: phone
        type: string
        ui:
          grid: true
          detail: true
          form: true
          gridOrder: 6
          section: Contact
          filterType: text
        access:
          read: [admin, manager, operator]
          write: [admin, manager]

      taxId:
        protoField: tax_id
        restName: taxId
        type: string
        ui:
          grid: false
          detail: true
          form: true
          section: Tax & Compliance
        access:
          read: [admin, compliance]       # только admin и compliance видят
          write: [admin]
        validation:
          pattern: "^\\d{3}-\\d{2}-\\d{4}$"

      pepStatus:
        protoField: pep_status
        restName: pepStatus
        type: boolean
        ui:
          grid: true
          detail: true
          form: true
          section: Tax & Compliance
          filterType: boolean
        access:
          read: [admin, compliance, manager]
          write: [admin, compliance]

      status:
        protoField: status
        restName: status
        type: enum
        enumValues: [Active, Blocked, Closed, Pending]
        ui:
          grid: true
          detail: true
          form: true
          gridOrder: 4
          section: General
          filterType: multiSelect
        access:
          read: ["*"]
          write: [admin, manager]

      residenceCountry:
        protoField: residence_country
        restName: residenceCountry
        type: reference
        referenceEntity: Country
        ui:
          grid: true
          detail: true
          form: true
          gridOrder: 7
          section: General
          filterType: multiSelect
        access:
          read: ["*"]
          write: [admin, manager]

      addresses:
        protoField: addresses
        restName: addresses
        type: array
        ui:
          grid: false
          detail: true
          form: true
          section: Addresses
        access:
          read: [admin, manager, operator]
          write: [admin, manager]
        fields:                           # вложенные поля
          street:
            protoField: street
            restName: street
            type: string
            access:
              read: [admin, manager, operator]
              write: [admin, manager]
          city:
            protoField: city
            restName: city
            type: string
            access:
              read: [admin, manager, operator]
              write: [admin, manager]
          zipCode:
            protoField: zip_code
            restName: zipCode
            type: string
            access:
              read: [admin, manager]      # operator не видит zip
              write: [admin, manager]

      investmentProfile:
        protoField: investment_profile
        restName: investmentProfile
        type: object
        ui:
          grid: false
          detail: true
          form: true
          section: Investment Profile
        access:
          read: [admin, manager]
          write: [admin]
        fields:
          annualIncome:
            protoField: annual_income
            restName: annualIncome
            type: decimal
            access:
              read: [admin]               # только admin видит доход
              write: [admin]
          riskTolerance:
            protoField: risk_tolerance
            restName: riskTolerance
            type: enum
            enumValues: [Low, Medium, High, Aggressive]
            access:
              read: [admin, manager]
              write: [admin]

      rowVersion:
        protoField: row_version
        restName: rowVersion
        type: uint
        ui:
          grid: false
          detail: false
          form: false                     # передаётся скрыто при update
        access:
          read: ["*"]
          write: ["*"]

      createdAt:
        protoField: created_at
        restName: createdAt
        type: datetime
        ui:
          grid: true
          detail: true
          form: false
          gridOrder: 99
          filterType: dateRange
        access:
          read: ["*"]
          write: []

  Account:
    enabled: true
    upstream: monolith
    service: broker.v1.AccountService
    basePath: /api/v1/accounts
    # ... аналогично Client

  User:
    enabled: true
    upstream: auth
    service: broker.v1.UserService
    basePath: /api/v1/users
    # ... аналогично

  # Пример полностью отключённой сущности
  InvestmentProfile:
    enabled: false                        # не доступна как отдельный endpoint
```

### Правила интерпретации конфига

| Поле конфига | Значение | Поведение |
|-------------|----------|-----------|
| `enabled: false` (сущность) | Сущность отключена | Endpoint не регистрируется, не попадает в Swagger |
| `enabled: false` (поле) | Поле отключено | Вырезается из response/request, не попадает в Swagger |
| `access.read: ["*"]` | Чтение для всех | Поле присутствует в response для любой роли |
| `access.read: [admin]` | Чтение только для admin | Для остальных ролей поле вырезается из response |
| `access.write: []` | Запись запрещена | Поле вырезается из request body, игнорируется при create/update |
| `ui.grid: false` | Не показывать в таблице | Frontend не рендерит колонку |
| `ui.form: false` | Не показывать в форме | Frontend не рендерит поле в диалогах create/edit |
| `validation.required: true` | Обязательное | Gateway валидирует до отправки в upstream |
| `roles` (endpoint) | Разрешённые роли | 403 если роль не в списке |

---

## Технологический стек

### Gateway

| Компонент | Технология | Обоснование |
|-----------|-----------|-------------|
| Runtime | .NET 8, ASP.NET Core | Единый стек с backend, общие proto-файлы |
| gRPC Client | `Grpc.Net.Client` + `Google.Protobuf` | Нативная поддержка в .NET |
| YAML парсинг | `YamlDotNet` | Зрелая библиотека, типизированная десериализация |
| Hot reload | `IOptionsMonitor<T>` + `FileSystemWatcher` | Стандартный .NET паттерн |
| Swagger | `Swashbuckle` + кастомный `IDocumentFilter` | Динамическая генерация OpenAPI spec |
| Admin UI | React 18, TypeScript, Vite 5, MUI 6 | Единый стек с основным фронтендом, переиспользование компонентов |
| Health checks | `AspNetCore.Diagnostics.HealthChecks` + `Grpc.HealthCheck` | gRPC health protocol v1 + HTTP health для REST upstreams |
| Logging | Serilog | Единообразно с остальными сервисами |
| Rate limiting | `AspNetCore.RateLimiting` | Встроенный, уже используется в auth |
| JWT валидация | `Microsoft.AspNetCore.Authentication.JwtBearer` | Единообразно с остальными сервисами |
| Тесты | xUnit, FluentAssertions, NSubstitute, Testcontainers | Единообразно с остальными сервисами |

### Shared Proto

| Компонент | Технология |
|-----------|-----------|
| Proto-файлы | `proto/broker/v1/*.proto` (Protocol Buffers v3) |
| Генерация C# | `Grpc.Tools` NuGet (build-time codegen) |
| Shared project | `Broker.Proto` -- общий .csproj с proto-файлами |

### Backend-сервисы (добавления)

| Компонент | Технология |
|-----------|-----------|
| gRPC Server | `Grpc.AspNetCore` NuGet |
| gRPC Services | ASP.NET Core gRPC services (`MapGrpcService<T>()`) |
| Dual protocol | Kestrel: HTTP/1.1 (REST) + HTTP/2 (gRPC) на разных портах |

---

## Proto-файлы (контракты)

Proto-файлы -- единый источник истины для модели данных. Описывают все сущности, все поля, все RPC-методы.

### Расположение

```
proto/
└── broker/v1/
    ├── common.proto          # Shared types: PagedRequest, PagedResponse, Money, Timestamp
    ├── clients.proto         # ClientService: ListClients, GetClient, CreateClient, ...
    ├── accounts.proto        # AccountService
    ├── instruments.proto     # InstrumentService
    ├── orders.proto          # TradeOrderService, NonTradeOrderService
    ├── transactions.proto    # TradeTransactionService, NonTradeTransactionService
    ├── auth.proto            # AuthService: Login, RefreshToken, GetMe, ChangePassword
    ├── users.proto           # UserService: ListUsers, GetUser, CreateUser, ...
    ├── roles.proto           # RoleService
    ├── permissions.proto     # PermissionService
    ├── references.proto      # ClearerService, CurrencyService, ExchangeService, TradePlatformService
    ├── audit.proto           # AuditService, EntityChangeService
    ├── dashboard.proto       # DashboardService
    └── countries.proto       # CountryService
```

### Пример: `common.proto`

```protobuf
syntax = "proto3";
package broker.v1;

import "google/protobuf/timestamp.proto";
import "google/protobuf/wrappers.proto";
import "google/protobuf/field_mask.proto";

// Пагинация (запрос)
message PagedRequest {
  int32 page = 1;
  int32 page_size = 2;
  string sort = 3;              // "fieldName asc" / "fieldName desc"
  string q = 4;                 // Глобальный поиск
}

// Пагинация (ответ)
message PagedMeta {
  int32 total_count = 1;
  int32 page = 2;
  int32 page_size = 3;
  int32 total_pages = 4;
}
```

### Пример: `clients.proto`

```protobuf
syntax = "proto3";
package broker.v1;

import "broker/v1/common.proto";
import "google/protobuf/timestamp.proto";
import "google/protobuf/wrappers.proto";
import "google/protobuf/field_mask.proto";
import "google/protobuf/empty.proto";

service ClientService {
  rpc ListClients(ListClientsRequest) returns (ListClientsResponse);
  rpc GetClient(GetClientRequest) returns (Client);
  rpc CreateClient(CreateClientRequest) returns (Client);
  rpc UpdateClient(UpdateClientRequest) returns (Client);
  rpc DeleteClient(DeleteClientRequest) returns (google.protobuf.Empty);
}

// ─── Messages ───────────────────────────────────────

enum ClientType {
  CLIENT_TYPE_UNSPECIFIED = 0;
  CLIENT_TYPE_INDIVIDUAL = 1;
  CLIENT_TYPE_CORPORATE = 2;
}

enum ClientStatus {
  CLIENT_STATUS_UNSPECIFIED = 0;
  CLIENT_STATUS_ACTIVE = 1;
  CLIENT_STATUS_BLOCKED = 2;
  CLIENT_STATUS_CLOSED = 3;
  CLIENT_STATUS_PENDING = 4;
}

enum KycStatus {
  KYC_STATUS_UNSPECIFIED = 0;
  KYC_STATUS_NOT_STARTED = 1;
  KYC_STATUS_IN_PROGRESS = 2;
  KYC_STATUS_VERIFIED = 3;
  KYC_STATUS_REJECTED = 4;
  KYC_STATUS_EXPIRED = 5;
}

enum RiskLevel {
  RISK_LEVEL_UNSPECIFIED = 0;
  RISK_LEVEL_LOW = 1;
  RISK_LEVEL_MEDIUM = 2;
  RISK_LEVEL_HIGH = 3;
  RISK_LEVEL_CRITICAL = 4;
}

message Country {
  string id = 1;
  string name = 2;
  string iso2 = 3;
  string iso3 = 4;
  string flag_emoji = 5;
}

message Address {
  string type = 1;
  string street = 2;
  string city = 3;
  string state = 4;
  string zip_code = 5;
  string country_id = 6;
  Country country = 7;
}

message InvestmentProfile {
  string experience_level = 1;
  string investment_objectives = 2;
  string risk_tolerance = 3;
  google.protobuf.DoubleValue annual_income = 4;
  google.protobuf.DoubleValue net_worth = 5;
  google.protobuf.DoubleValue liquid_net_worth = 6;
  string source_of_funds = 7;
}

message Client {
  string id = 1;
  ClientType client_type = 2;
  string first_name = 3;
  string last_name = 4;
  string company_name = 5;
  string email = 6;
  string phone = 7;
  string tax_id = 8;
  ClientStatus status = 9;
  KycStatus kyc_status = 10;
  bool pep_status = 11;
  RiskLevel risk_level = 12;
  string external_id = 13;
  Country residence_country = 14;
  Country citizenship_country = 15;
  repeated Address addresses = 16;
  InvestmentProfile investment_profile = 17;
  uint32 row_version = 18;
  google.protobuf.Timestamp created_at = 19;
  string created_by = 20;
  google.protobuf.Timestamp updated_at = 21;
  string updated_by = 22;
}

// ─── Requests / Responses ────────────────────────────

message ListClientsRequest {
  // Pagination
  int32 page = 1;
  int32 page_size = 2;
  string sort = 3;
  string q = 4;

  // Filters
  repeated ClientStatus status = 5;
  repeated ClientType client_type = 6;
  repeated KycStatus kyc_status = 7;
  repeated RiskLevel risk_level = 8;
  google.protobuf.BoolValue pep_status = 9;
  repeated string residence_country_ids = 10;
  repeated string citizenship_country_ids = 11;
  string name = 12;
  string email = 13;
  string phone = 14;
  string external_id = 15;
  google.protobuf.Timestamp created_from = 16;
  google.protobuf.Timestamp created_to = 17;

  // FieldMask — какие поля вернуть (пустой = все)
  google.protobuf.FieldMask field_mask = 20;
}

message ListClientsResponse {
  repeated Client items = 1;
  PagedMeta meta = 2;
}

message GetClientRequest {
  string id = 1;
  google.protobuf.FieldMask field_mask = 2;
}

message CreateClientRequest {
  ClientType client_type = 1;
  string first_name = 2;
  string last_name = 3;
  string company_name = 4;
  string email = 5;
  string phone = 6;
  string tax_id = 7;
  ClientStatus status = 8;
  KycStatus kyc_status = 9;
  bool pep_status = 10;
  RiskLevel risk_level = 11;
  string external_id = 12;
  string residence_country_id = 13;
  string citizenship_country_id = 14;
  repeated Address addresses = 15;
  InvestmentProfile investment_profile = 16;
}

message UpdateClientRequest {
  string id = 1;
  ClientType client_type = 2;
  string first_name = 3;
  string last_name = 4;
  string company_name = 5;
  string email = 6;
  string phone = 7;
  string tax_id = 8;
  ClientStatus status = 9;
  KycStatus kyc_status = 10;
  bool pep_status = 11;
  RiskLevel risk_level = 12;
  string external_id = 13;
  string residence_country_id = 14;
  string citizenship_country_id = 15;
  repeated Address addresses = 16;
  InvestmentProfile investment_profile = 17;
  uint32 row_version = 18;
}

message DeleteClientRequest {
  string id = 1;
}
```

---

## Ключевые компоненты Gateway

### 1. ConfigLoader — загрузка и hot-reload конфига

```
Startup:
  1. Загрузить gateway.yaml, upstreams.yaml, profiles.yaml
  2. Десериализовать в типизированные модели (GatewayConfig)
  3. Валидировать (ConfigValidator): все referenced entities/fields существуют,
     proto-fields корректны, нет циклических ссылок
  4. Зарегистрировать как IOptionsMonitor<GatewayConfig>

Hot-reload:
  - FileSystemWatcher отслеживает изменения YAML-файлов
  - При изменении: перезагрузить → валидировать → если ОК → применить
  - Если валидация провалилась — сохранить старый конфиг, залогировать ошибку
  - Endpoint: POST /admin/config/reload (ручной reload)
```

### 2. DynamicRouteBuilder — регистрация REST-эндпоинтов

```
При старте (и при hot-reload конфига):
  1. Для каждой enabled entity в конфиге:
     - Зарегистрировать REST-эндпоинты из entity.endpoints
     - Каждый endpoint → middleware pipeline:
       JwtAuth → AccessControl → QueryValidation → GrpcProxy → FieldFilter → Response
  2. Зарегистрировать metadata-эндпоинты:
     - GET /api/v1/config/{entityType} — конфиг полей для роли из JWT
     - GET /api/v1/config — список доступных сущностей
```

### 3. ProxyMiddleware — вызов upstream (gRPC или REST)

```
Incoming REST request:
  1. Resolve entity config по URL path
  2. Resolve upstream config (protocol: grpc | rest)
  3. Выбрать IUpstreamClient:

  ── Если upstream.protocol == grpc ──
     a. Создать gRPC channel (с connection pooling)
     b. Map REST → gRPC:
        - Route params ({id}) → proto message fields
        - Query params (?status=Active) → proto filter fields
        - Request body (JSON) → proto message (via JsonParser)
        - Построить FieldMask из конфига
     c. Вызвать gRPC method
     d. Map gRPC → REST:
        - Proto message → JSON (via JsonFormatter, camelCase)
        - gRPC status codes → HTTP status codes

  ── Если upstream.protocol == rest ──
     a. Построить HTTP request к upstream (HttpClient, connection pooling)
     b. Проксировать: route params, query params, request body (JSON passthrough)
     c. Получить JSON response от upstream
     d. HTTP status codes передаются as-is

  4. Apply FieldFilter (удалить поля, недоступные для роли)
  5. Вернуть JSON response
```

### 4. FieldFilter — фильтрация полей

```
Вход: JSON object + role + entity config
Выход: JSON object с удалёнными полями

Алгоритм:
  1. Для каждого поля в JSON:
     a. Найти field config
     b. Если field config не найден — удалить (неизвестное поле)
     c. Если field.enabled == false — удалить
     d. Если role не в field.access.read — удалить
     e. Если поле type == object или array — рекурсивно фильтровать вложенные
  2. Для write (request body):
     a. Аналогично, но проверять field.access.write
     b. Удалить read-only поля
```

### 5. Swagger Generator

```
При старте (и при hot-reload):
  1. Для каждой enabled entity:
     a. Создать schema из field configs (type, required, enum values)
     b. Создать path items из endpoint configs
     c. Фильтровать по текущему профилю (frontend видит одно, n8n другое)
  2. Собрать OpenAPI 3.0 spec
  3. Сервировать на GET /swagger/v1/swagger.json

Swagger endpoint принимает query param ?profile=frontend|n8n|external
для генерации spec под конкретного потребителя.
```

### 6. Config Metadata API

Frontend запрашивает конфиг перед рендерингом:

```
GET /api/v1/config/Client
Authorization: Bearer <jwt>

Response (для роли "manager"):
{
  "entityType": "Client",
  "basePath": "/api/v1/clients",
  "actions": {
    "create": true,
    "update": true,
    "delete": false,
    "export": true
  },
  "fields": [
    {
      "code": "firstName",
      "type": "string",
      "label": "First Name",
      "ui": { "grid": true, "detail": true, "form": true, "gridOrder": 2, "section": "General", "filterType": "text" },
      "writable": true,
      "validation": { "required": true, "maxLength": 100 }
    },
    {
      "code": "taxId",
      "type": "string",
      "label": "Tax ID",
      "ui": { "grid": false, "detail": false, "form": false },
      "writable": false,
      "validation": null
    }
    // taxId.detail = false для manager, т.к. access.read не включает manager
    // ...
  ],
  "sections": ["General", "Contact", "Tax & Compliance", "Addresses", "Investment Profile"]
}
```

Frontend использует этот ответ для:
- Построения колонок DataGrid (`ui.grid == true`)
- Построения фильтров (`ui.filterType`)
- Построения форм create/edit (`ui.form == true`, `writable == true`)
- Построения detail page (`ui.detail == true`, секционирование по `section`)
- Показа/скрытия кнопок действий (`actions.*`)

---

## Маппинг gRPC status → HTTP status

| gRPC Status | HTTP Status | Описание |
|-------------|-------------|----------|
| OK | 200 / 201 / 204 | Успех (201 для Create, 204 для Delete) |
| NOT_FOUND | 404 | Сущность не найдена |
| INVALID_ARGUMENT | 400 | Ошибка валидации |
| ALREADY_EXISTS | 409 | Дубликат (уникальность) |
| FAILED_PRECONDITION | 409 | Бизнес-правило / concurrency conflict |
| PERMISSION_DENIED | 403 | Нет прав |
| UNAUTHENTICATED | 401 | Не авторизован |
| RESOURCE_EXHAUSTED | 429 | Rate limit |
| INTERNAL | 500 | Внутренняя ошибка |
| UNAVAILABLE | 503 | Сервис недоступен |

Gateway преобразует gRPC-ошибки в RFC 7807 ProblemDetails (единообразно с текущим API).

---

## Взаимодействие с Frontend

### Текущее состояние

```
Frontend → nginx → auth-service (REST)
Frontend → nginx → monolith (REST)
```

### Целевое состояние

```
Frontend → nginx → Gateway (REST) → auth-service (gRPC или REST)
Frontend → nginx → Gateway (REST) → monolith (gRPC или REST)
Frontend → Gateway: GET /api/v1/config/{entity} — metadata для рендеринга
```

### Изменения во Frontend

1. **Base URL** -- не меняется (`/api/v1`), nginx перенаправляет на Gateway вместо backend-сервисов
2. **Новый API-хук** -- `useEntityConfig(entityType)` — загрузка метаданных полей
3. **Динамический DataGrid** -- колонки, фильтры, сортировка из конфига, а не из hardcoded `columns[]`
4. **Динамические формы** -- поля create/edit диалогов из конфига
5. **Динамический detail page** -- секции и поля из конфига
6. **Action visibility** -- кнопки create/edit/delete из `actions` конфига (дополнение к permission-based gating)

> **Обратная совместимость:** Фронтенд может работать и без Gateway — если `/api/v1/config/*` вернул 404, используются hardcoded columns/fields (fallback). Это обеспечивает плавную миграцию.

---

## Взаимодействие с n8n

### Текущее состояние

```
n8n → monolith (REST, http://api:8080/api/v1)
n8n → auth-service (REST, http://auth:8082/api/v1)
```

### Целевое состояние

```
n8n → Gateway (REST, http://gateway:8080/api/v1)
```

n8n получает:
- Единый base URL вместо двух
- Swagger, отфильтрованный под профиль `n8n` (только разрешённые сущности и поля)
- Rate limiting, отдельный от frontend
- Basic auth или API key (не JWT)

---

## Docker Compose (целевой)

```yaml
services:
  postgres:
    image: postgres:17-alpine
    # ... без изменений

  auth:
    build:
      context: .
      dockerfile: Dockerfile.auth
    container_name: broker-auth
    # Не экспонирует порты наружу — доступен только через Gateway
    expose:
      - "8082"          # REST
      - "50052"         # gRPC (когда включён)
    environment:
      ASPNETCORE_URLS: "http://+:8082;http://+:50052"
      Kestrel__Endpoints__Rest__Url: "http://+:8082"
      Kestrel__Endpoints__Rest__Protocols: Http1
      Kestrel__Endpoints__Grpc__Url: "http://+:50052"
      Kestrel__Endpoints__Grpc__Protocols: Http2
      # ... остальные env vars
    depends_on:
      postgres:
        condition: service_healthy

  api:
    build:
      context: .
      dockerfile: Dockerfile.api
    container_name: broker-api
    # Не экспонирует порты наружу — доступен только через Gateway
    expose:
      - "8080"          # REST
      - "50051"         # gRPC (когда включён)
    environment:
      ASPNETCORE_URLS: "http://+:8080;http://+:50051"
      Kestrel__Endpoints__Rest__Url: "http://+:8080"
      Kestrel__Endpoints__Rest__Protocols: Http1
      Kestrel__Endpoints__Grpc__Url: "http://+:50051"
      Kestrel__Endpoints__Grpc__Protocols: Http2
      # ... остальные env vars
    depends_on:
      postgres:
        condition: service_healthy
      auth:
        condition: service_healthy

  gateway:
    build:
      context: .
      dockerfile: Dockerfile.gateway
    container_name: broker-gateway
    restart: unless-stopped
    ports:
      - "8080:8080"     # REST API (единственная внешняя точка API)
      - "8081:8081"     # Admin UI
    environment:
      ASPNETCORE_ENVIRONMENT: "${ASPNETCORE_ENVIRONMENT:-Production}"
      Jwt__Secret: "${JWT_SECRET}"
      Gateway__ConfigPath: "/etc/gateway/config"
    volumes:
      - ./config:/etc/gateway/config:ro
    depends_on:
      api:
        condition: service_healthy
      auth:
        condition: service_healthy
    healthcheck:
      test: ["CMD-SHELL", "wget -qO /dev/null http://127.0.0.1:8080/health/live || exit 1"]
      interval: 10s
      timeout: 5s
      retries: 5
      start_period: 10s
    deploy:
      resources:
        limits:
          memory: 256M

  web:
    build:
      context: .
      dockerfile: Dockerfile.web
    container_name: broker-web
    ports:
      - "3000:8080"
    depends_on:
      gateway:
        condition: service_healthy
    # nginx.conf обновлён: /api/* → gateway:8080

  n8n:
    environment:
      BROKER_API_URL: "http://gateway:8080/api/v1"    # один URL вместо двух
    depends_on:
      gateway:
        condition: service_healthy
```

---

## Реализованные CRUD-эндпоинты конфигурации

Gateway уже предоставляет набор REST-эндпоинтов для управления YAML-конфигурацией через Admin UI. Все эндпоинты требуют разрешение `settings.manage`.

### Эндпоинты

| Метод | Путь | Описание |
|-------|------|----------|
| GET | `/api/v1/config/menu/raw` | Полное меню без фильтрации по ролям (для редактора) |
| PUT | `/api/v1/config/menu` | Сохранить меню в YAML-файл |
| GET | `/api/v1/config/entities/raw` | Все сущности без фильтрации по ролям |
| PUT | `/api/v1/config/entities` | Сохранить сущности в YAML-файл |
| GET | `/api/v1/config/upstreams` | Все upstreams |
| PUT | `/api/v1/config/upstreams` | Сохранить upstreams в YAML-файл |

`/raw`-эндпоинты возвращают полные данные без учёта роли текущего пользователя -- предназначены для UI-редактора конфигурации. PUT-эндпоинты принимают JSON, сериализуют в YAML и записывают на диск.

### Сериализация в ConfigLoader

Для записи конфигурации обратно в YAML-файлы в `ConfigLoader` добавлен `ISerializer` из YamlDotNet (`SerializerBuilder` с `CamelCaseNamingConvention`):

- `SaveMenu()` -- сериализует структуру меню и записывает в `config/menu.yaml`
- `SaveEntities()` -- сериализует конфигурацию сущностей и записывает в `config/entities.yaml`
- `SaveUpstreams()` -- сериализует upstreams и записывает в `config/upstreams.yaml`
- Общий generic-метод `SaveFile<T>()` инкапсулирует сериализацию в YAML и запись на диск

### Секция Configuration в menu.yaml

В `menu.yaml` добавлена секция для навигации к страницам управления конфигурацией:

```yaml
- id: config
  label: Configuration
  icon: AdminPanelSettings
  permissions: [settings.manage]
  children:
    - id: config-menu
      label: Menu Editor
      icon: Menu
      path: /config/menu
    - id: config-entities
      label: Entity Fields
      icon: ViewColumn
      path: /config/entities
    - id: config-upstreams
      label: Upstreams
      icon: Cloud
      path: /config/upstreams
```

Секция доступна только пользователям с разрешением `settings.manage`. Каждый дочерний пункт ведёт на соответствующую страницу Admin UI для редактирования YAML-конфигурации.

---

## План реализации

### Phase 1: Proto-файлы и shared project

**Цель:** Определить gRPC-контракты для всех сущностей.

**Задачи:**
1. Создать директорию `proto/broker/v1/` в корне репозитория
2. Написать proto-файлы для всех сущностей (clients, accounts, instruments, orders, transactions, auth, users, roles, permissions, references, audit, dashboard, countries)
3. Создать shared проект `Broker.Proto/Broker.Proto.csproj` с `Grpc.Tools` для кодогенерации
4. Подключить `Broker.Proto` как зависимость к backend и auth-service
5. Убедиться что proto messages покрывают все поля текущих DTO

**Результат:** Скомпилированные C# классы из proto, доступные всем проектам.

---

### Phase 2: gRPC-сервисы в Monolith и Auth

**Цель:** Backend-сервисы начинают обслуживать gRPC-запросы параллельно с REST.

**Задачи:**
1. Добавить `Grpc.AspNetCore` NuGet в оба сервиса
2. Настроить Kestrel dual-protocol (HTTP/1.1 + HTTP/2 на разных портах)
3. Реализовать gRPC service implementations:
   - `ClientGrpcService : ClientService.ClientServiceBase` — делегирует в MediatR handlers
   - Аналогично для всех остальных сервисов
4. Зарегистрировать gRPC-сервисы: `app.MapGrpcService<ClientGrpcService>()`
5. Добавить gRPC health check service (`Grpc.HealthCheck`)
6. Обновить docker-compose: expose gRPC-порты
7. Интеграционные тесты для gRPC-эндпоинтов

**Паттерн gRPC service:**
```csharp
public sealed class ClientGrpcService(ISender mediator) : ClientService.ClientServiceBase
{
    public override async Task<ListClientsResponse> ListClients(
        ListClientsRequest request, ServerCallContext context)
    {
        var query = MapToQuery(request);       // proto → MediatR query
        var result = await mediator.Send(query, context.CancellationToken);
        return MapToResponse(result);           // PagedResult<DTO> → proto response
    }
}
```

> gRPC-сервисы — тонкие адаптеры. Вся бизнес-логика остаётся в MediatR handlers.

**Результат:** Монолит и Auth обслуживают REST + gRPC одновременно. Внешний API не затронут.

---

### Phase 3: Gateway MVP (без field filtering)

**Цель:** Gateway принимает REST, вызывает upstream (gRPC или REST), возвращает JSON. Без фильтрации полей.

**Задачи:**
1. Создать проект `gateway/Broker.Gateway/`
2. Реализовать `ConfigLoader` — чтение YAML-файлов
3. Реализовать `DynamicRouteBuilder` — регистрация REST-эндпоинтов из конфига
4. Реализовать `ProxyMiddleware` с двумя upstream-клиентами:
   - `GrpcUpstreamClient` — для `protocol: grpc` (REST→gRPC translation)
   - `RestUpstreamClient` — для `protocol: rest` (JSON passthrough + field filtering)
5. JWT-валидация (копия текущей конфигурации из backend)
6. Маппинг gRPC status / HTTP status → ProblemDetails
7. Health checks (gRPC + HTTP upstream connectivity)
9. Swagger generation из YAML-конфига
10. Обновить docker-compose: добавить gateway service
11. Обновить nginx: `/api/*` → gateway вместо backend-сервисов
12. Переключить n8n на gateway URL
13. Интеграционные тесты: REST→Gateway→gRPC→Backend roundtrip

**Результат:** Все REST-запросы идут через Gateway. Backend-сервисы не экспонируют порты наружу.

---

### Phase 4: Field Filtering + Access Control

**Цель:** Gateway фильтрует поля в запросах и ответах на основании YAML-конфига и роли.

**Задачи:**
1. Реализовать `FieldFilter` — фильтрация JSON-полей по роли
2. Реализовать `FieldMaskBuilder` — построение gRPC FieldMask из конфига
3. Реализовать `QueryParamValidator` — проверка query params против конфига
4. Реализовать endpoint-level access control (roles в endpoint config)
5. Реализовать Config Metadata API (`GET /api/v1/config/{entity}`)
6. Реализовать access profiles (frontend, n8n, external)
7. Реализовать валидацию request body по конфигу (required, pattern, maxLength)
8. Hot-reload конфига без рестарта (`FileSystemWatcher` + `IOptionsMonitor`)
9. Написать YAML-конфиг для всех текущих сущностей
10. Unit-тесты на FieldFilter, QueryParamValidator
11. Integration-тесты: проверить field-level filtering для разных ролей

**Результат:** Поля фильтруются по ролям. Frontend может запрашивать метаданные.

---

### Phase 5: Admin UI

**Цель:** Веб-интерфейс для управления YAML-конфигурацией.

**Стек:** React 18, TypeScript, Vite 5, MUI 6, React Query — единый стек с основным фронтендом.

**Задачи:**
1. Создать React SPA проект `gateway/admin/` (Vite + TypeScript + MUI)
2. Gateway сервирует Admin SPA на порте 8081 (статика через Kestrel `UseStaticFiles`)
3. Admin API endpoints в Gateway (`/admin/api/`):
   - `GET /admin/api/entities` — список сущностей с конфигом
   - `PUT /admin/api/entities/{name}` — обновить конфиг сущности
   - `GET /admin/api/upstreams` — список upstreams + health status
   - `GET /admin/api/profiles` — профили доступа
   - `PUT /admin/api/profiles/{name}` — обновить профиль
   - `POST /admin/api/config/reload` — применить изменения (hot-reload)
   - `GET /admin/api/config/diff` — diff текущего vs сохранённого конфига
4. Страницы:
   - Список сущностей (toggle enabled, overview)
   - Настройка полей сущности (DataGrid с toggle visibility, access roles, validation)
   - Настройка endpoints (toggle, roles)
   - Профили доступа (CRUD)
   - Upstreams (статус gRPC/REST подключений, health)
   - Diff/история изменений конфига
5. YAML-превью с подсветкой синтаксиса (Monaco Editor или react-simple-code-editor)
6. Валидация конфига перед сохранением
7. Кнопка "Apply" — сохранить YAML + trigger hot-reload
8. Аутентификация: Basic Auth (admin-only)
9. Dockerfile.gateway включает multi-stage build admin SPA

**Результат:** Конфигурация управляется через UI без редактирования YAML вручную.

---

### Phase 6: Frontend — динамический рендеринг

**Цель:** Frontend строит UI из metadata конфига вместо hardcoded columns/fields.

**Задачи:**
1. Новый хук `useEntityConfig(entityType)` — загрузка и кэширование метаданных
2. Компонент `DynamicDataGrid` — строит колонки и фильтры из конфига
3. Компонент `DynamicForm` — строит поля формы из конфига (create/edit dialogs)
4. Компонент `DynamicDetail` — строит секции detail page из конфига
5. Fallback на hardcoded layout если `/config/{entity}` недоступен
6. Обновить все страницы для использования динамического рендеринга

**Результат:** UI полностью управляется конфигурацией. Изменение YAML → изменение UI.

---

### Phase 7: Стабилизация и оптимизация

**Цель:** Перевести высоконагруженные upstreams на gRPC, оптимизировать Gateway.

**Задачи:**
1. Перевести monolith upstream на `protocol: grpc` в YAML (list queries — основная нагрузка)
2. Профилирование: замерить latency REST vs gRPC для типичных запросов
3. Connection pooling и retry policy для gRPC-каналов
4. Кэширование конфига и метаданных в памяти Gateway
5. Distributed tracing (OpenTelemetry) сквозь Gateway → upstream
6. Нагрузочное тестирование Gateway (k6 / bombardier)
7. Документация: обновить CLAUDE.md, архитектурные диаграммы

> **Примечание:** REST-эндпоинты на backend-сервисах сохраняются. Gateway поддерживает оба протокола — выбор per-upstream в YAML. Это позволяет новым сервисам стартовать с REST и переходить на gRPC по мере необходимости.

**Результат:** Gateway стабилен под нагрузкой. Критичные пути используют gRPC, остальные — REST.

---

## Диаграмма зависимостей проектов (целевая)

```
┌──────────────────────────────────────────────────────────────┐
│                         proto/                                │
│  Broker.Proto (.csproj)                                      │
│  Содержит: *.proto файлы + Grpc.Tools codegen                │
└──────────┬──────────────────┬──────────────────┬─────────────┘
           │                  │                  │
           ▼                  ▼                  ▼
┌──────────────────┐ ┌──────────────────┐ ┌──────────────────┐
│  gateway/        │ │  backend/        │ │  auth-service/   │
│  Broker.Gateway  │ │  Infrastructure  │ │  Infrastructure  │
│                  │ │  (REST + gRPC)   │ │  (REST + gRPC)   │
│  References:     │ │                  │ │                  │
│  - Broker.Proto  │ │  References:     │ │  References:     │
│  - YamlDotNet    │ │  - Broker.Proto  │ │  - Broker.Proto  │
│  - Grpc.Net.Client│ │  - Grpc.AspNetCore│ │  - Grpc.AspNetCore│
└──────────────────┘ └──────────────────┘ └──────────────────┘

┌──────────────────┐
│  gateway/admin/  │   React 18, TypeScript, Vite 5, MUI 6
│  (React SPA)     │   Сервируется Gateway на :8081
└──────────────────┘
```

---

## Риски и митигация

| Риск | Вероятность | Влияние | Митигация |
|------|------------|---------|-----------|
| Gateway как single point of failure | Высокая | Критичное | Health checks + restart policy + горизонтальное масштабирование (несколько реплик) |
| Задержка из-за дополнительного hop | Средняя | Среднее | gRPC binary protocol компенсирует; connection pooling; кэширование конфига в памяти |
| Сложность отладки (запрос проходит через 3 сервиса) | Средняя | Среднее | Correlation ID сквозной; structured logging; distributed tracing (OpenTelemetry) |
| Рассинхронизация proto и YAML-конфига | Средняя | Среднее | ConfigValidator проверяет proto-поля при загрузке; CI-проверка |
| YAML-конфиг становится слишком большим | Низкая | Низкое | Разбиение на файлы по сущностям; Admin UI абстрагирует сложность |
| Breaking changes в proto | Средняя | Высокое | Proto style guide: поля никогда не удалять, только deprecate; CI-линтер `buf lint` |

---

## Метрики успеха

| Метрика | Текущее | Целевое |
|---------|---------|---------|
| Внешних точек входа API | 2 (auth:8082, api:5050) | 1 (gateway:8080) |
| Время добавления нового сервиса | Правка nginx + docker-compose | Добавить upstream в YAML |
| Время скрытия/показа поля | Deploy (код + build + restart) | Правка YAML (hot-reload, ~1 сек) |
| Межсервисный протокол | HTTP/REST (text JSON) | gRPC или REST (настраивается per-upstream в YAML) |
| Swagger для n8n | Ручная настройка двух URL | Авто-генерация из конфига |
| Field-level access control | Нет | YAML-конфигурируемый по ролям |
