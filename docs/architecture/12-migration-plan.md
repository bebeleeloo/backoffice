# 12. План перехода к целевой архитектуре

## Текущее состояние

```
Frontend (3 SPA) ─── nginx ───── API Gateway (:8090) ───┬── Core API (:8080) ──── PostgreSQL (:5432)
     :3000                                              └── Auth Service (:8082) ──┘      public.* / auth.*
                                                             n8n (:5678) ── n8n-db
```

- 2 бэкенд-сервиса (core + auth)
- 3 SPA-фронтенда (backoffice, auth, config) в pnpm monorepo + Turborepo
- API Gateway реализован (REST proxy, config CRUD, menu API)
- Динамический sidebar из Config Service (встроен в Gateway)
- `@broker/ui-kit` и `@broker/auth-module` -- shared-пакеты
- NavigationProvider для кросс-SPA навигации
- Нет Report Service
- Нет gRPC

## Целевое состояние

```
Frontend Apps                API Gateway         Backend Services
┌─────────────┐             ┌───────────┐       ┌─────────────┐
│ backoffice  │──REST──────▶│  Gateway  │──┬───▶│  Core   │ gRPC/REST
│ (all modules)│            │  :8090    │  │    │  :8080/:50051│
├─────────────┤             │           │  ├───▶│  Auth        │ gRPC/REST
│ other-project│──REST─────▶│  Config   │  │    │  :8082/:50052│
│ (auth+own)  │             │  YAML     │  ├───▶│  Reports     │ gRPC/REST
└─────────────┘             │  Swagger  │  │    │  :8084/:50053│
                            └───────────┘  │    └─────────────┘
n8n ──REST─────────────────────────────────┘         │
                                                     ▼
                                              PostgreSQL :5432
                                              public.* / auth.* / reports.*
```

---

## Фазы

```
Фаза 1 ─── Фаза 2 ─── Фаза 3 ─── Фаза 4 ─── Фаза 5 ─── Фаза 6 ─── Фаза 7 ─── Фаза 8 ─── Фаза 9
Monorepo    ui-kit     auth-       Config     Dynamic    API        Report     gRPC       Gateway
фронта     выделение   module     Service    sidebar    Gateway    Service    интеграция Admin UI
  ✅         ✅         ✅         ✅          ✅         ✅
```

**Фазы 1-6 выполнены.** Фазы 1-3 были выполнены параллельно с фазами 4-5 (фронт и бэкенд-сервисы независимы). Фаза 7 зависит от выбора технологии отчётов. Фаза 8 добавляет gRPC к уже работающим REST-сервисам. Фаза 9 частично реализована через apps/config.

---

## Фаза 1: Инициализация monorepo фронтенда

**Цель:** Перевести `frontend/` на pnpm workspaces + Turborepo без изменения функциональности.

**Длительность:** 2-3 дня

### Шаги

1. **Инициализация pnpm workspace**
   - Установить pnpm глобально, создать `pnpm-workspace.yaml`
   - Создать `turbo.json` с задачами `build`, `dev`, `test`, `lint`
   - Создать `tsconfig.base.json` с общими настройками TypeScript
   - Корневой `package.json` с workspace scripts
   - Корневой `eslint.config.js` (shared rules)

2. **Перенос текущего фронтенда в `apps/backoffice/`**
   - `frontend/src/` → `frontend/apps/backoffice/src/`
   - `frontend/index.html` → `frontend/apps/backoffice/index.html`
   - `frontend/vite.config.ts` → `frontend/apps/backoffice/vite.config.ts`
   - `frontend/vitest.config.ts` → `frontend/apps/backoffice/vitest.config.ts`
   - `frontend/public/` → `frontend/apps/backoffice/public/`
   - Обновить пути в конфигах

3. **Создать пустой `packages/ui-kit/`**
   - `package.json` с `name: "@broker/ui-kit"`
   - Пока пустой -- реэкспортирует ничего
   - `apps/backoffice` добавляет зависимость `"@broker/ui-kit": "workspace:*"`

4. **Проверка**
   - `pnpm install`
   - `pnpm turbo build` -- backoffice собирается
   - `pnpm turbo test` -- все 109 тестов проходят
   - `pnpm turbo dev` -- dev-сервер работает
   - Обновить `Dockerfile.web` для новой структуры
   - `docker compose up --build web` -- контейнер работает

### Результат

```
frontend/
├── packages/
│   └── ui-kit/              # пустой каркас
├── apps/
│   └── backoffice/          # весь текущий код
├── pnpm-workspace.yaml
├── turbo.json
└── package.json
```

Функциональность не изменилась. Инфраструктура monorepo готова.

### Риски

| Риск | Митигация |
|------|-----------|
| Поломка путей при переносе | Прогнать все тесты + ручной smoke test |
| Vite не резолвит workspace-пакеты | Настроить `resolve.alias` или `vite-tsconfig-paths` |
| CI сломается | Обновить CI workflow paths + cache для pnpm |

---

## Фаза 2: Выделение @broker/ui-kit

**Цель:** Вынести общие компоненты, тему, хуки, утилиты в shared-пакет.

**Длительность:** 3-5 дней

**Зависимости:** Фаза 1

### Шаги

1. **Перенос темы**
   - `apps/backoffice/src/theme/` → `packages/ui-kit/src/theme/`
   - Экспорт: `createAppTheme`, `createAppListTheme`, `AppThemeProvider`, `useThemeMode`, `useListTheme`, `SIDEBAR_COLORS`, `STAT_GRADIENTS`

2. **Перенос компонентов**
   - `apps/backoffice/src/components/` → `packages/ui-kit/src/components/`
   - Все 14 компонентов: `PageContainer`, `FilteredDataGrid`, `ConfirmDialog`, `UserAvatar`, `Breadcrumbs`, `DetailField`, `ExportButton`, `GlobalSearchBar`, `ErrorBoundary`, `RouteLoadingFallback`, `EntityHistoryDialog`, `AuditDetailDialog`, `GridFilterRow`, `ChangeHistoryComponents`

3. **Перенос хуков и утилит**
   - `apps/backoffice/src/hooks/` → `packages/ui-kit/src/hooks/`
   - `apps/backoffice/src/utils/` → `packages/ui-kit/src/utils/`

4. **Перенос auth**
   - `apps/backoffice/src/auth/` → `packages/ui-kit/src/auth/`
   - `AuthProvider`, `useAuth`, `useHasPermission`, `RequireAuth`

5. **Перенос API client**
   - `apps/backoffice/src/api/client.ts` → `packages/ui-kit/src/api/client.ts`
   - Axios instance, interceptors, token refresh

6. **Перенос layout**
   - `apps/backoffice/src/layouts/MainLayout.tsx` → `packages/ui-kit/src/layouts/`

7. **Обновление импортов в backoffice**
   - Заменить все `../components/...`, `../theme/...`, `../hooks/...` на `@broker/ui-kit`
   - IDE Find & Replace или codemod

8. **Главный реэкспорт**
   - `packages/ui-kit/src/index.ts` -- экспортирует всё

9. **Тесты**
   - Перенести тесты компонентов в `packages/ui-kit/`
   - Обновить `renderWithProviders` для ui-kit контекста
   - Все тесты должны пройти

### Результат

```
packages/ui-kit/src/
├── components/      # 14 общих компонентов
├── layouts/         # MainLayout
├── theme/           # createAppTheme, SIDEBAR_COLORS
├── auth/            # AuthProvider, useAuth, RequireAuth
├── api/client.ts    # Axios instance
├── hooks/           # useDebounce, useConfirm
├── utils/           # exportToExcel, validateFields
└── index.ts

apps/backoffice/src/
├── pages/           # Все бизнес-страницы (импорты из @broker/ui-kit)
├── api/
│   ├── types.ts     # DTO типы
│   └── hooks.ts     # React Query хуки
└── main.tsx
```

### Критерий готовности
- `pnpm turbo build` -- оба пакета собираются
- Все тесты проходят
- Docker-образ собирается и работает
- Визуально ничего не изменилось

---

## Фаза 3: Выделение @broker/auth-module

**Цель:** Auth-страницы в отдельный переиспользуемый пакет.

**Длительность:** 2-3 дня

**Зависимости:** Фаза 2

### Шаги

1. **Создать `packages/auth-module/`**
   ```
   packages/auth-module/
   ├── src/
   │   ├── pages/
   │   ├── api/
   │   ├── routes.ts
   │   └── index.ts
   └── package.json    # depends on @broker/ui-kit
   ```

2. **Перенести auth-страницы из backoffice**
   - `LoginPage.tsx`
   - `UsersPage.tsx`, `UserDetailsPage.tsx`, `UserDialogs.tsx`
   - `RolesPage.tsx`, `RoleDetailsPage.tsx`, `RoleDialogs.tsx`
   - `settings/ProfileTab.tsx`

3. **Вырезать auth API из backoffice**
   - Из `apps/backoffice/src/api/types.ts` вырезать: `UserDto`, `RoleDto`, `PermissionDto`, `AuthResponse`, `UserProfile` и связанные
   - Из `apps/backoffice/src/api/hooks.ts` вырезать: `useUsers`, `useUser`, `useCreateUser`, `useUpdateUser`, `useDeleteUser`, `useRoles`, `useRole`, `useCreateRole`, `useUpdateRole`, `useDeleteRole`, `usePermissions`, `useLogin`, `useRefreshToken`, `useMe`, `useChangePassword`, `useUpdateProfile`, `useUploadPhoto`, `useDeletePhoto`
   - Перенести в `packages/auth-module/src/api/`

4. **Экспортировать `authModule: ModuleDefinition`**
   ```typescript
   export const authModule: ModuleDefinition = {
     name: 'auth',
     routes: authRoutes,
     loginPage: LoginPage,
   }
   ```

5. **Подключить в backoffice**
   ```typescript
   import { authModule } from '@broker/auth-module'
   createApp({ modules: [backofficeModule, authModule] })
   ```

6. **Реализовать `createApp()` в ui-kit**
   - Фабричная функция: собирает роуты из модулей, оборачивает провайдерами, создаёт роутер

7. **Перенести auth-тесты** в `packages/auth-module/`

8. **Создать `apps/auth-standalone/`** (опционально)
   - Минимальный app: только `authModule`
   - Отдельный `Dockerfile`
   - Для проектов, где нужно только управление пользователями

### Результат
- Auth-модуль можно подключить к любому app одной строкой
- Backoffice работает как раньше
- При необходимости auth деплоится отдельно

### Критерий готовности
- Login, Users, Roles работают в backoffice
- `@broker/auth-module` можно подключить к чистому app
- Все тесты проходят

---

## Фаза 4: Config Service

**Цель:** Сервис конфигурации -- YAML-based, управляет видимостью меню, полей, сущностей.

**Длительность:** 5-7 дней

**Зависимости:** Нет (можно начинать параллельно с фазами 1-3)

### Шаги

1. **Инициализация проекта**
   ```
   config-service/
   ├── src/
   │   ├── Broker.Config.Api/          # ASP.NET Core, port 8086
   │   ├── Broker.Config.Application/
   │   └── Broker.Config.Infrastructure/
   └── config/
       ├── menu.yaml
       ├── entities.yaml
       └── profiles.yaml
   ```

2. **Модель конфигурации**
   - `MenuConfig` -- дерево пунктов меню с permissions
   - `EntityConfig` -- список сущностей, полей, видимость по ролям
   - `ProfileConfig` -- профили доступа (frontend JWT, n8n Basic, external API key)

3. **YAML-загрузчик**
   - Чтение YAML-файлов при старте
   - Hot reload по `FileSystemWatcher` или `POST /config/reload`
   - Валидация схемы при загрузке

4. **API эндпоинты**

   | Method | Endpoint | Описание |
   |--------|----------|----------|
   | `GET` | `/api/v1/config/menu` | Меню для текущего пользователя (фильтрация по JWT permissions) |
   | `GET` | `/api/v1/config/entities` | Список доступных сущностей |
   | `GET` | `/api/v1/config/entities/{name}` | Конфигурация конкретной сущности (поля, действия) |
   | `POST` | `/api/v1/config/reload` | Перезагрузка конфигурации (admin only) |
   | `GET` | `/api/v1/config/profiles` | Профили доступа |

5. **JWT-валидация**
   - Тот же `Jwt:Secret`, та же логика проверки claims
   - Извлечение permission-клеймов для фильтрации

6. **Docker Compose**
   ```yaml
   config:
     build:
       context: .
       dockerfile: Dockerfile.config
     container_name: broker-config
     ports:
       - "8086:8086"
     volumes:
       - ./config-service/config:/app/config:ro
     environment:
       Jwt__Secret: "${JWT_SECRET}"
   ```

7. **Тесты**
   - Unit: YAML-парсинг, фильтрация по ролям, валидация
   - Integration: API эндпоинты с разными JWT-токенами

### Результат
- `GET /config/menu` возвращает динамическое меню по роли
- `GET /config/entities/clients` возвращает конфиг полей
- YAML-конфиг можно менять без деплоя кода

### Критерий готовности
- API работает, меню фильтруется корректно
- YAML-валидация при загрузке
- Hot reload работает
- Тесты покрывают основные сценарии

---

## Фаза 5: Динамический sidebar

**Цель:** Подключить фронтенд к Config Service -- sidebar из API, убрать хардкод.

**Длительность:** 2-3 дня

**Зависимости:** Фаза 2 (ui-kit) + Фаза 4 (Config Service)

### Шаги

1. **Добавить `configApi.ts` в ui-kit**
   - `useMenu()` -- React Query хук, `GET /config/menu`, staleTime 5 минут
   - `useEntityConfig(name)` -- `GET /config/entities/{name}` (на будущее)

2. **Создать `iconMap` в ui-kit**
   - Маппинг строковых имён иконок → MUI Icon компоненты
   - Fallback-иконка для неизвестных имён

3. **Переписать `MainLayout`**
   - Убрать захардкоженный массив `menuItems`
   - Получать из `useMenu()`
   - Рендерить `SidebarItem` по данным API
   - Loading state: skeleton или spinner пока меню грузится

4. **Написать YAML-конфиг меню**
   - Перенести текущую структуру sidebar в `config/menu.yaml`
   - Все текущие пункты + permissions

5. **nginx routing**
   - Добавить проксирование `/api/v1/config/` → `config:8086`

6. **Проверка**
   - Разные роли видят разное меню
   - Collapsed/expanded sidebar работает
   - Mobile drawer работает
   - Submenu (Orders, Transactions) работают

### Результат
- Sidebar полностью управляется из YAML
- Добавление пункта меню -- правка YAML + reload, без деплоя фронта

---

## Фаза 6: API Gateway

**Цель:** Прокси-слой между фронтендом и бэкенд-сервисами. REST→REST/gRPC, field filtering, Swagger generation.

**Длительность:** 10-14 дней

**Зависимости:** Фаза 4 (Config Service, или встроить конфиг в Gateway)

> **Решение:** Config Service может быть **частью** API Gateway (один .NET-проект) или отдельным сервисом. Для начала рекомендуется встроить в Gateway, а при росте -- выделить.

### Шаги

1. **Инициализация проекта**
   ```
   gateway/
   ├── src/
   │   ├── Broker.Gateway.Api/        # ASP.NET Core, port 8090
   │   └── Broker.Gateway.Core/       # Config loader, proxy, field filter
   ├── config/
   │   ├── upstreams.yaml
   │   ├── entities.yaml
   │   ├── profiles.yaml
   │   └── menu.yaml
   └── proto/                          # Копии .proto файлов (при gRPC)
   ```

2. **YAML-конфигурация upstreams**
   ```yaml
   upstreams:
     core:
       protocol: rest
       restAddress: http://broker-api:8080
     auth:
       protocol: rest
       restAddress: http://broker-auth:8082
   ```

3. **REST Proxy Middleware**
   - Маршрутизация `GET /api/v1/clients` → `core`
   - Маршрутизация `GET /api/v1/users` → `auth`
   - Forward headers (Authorization, X-Correlation-Id)
   - Response field filtering по конфигу и роли

4. **Field Filter**
   - Парсинг JSON-ответа от upstream
   - Удаление полей, не разрешённых для текущей роли
   - Фильтрация вложенных объектов и массивов

5. **Access Profile Middleware**
   - JWT (фронтенд) → извлечь роль из claims
   - Basic Auth (n8n) → маппинг на роль из profiles.yaml
   - API Key (внешние) → маппинг на роль из profiles.yaml

6. **Swagger Generator**
   - Генерация OpenAPI spec из entities.yaml
   - Per-profile Swagger: n8n видит свой набор эндпоинтов
   - `GET /swagger/v1/swagger.json`

7. **Config endpoints** (если Config Service встроен)
   - `GET /api/v1/config/menu`
   - `GET /api/v1/config/entities/{name}`

8. **Docker Compose**
   ```yaml
   gateway:
     build:
       context: .
       dockerfile: Dockerfile.gateway
     container_name: broker-gateway
     ports:
       - "8090:8090"
     depends_on:
       api:
         condition: service_healthy
       auth:
         condition: service_healthy
     volumes:
       - ./gateway/config:/app/config:ro
   ```

9. **Переключение фронтенда**
   - `VITE_API_URL` → `http://gateway:8090/api/v1`
   - nginx проксирует `/api/` на gateway вместо api/auth напрямую
   - n8n переключить на gateway

10. **Тесты**
    - Unit: field filtering, config parsing, routing
    - Integration: end-to-end через gateway к реальным сервисам

### Результат
- Единая точка входа для всех клиентов
- Field-level RBAC работает
- n8n ходит через gateway с Basic Auth
- Swagger генерируется из конфига

### Риски

| Риск | Митигация |
|------|-----------|
| Latency: +1 hop на каждый запрос | Измерить overhead, кэшировать конфиг в памяти |
| Gateway -- single point of failure | Health checks, restart policy, горизонтальное масштабирование |
| Сложность отладки | Сквозной Correlation-Id, structured logging |
| Рассинхрон конфига с реальными API | Валидация конфига при старте, integration тесты |

---

## Фаза 7: Report Service

**Цель:** Микросервис отчётов -- генерация, расписание, журнал, доставка.

**Длительность:** 10-14 дней

**Зависимости:** Нет (можно начинать параллельно с фазой 6)

### Шаги

1. **Выбор технологии**
   - Финальное решение: Seal Report (бесплатно) или FastReport ($500)
   - Установка NuGet-пакета, проверка генерации PDF/Excel

2. **Инициализация проекта**
   ```
   report-service/
   ├── src/
   │   ├── Broker.Reports.Domain/        # ReportTemplate, ReportExecution, ReportSchedule
   │   ├── Broker.Reports.Application/   # CQRS handlers
   │   ├── Broker.Reports.Infrastructure/ # EF Core (schema: reports), Rendering, Scheduler
   │   └── Broker.Reports.Api/           # Controllers, port 8084
   ├── templates/                         # .srex или .frx файлы шаблонов
   └── tests/
   ```

3. **Domain**
   - `ReportTemplate` -- имя, описание, файл шаблона, параметры
   - `ReportExecution` -- журнал: кто, когда, какой шаблон, параметры, статус, формат, размер, delivery
   - `ReportSchedule` -- cron-выражение, шаблон, параметры, получатели, активность

4. **Application**
   - `GenerateReport` -- рендеринг по шаблону + параметрам, запись в журнал
   - `GetReportJournal` -- список выполнений с фильтрами и пагинацией
   - `CreateSchedule` / `UpdateSchedule` / `DeleteSchedule`
   - `GetTemplates` -- список доступных шаблонов

5. **Infrastructure**
   - `ReportsDbContext` (schema: `reports`)
   - Rendering engine (Seal Report / FastReport NuGet)
   - Quartz.NET scheduler -- выполняет расписания
   - Email delivery (SMTP)
   - File storage (локальная файловая система / S3)

6. **API эндпоинты**

   | Method | Endpoint | Описание |
   |--------|----------|----------|
   | `GET` | `/api/v1/reports/templates` | Список шаблонов |
   | `POST` | `/api/v1/reports/generate` | Генерация отчёта |
   | `GET` | `/api/v1/reports/journal` | Журнал выполнений |
   | `GET` | `/api/v1/reports/journal/{id}` | Детали выполнения |
   | `GET` | `/api/v1/reports/journal/{id}/download` | Скачать сгенерированный файл |
   | `GET` | `/api/v1/reports/schedules` | Список расписаний |
   | `POST` | `/api/v1/reports/schedules` | Создать расписание |
   | `PUT` | `/api/v1/reports/schedules/{id}` | Обновить расписание |
   | `DELETE` | `/api/v1/reports/schedules/{id}` | Удалить расписание |

7. **Permissions**
   - `reports.read` -- просмотр журнала, скачивание
   - `reports.generate` -- ручная генерация
   - `reports.manage` -- шаблоны, расписания

8. **@broker/reports-module** (фронтенд)
   - `ReportJournalPage` -- таблица с фильтрами (дата, шаблон, пользователь, статус)
   - `ReportTemplatesPage` -- список шаблонов с кнопкой "Сгенерировать"
   - `ReportSchedulesPage` -- CRUD расписаний
   - `ReportDialogs` -- диалог генерации (выбор параметров, формата)
   - Подключить в `apps/backoffice`

9. **Docker Compose**
   ```yaml
   reports:
     build:
       context: .
       dockerfile: Dockerfile.reports
     container_name: broker-reports
     ports:
       - "8084:8084"
     depends_on:
       postgres:
         condition: service_healthy
     volumes:
       - report-templates:/app/templates
       - report-output:/app/output
   ```

10. **Тесты**
    - Unit: валидаторы, парсинг cron, логика журнала
    - Integration: генерация PDF/Excel, API endpoints

### Результат
- Отчёты генерируются по шаблонам
- Журнал: кто, когда, что, куда
- Автоматическая генерация по расписанию
- UI-модуль подключается к любому app

---

## Фаза 8: gRPC интеграция

**Цель:** Добавить gRPC-эндпоинты к существующим сервисам для эффективного межсервисного взаимодействия.

**Длительность:** 7-10 дней

**Зависимости:** Фаза 6 (Gateway, чтобы было кому вызывать gRPC)

### Шаги

1. **Proto-файлы**
   ```
   proto/
   ├── broker/
   │   ├── clients/v1/clients.proto
   │   ├── accounts/v1/accounts.proto
   │   ├── auth/v1/users.proto
   │   ├── auth/v1/roles.proto
   │   └── common/v1/pagination.proto
   ```

2. **Core: добавить gRPC**
   - NuGet: `Grpc.AspNetCore`
   - Реализовать gRPC-сервисы (обёртки над существующими MediatR handlers)
   - Kestrel: HTTP/2 на порту 50051, HTTP/1.1 на 8080
   - REST-эндпоинты **не удаляются**

3. **Auth Service: добавить gRPC**
   - Аналогично: gRPC на 50052, REST на 8082

4. **Report Service: добавить gRPC**
   - gRPC на 50053, REST на 8084

5. **Gateway: gRPC proxy**
   - `IUpstreamClient` абстракция: `GrpcUpstreamClient` + `RestUpstreamClient`
   - В `upstreams.yaml` переключить `protocol: grpc` для нужных upstreams
   - FieldMask в gRPC-запросах -- запрашивать только разрешённые поля

6. **Тесты**
   - gRPC клиентские тесты
   - Gateway: тесты с обоими протоколами

### Результат
- Сервисы поддерживают оба протокола
- Gateway может переключать протокол per-upstream в YAML
- Межсервисные вызовы через gRPC (быстрее, типизация)
- Внешние клиенты по-прежнему получают REST/JSON

---

## Фаза 9: Gateway Admin UI (опционально)

**Цель:** Web-интерфейс для управления конфигурацией Gateway без правки YAML вручную.

**Длительность:** 5-7 дней

**Зависимости:** Фаза 6 (Gateway)

### Шаги

1. **Создать `@broker/gateway-module`**
   - `EntitiesPage` -- список сущностей, включение/выключение
   - `EntityFieldsPage` -- конфигурация полей по ролям
   - `AccessProfilesPage` -- профили доступа (JWT, Basic, API key)
   - `UpstreamsPage` -- управление upstream-сервисами
   - `ConfigDiffPage` -- предпросмотр изменений перед применением

2. **Gateway Admin API**
   - CRUD эндпоинты для конфигурации
   - Валидация перед сохранением
   - Версионирование конфигов
   - `POST /config/apply` -- применить изменения (hot reload)

3. **Подключить в backoffice**
   ```typescript
   import { gatewayModule } from '@broker/gateway-module'
   createApp({ modules: [backofficeModule, authModule, reportsModule, gatewayModule] })
   ```

---

## Порядок и зависимости

```
               Фаза 1: Monorepo
                   │
                   ▼
              Фаза 2: ui-kit
                   │
                   ▼
             Фаза 3: auth-module ─────────────┐
                   │                           │
                   │     Фаза 4: Config Service│(параллельно)
                   │         │                 │
                   ▼         ▼                 │
             Фаза 5: Динамический sidebar      │
                   │                           │
                   ▼                           │
             Фаза 6: API Gateway ◄─────────────┘
                   │
            ┌──────┴──────┐
            ▼             ▼
   Фаза 7: Reports   Фаза 8: gRPC
            │             │
            └──────┬──────┘
                   ▼
         Фаза 9: Gateway Admin UI
```

### Параллельные потоки

**Поток A (фронтенд):** Фаза 1 → 2 → 3 → 5
**Поток B (бэкенд):** Фаза 4 → 6 → 8
**Поток C (отчёты):** Фаза 7 (независим, подключается к gateway когда тот готов)

Потоки A и B можно вести **параллельно** разными разработчиками. Точка соединения -- Фаза 5 (фронт подключается к Config Service).

---

## Временная оценка

| Фаза | Длительность | Параллельно с |
|------|-------------|---------------|
| 1. Monorepo | 2-3 дня | -- |
| 2. ui-kit | 3-5 дней | 4 |
| 3. auth-module | 2-3 дня | 4 |
| 4. Config Service | 5-7 дней | 1, 2, 3 |
| 5. Dynamic sidebar | 2-3 дня | -- |
| 6. API Gateway | 10-14 дней | 7 |
| 7. Report Service | 10-14 дней | 6 |
| 8. gRPC | 7-10 дней | -- |
| 9. Gateway Admin UI | 5-7 дней | -- |

**Критический путь (последовательно):** Фазы 1 → 2 → 3 → 5 → 6 → 8 → 9 ≈ **35-50 дней**

**С параллельной работой (2 разработчика):**
- Разработчик A: Фазы 1 → 2 → 3 → 5 → 7 → 9
- Разработчик B: Фазы 4 → 6 → 8

**Реалистичный срок: 6-8 недель** при двух разработчиках.

---

## Чеклист готовности по фазам

### Фаза 1: Monorepo -- ✅ Выполнено
- [x] pnpm workspaces + Turborepo работают
- [x] `apps/backoffice/` -- текущий код перенесён
- [x] `Dockerfile.web` обновлён для новой структуры
- [x] CI обновлён

### Фаза 2: ui-kit -- ✅ Выполнено
- [x] `@broker/ui-kit` -- все общие компоненты вынесены
- [x] Тема, layout, auth, API client, хуки, утилиты -- в ui-kit
- [x] Все тесты проходят

### Фаза 3: auth-module -- ✅ Выполнено
- [x] `@broker/auth-module` -- auth-страницы переиспользуемы
- [x] LoginPage, UsersPage, RolesPage, ProfileTab вынесены
- [x] Auth API hooks и типы вынесены
- [x] Docker-образ собирается и работает

### Фаза 4: Config Service -- ✅ Выполнено (встроен в API Gateway)
- [x] Config Service встроен в API Gateway
- [x] `GET /config/menu` отвечает с фильтрацией по permissions
- [x] YAML-конфигурация меню, сущностей, upstreams

### Фаза 5: Динамический sidebar -- ✅ Выполнено
- [x] Sidebar формируется из API
- [x] Разные роли видят разное меню
- [x] YAML-конфиг меню покрывает все текущие пункты
- [x] Collapsed/expanded/mobile sidebar работают

### Фаза 6: API Gateway -- ✅ Выполнено (REST proxy, config CRUD)
- [x] Все запросы идут через Gateway
- [x] REST proxy к core и auth service
- [x] Config CRUD endpoints
- [x] n8n работает через Gateway

### Фаза 7: Report Service -- ожидает
- [ ] Шаблоны отчётов создаются в дизайнере
- [ ] Генерация PDF/Excel работает
- [ ] Журнал фиксирует все выполнения
- [ ] Scheduler генерирует по cron
- [ ] `@broker/reports-module` подключён к backoffice

### Фаза 8: gRPC -- ожидает
- [ ] Все сервисы поддерживают gRPC + REST
- [ ] Gateway переключается между протоколами через YAML
- [ ] Proto-файлы покрывают все основные сущности
- [ ] Нет регрессий в REST-функциональности

### Фаза 9: Gateway Admin UI -- частично реализовано (apps/config)
- [x] `apps/config` SPA создано
- [x] MenuEditorPage, EntityFieldsPage, UpstreamsPage
- [ ] Версионирование конфигов
- [ ] ConfigDiffPage -- предпросмотр изменений

---

## Что НЕ меняется

| Компонент | Статус |
|-----------|--------|
| Domain-модели (Client, Account, Order...) | Без изменений |
| CQRS handlers | Без изменений (gRPC -- обёртка сверху) |
| EF Core, миграции | Без изменений |
| JWT-аутентификация | Без изменений |
| Permission-модель (31 право) | Без изменений (Config Service -- надстройка) |
| Audit logging | Без изменений |
| Concurrency (RowVersion/xmin) | Без изменений |
| Integration тесты (Testcontainers) | Без изменений |
| PostgreSQL схема | Добавляется `reports.*`, остальное без изменений |
