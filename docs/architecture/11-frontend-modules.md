# 11. Модульная архитектура фронтенда

## Назначение

Модульная архитектура позволяет разделить фронтенд на **независимые пакеты** (UI-модули), которые:

1. **Разрабатываются независимо** -- разные команды работают над разными модулями в одном монорепозитории
2. **Деплоятся отдельно** -- каждый модуль собирается в самостоятельное SPA со своим Docker-образом
3. **Переиспользуются между проектами** -- auth-модуль подключается к любому проекту как npm-зависимость
4. **Сохраняют единый стиль** -- общая дизайн-система, тема, компоненты через `@broker/ui-kit`
5. **Управляются через Config Service** -- sidebar, видимость пунктов меню, доступы -- приходят с сервера

### Зачем нужно

| Проблема | Решение |
|----------|---------|
| Весь фронтенд -- один бандл, деплоится целиком | Каждый модуль -- отдельное SPA, свой Dockerfile, свой релизный цикл |
| Auth UI нужен на соседнем проекте | `@broker/auth-module` подключается как npm-зависимость |
| Sidebar захардкожен, для скрытия пункта нужен деплой | Sidebar конфигурируется через Config Service, по ролям |
| Разные команды конфликтуют в одной кодовой базе | Изолированные пакеты в monorepo, чёткие границы |
| Новый проект -- заново создавать тему, компоненты, авторизацию | `@broker/ui-kit` -- подключил и получил всё |
| Report Service UI нужен только на продакшене с отчётами | Модуль `@broker/reports-module` подключается по необходимости |

---

## Целевая архитектура

```
                    ┌─────────────────────────────────────────┐
                    │          pnpm monorepo + Turborepo       │
                    │                                         │
  ┌─────────────────┤  packages/ (shared)                     │
  │                 │  ├── ui-kit/          @broker/ui-kit     │
  │                 │  ├── auth-module/     @broker/auth-module│
  │                 │  └── reports-module/  @broker/reports-module
  │                 │                                         │
  │                 │  apps/ (deployable SPA)                  │
  │                 │  ├── backoffice/      Основная админка   │
  │                 │  ├── auth-standalone/ Auth отдельно      │
  │                 │  ├── reports/         Reports отдельно   │
  │                 │  └── other-project/   Соседний проект    │
  │                 └─────────────────────────────────────────┘
  │
  │  npm install @broker/ui-kit @broker/auth-module
  │
  ▼
┌──────────────────────────────────┐
│  apps/backoffice                 │
│  ┌──────────┐ ┌───────────────┐ │
│  │ ui-kit   │ │ auth-module   │ │
│  │ (тема,   │ │ (Users,Roles, │ │
│  │ layout,  │ │  Login)       │ │
│  │ компон.) │ │               │ │
│  └──────────┘ └───────────────┘ │
│  ┌──────────────────────────────┤
│  │ Собственные страницы:        │
│  │ Clients, Accounts, Orders,   │
│  │ Instruments, Transactions,   │
│  │ Dashboard, Settings, Audit   │
│  └──────────────────────────────┘
        │
        │ Dockerfile.web-backoffice
        ▼
  Docker image: broker/web-backoffice:1.2.3

┌──────────────────────────────────┐
│  apps/other-project              │
│  ┌──────────┐ ┌───────────────┐ │
│  │ ui-kit   │ │ auth-module   │ │
│  └──────────┘ └───────────────┘ │
│  ┌──────────────────────────────┤
│  │ Собственные страницы проекта │
│  └──────────────────────────────┘
        │
        │ Dockerfile.web-other
        ▼
  Docker image: broker/web-other:1.0.0
```

---

## Структура монорепозитория

```
frontend/
├── packages/
│   ├── ui-kit/                          # @broker/ui-kit
│   │   ├── src/
│   │   │   ├── components/
│   │   │   │   ├── PageContainer.tsx     # Обёртка страницы (title, actions, breadcrumbs)
│   │   │   │   ├── FilteredDataGrid.tsx  # DataGrid + inline-фильтры + empty state
│   │   │   │   ├── GridFilterRow.tsx     # Рендеринг строки фильтров
│   │   │   │   ├── ConfirmDialog.tsx     # Диалог подтверждения удаления
│   │   │   │   ├── UserAvatar.tsx        # Аватар (фото или инициалы)
│   │   │   │   ├── Breadcrumbs.tsx       # Навигационные хлебные крошки
│   │   │   │   ├── DetailField.tsx       # Поле label + value
│   │   │   │   ├── ExportButton.tsx      # Кнопка экспорта в Excel
│   │   │   │   ├── GlobalSearchBar.tsx   # Поиск по сущностям
│   │   │   │   ├── ErrorBoundary.tsx     # Error boundary с MUI fallback
│   │   │   │   ├── RouteLoadingFallback.tsx
│   │   │   │   ├── EntityHistoryDialog.tsx
│   │   │   │   ├── AuditDetailDialog.tsx
│   │   │   │   └── index.ts             # Реэкспорт всех компонентов
│   │   │   ├── layouts/
│   │   │   │   ├── MainLayout.tsx        # Sidebar + content area
│   │   │   │   ├── SidebarItem.tsx       # Пункт меню (иконка, label, children)
│   │   │   │   └── index.ts
│   │   │   ├── theme/
│   │   │   │   ├── index.ts             # createAppTheme, SIDEBAR_COLORS, STAT_GRADIENTS
│   │   │   │   ├── ThemeContext.tsx      # AppThemeProvider, useThemeMode, useListTheme
│   │   │   │   └── index.ts
│   │   │   ├── auth/
│   │   │   │   ├── AuthContext.tsx       # AuthProvider, AuthState
│   │   │   │   ├── useAuth.ts           # useAuth hook
│   │   │   │   ├── usePermission.ts     # useHasPermission hook
│   │   │   │   ├── RequireAuth.tsx      # Route guard
│   │   │   │   └── index.ts
│   │   │   ├── api/
│   │   │   │   ├── client.ts            # Axios instance, interceptors, token refresh
│   │   │   │   ├── configApi.ts         # useMenu(), useEntityConfig()
│   │   │   │   └── index.ts
│   │   │   ├── hooks/
│   │   │   │   ├── useDebounce.ts
│   │   │   │   ├── useConfirm.ts
│   │   │   │   └── index.ts
│   │   │   ├── utils/
│   │   │   │   ├── exportToExcel.ts
│   │   │   │   ├── extractErrorMessage.ts
│   │   │   │   ├── validateFields.ts
│   │   │   │   └── index.ts
│   │   │   ├── icons.ts                 # iconMap: string → MUI Icon component
│   │   │   ├── types.ts                 # MenuItem, ModuleDefinition, общие типы
│   │   │   └── index.ts                 # Главный реэкспорт
│   │   ├── package.json
│   │   └── tsconfig.json
│   │
│   ├── auth-module/                     # @broker/auth-module
│   │   ├── src/
│   │   │   ├── pages/
│   │   │   │   ├── LoginPage.tsx
│   │   │   │   ├── UsersPage.tsx
│   │   │   │   ├── UserDetailsPage.tsx
│   │   │   │   ├── UserDialogs.tsx
│   │   │   │   ├── RolesPage.tsx
│   │   │   │   ├── RoleDetailsPage.tsx
│   │   │   │   ├── RoleDialogs.tsx
│   │   │   │   └── ProfileTab.tsx
│   │   │   ├── api/
│   │   │   │   ├── types.ts             # UserDto, RoleDto, PermissionDto
│   │   │   │   └── hooks.ts            # useUsers, useRoles, useLogin, useMe...
│   │   │   ├── routes.ts               # Экспорт RouteObject[]
│   │   │   └── index.ts                # Экспорт модуля
│   │   ├── package.json
│   │   └── tsconfig.json
│   │
│   └── reports-module/                  # @broker/reports-module (будущий)
│       ├── src/
│       │   ├── pages/
│       │   │   ├── ReportJournalPage.tsx
│       │   │   ├── ReportTemplatesPage.tsx
│       │   │   ├── ReportSchedulesPage.tsx
│       │   │   └── ReportDialogs.tsx
│       │   ├── api/
│       │   │   ├── types.ts
│       │   │   └── hooks.ts
│       │   ├── routes.ts
│       │   └── index.ts
│       └── package.json
│
├── apps/
│   ├── backoffice/                      # Основная админка
│   │   ├── src/
│   │   │   ├── pages/
│   │   │   │   ├── DashboardPage.tsx
│   │   │   │   ├── ClientsPage.tsx
│   │   │   │   ├── ClientDetailsPage.tsx
│   │   │   │   ├── ClientDialogs.tsx
│   │   │   │   ├── AccountsPage.tsx
│   │   │   │   ├── ... (все бизнес-страницы)
│   │   │   │   ├── AuditPage.tsx
│   │   │   │   ├── SettingsPage.tsx
│   │   │   │   └── settings/
│   │   │   ├── api/
│   │   │   │   ├── types.ts             # ClientDto, AccountDto, OrderDto...
│   │   │   │   └── hooks.ts            # useClients, useAccounts, useOrders...
│   │   │   ├── module.ts               # Экспорт ModuleDefinition
│   │   │   └── main.tsx                # Точка входа
│   │   ├── Dockerfile
│   │   ├── nginx.conf
│   │   ├── vite.config.ts
│   │   └── package.json
│   │
│   ├── auth-standalone/                 # Auth как отдельное SPA (опционально)
│   │   ├── src/
│   │   │   ├── main.tsx
│   │   │   └── module.ts
│   │   ├── Dockerfile
│   │   └── package.json
│   │
│   └── other-project/                   # Соседний проект
│       ├── src/
│       │   ├── pages/                   # Страницы этого проекта
│       │   ├── api/
│       │   ├── module.ts
│       │   └── main.tsx
│       ├── Dockerfile
│       └── package.json
│
├── pnpm-workspace.yaml
├── turbo.json
├── package.json                         # Root package
└── tsconfig.base.json                   # Общий tsconfig
```

---

## Пакеты

### @broker/ui-kit

Ядро дизайн-системы. Содержит всё, что одинаково для всех приложений.

**Зависимости:**
```json
{
  "name": "@broker/ui-kit",
  "peerDependencies": {
    "react": "^18.3.0",
    "react-dom": "^18.3.0",
    "@mui/material": "^6.1.0",
    "@mui/icons-material": "^6.1.0",
    "@mui/x-data-grid": "^7.18.0",
    "@tanstack/react-query": "^5.56.0",
    "react-router-dom": "^6.26.0",
    "axios": "^1.7.0",
    "notistack": "^3.0.2"
  }
}
```

**Экспорт:**

| Категория | Экспорты |
|-----------|----------|
| Компоненты | `PageContainer`, `FilteredDataGrid`, `ConfirmDialog`, `UserAvatar`, `Breadcrumbs`, `DetailField`, `ExportButton`, `GlobalSearchBar`, `ErrorBoundary`, `RouteLoadingFallback`, `EntityHistoryDialog`, `AuditDetailDialog` |
| Layout | `MainLayout`, `SidebarItem` |
| Тема | `createAppTheme`, `createAppListTheme`, `AppThemeProvider`, `useThemeMode`, `useListTheme`, `SIDEBAR_COLORS`, `STAT_GRADIENTS` |
| Auth | `AuthProvider`, `useAuth`, `useHasPermission`, `RequireAuth` |
| API | `apiClient` (Axios), `useMenu`, `useEntityConfig` |
| Хуки | `useDebounce`, `useConfirm` |
| Утилиты | `exportToExcel`, `extractErrorMessage`, `validateRequired`, `validateEmail` |
| Типы | `MenuItem`, `ModuleDefinition`, `PagedResult`, `FieldErrors` |
| Иконки | `iconMap` -- маппинг `string → React.ComponentType` |

### @broker/auth-module

UI для auth-сервиса. Подключается к любому проекту, где нужно управление пользователями и ролями.

**Зависимости:**
```json
{
  "name": "@broker/auth-module",
  "dependencies": {
    "@broker/ui-kit": "workspace:*"
  },
  "peerDependencies": {
    "react": "^18.3.0",
    "@mui/material": "^6.1.0",
    "@tanstack/react-query": "^5.56.0",
    "react-router-dom": "^6.26.0"
  }
}
```

**Содержит:**

| Компонент | Описание |
|-----------|----------|
| `LoginPage` | Split-screen логин (брендированная левая панель + форма) |
| `UsersPage` | Список пользователей с фильтрами |
| `UserDetailsPage` | Детальная карточка пользователя |
| `UserDialogs` | Создание/редактирование/фото пользователя |
| `RolesPage` | Список ролей |
| `RoleDetailsPage` | Роль + назначенные permissions |
| `RoleDialogs` | Создание/редактирование роли |
| `ProfileTab` | Профиль, фото, смена пароля |
| API hooks | `useUsers`, `useUser`, `useCreateUser`, `useUpdateUser`, `useDeleteUser`, `useRoles`, `useRole`, `useCreateRole`, `useUpdateRole`, `useDeleteRole`, `usePermissions`, `useLogin`, `useRefreshToken`, `useMe`, `useChangePassword`, `useUpdateProfile`, `useUploadPhoto`, `useDeletePhoto` |
| Типы | `UserDto`, `RoleDto`, `PermissionDto`, `AuthResponse`, `UserProfile` |

### @broker/reports-module (будущий)

UI для сервиса отчётов. Подключается к проектам, где нужна генерация и журналирование отчётов.

**Содержит:**

| Компонент | Описание |
|-----------|----------|
| `ReportJournalPage` | Журнал: кто, когда, какой отчёт, куда отправил |
| `ReportTemplatesPage` | Список шаблонов отчётов |
| `ReportSchedulesPage` | Расписания автоматической генерации |
| `ReportDialogs` | Генерация отчёта вручную, настройка расписания |
| API hooks | `useReportJournal`, `useReportTemplates`, `useSchedules`, `useGenerateReport` |

---

## Регистрация модулей

### ModuleDefinition

Каждый модуль экспортирует объект `ModuleDefinition`:

```typescript
// packages/ui-kit/src/types.ts
export interface ModuleDefinition {
  name: string               // Совпадает с menu.yaml → module
  routes: RouteObject[]      // React Router v6 route objects
  loginPage?: ComponentType  // Кастомная страница логина (опционально)
}
```

### Экспорт из модуля

```typescript
// packages/auth-module/src/routes.ts
import type { RouteObject } from 'react-router-dom'

export const authRoutes: RouteObject[] = [
  {
    path: '/users',
    lazy: () => import('./pages/UsersPage').then(m => ({ Component: m.UsersPage })),
  },
  {
    path: '/users/:id',
    lazy: () => import('./pages/UserDetailsPage').then(m => ({ Component: m.UserDetailsPage })),
  },
  {
    path: '/roles',
    lazy: () => import('./pages/RolesPage').then(m => ({ Component: m.RolesPage })),
  },
  {
    path: '/roles/:id',
    lazy: () => import('./pages/RoleDetailsPage').then(m => ({ Component: m.RoleDetailsPage })),
  },
]

// packages/auth-module/src/index.ts
import type { ModuleDefinition } from '@broker/ui-kit'
import { authRoutes } from './routes'

export const authModule: ModuleDefinition = {
  name: 'auth',
  routes: authRoutes,
}
```

### Подключение в приложении

```typescript
// apps/backoffice/src/module.ts
import type { ModuleDefinition } from '@broker/ui-kit'

export const backofficeModule: ModuleDefinition = {
  name: 'backoffice',
  routes: [
    {
      index: true,
      lazy: () => import('./pages/DashboardPage').then(m => ({ Component: m.DashboardPage })),
    },
    {
      path: '/clients',
      lazy: () => import('./pages/ClientsPage').then(m => ({ Component: m.ClientsPage })),
    },
    {
      path: '/clients/:id',
      lazy: () => import('./pages/ClientDetailsPage').then(m => ({ Component: m.ClientDetailsPage })),
    },
    // ... остальные бизнес-роуты
  ],
}
```

```typescript
// apps/backoffice/src/main.tsx
import { createApp } from '@broker/ui-kit'
import { backofficeModule } from './module'
import { authModule } from '@broker/auth-module'
import { reportsModule } from '@broker/reports-module'

createApp({
  modules: [backofficeModule, authModule, reportsModule],
})
```

```typescript
// apps/other-project/src/main.tsx
import { createApp } from '@broker/ui-kit'
import { otherProjectModule } from './module'
import { authModule } from '@broker/auth-module'
// reports-module не подключаем -- на этом проекте его нет

createApp({
  modules: [otherProjectModule, authModule],
})
```

### createApp

Фабричная функция в `@broker/ui-kit`, собирающая приложение из модулей:

```typescript
// packages/ui-kit/src/createApp.tsx
import { createBrowserRouter, RouterProvider } from 'react-router-dom'

interface AppConfig {
  modules: ModuleDefinition[]
}

export function createApp({ modules }: AppConfig) {
  // 1. Собрать все роуты из модулей
  const allRoutes = modules.flatMap(m => m.routes)

  // 2. Найти LoginPage (из auth-module или дефолтный)
  const authMod = modules.find(m => m.loginPage)
  const LoginPage = authMod?.loginPage ?? DefaultLoginPage

  // 3. Собрать роутер
  const router = createBrowserRouter([
    { path: '/login', element: <LoginPage /> },
    {
      path: '/',
      element: <RequireAuth><MainLayout /></RequireAuth>,
      errorElement: <ErrorBoundary />,
      children: [
        ...allRoutes,
        { path: '*', element: <NotFoundPage /> },
      ],
    },
  ])

  // 4. Рендер с провайдерами
  const root = createRoot(document.getElementById('root')!)
  root.render(
    <StrictMode>
      <QueryClientProvider client={queryClient}>
        <AppThemeProvider>
          <SnackbarProvider maxSnack={3} anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}>
            <LocalizationProvider dateAdapter={AdapterDayjs}>
              <AuthProvider>
                <RouterProvider router={router} />
              </AuthProvider>
            </LocalizationProvider>
          </SnackbarProvider>
        </AppThemeProvider>
      </QueryClientProvider>
    </StrictMode>
  )
}
```

---

## Динамический Sidebar

### Конфигурация меню через Config Service

Sidebar **не содержит** захардкоженных пунктов меню. Структура, видимость и порядок определяются в YAML-конфиге на стороне Config Service и отдаются по API в зависимости от роли пользователя.

### YAML-конфигурация

```yaml
# config/menu.yaml
menu:
  - id: dashboard
    label: Dashboard
    icon: Dashboard
    path: /
    module: backoffice
    permissions: []                    # Все аутентифицированные видят

  - id: clients
    label: Clients
    icon: People
    path: /clients
    module: backoffice
    permissions: [clients.read]

  - id: accounts
    label: Accounts
    icon: AccountBalance
    path: /accounts
    module: backoffice
    permissions: [accounts.read]

  - id: instruments
    label: Instruments
    icon: ShowChart
    path: /instruments
    module: backoffice
    permissions: [instruments.read]

  - id: orders
    label: Orders
    icon: Receipt
    module: backoffice
    children:
      - id: trade-orders
        label: Trade Orders
        path: /trade-orders
        permissions: [orders.read]
      - id: non-trade-orders
        label: Non-Trade Orders
        path: /non-trade-orders
        permissions: [orders.read]

  - id: transactions
    label: Transactions
    icon: SwapHoriz
    module: backoffice
    children:
      - id: trade-transactions
        label: Trade Transactions
        path: /trade-transactions
        permissions: [transactions.read]
      - id: non-trade-transactions
        label: Non-Trade Transactions
        path: /non-trade-transactions
        permissions: [transactions.read]

  - id: users
    label: Users
    icon: Group
    path: /users
    module: auth
    permissions: [users.read]

  - id: roles
    label: Roles
    icon: Shield
    path: /roles
    module: auth
    permissions: [roles.read]

  - id: reports
    label: Reports
    icon: Assessment
    module: reports
    children:
      - id: report-journal
        label: Journal
        path: /reports
        permissions: [reports.read]
      - id: report-templates
        label: Templates
        path: /reports/templates
        permissions: [reports.manage]
      - id: report-schedules
        label: Schedules
        path: /reports/schedules
        permissions: [reports.manage]

  - id: audit
    label: Audit Log
    icon: History
    path: /audit
    module: backoffice
    permissions: [audit.read]

  - id: settings
    label: Settings
    icon: Settings
    path: /settings
    module: backoffice
    permissions: [settings.manage]
```

### API эндпоинт

```
GET /api/v1/config/menu
Authorization: Bearer {jwt}
```

Config Service читает YAML, фильтрует пункты по permissions из JWT-токена текущего пользователя и возвращает только разрешённые:

```json
[
  {
    "id": "dashboard",
    "label": "Dashboard",
    "icon": "Dashboard",
    "path": "/",
    "module": "backoffice"
  },
  {
    "id": "clients",
    "label": "Clients",
    "icon": "People",
    "path": "/clients",
    "module": "backoffice"
  },
  {
    "id": "orders",
    "label": "Orders",
    "icon": "Receipt",
    "module": "backoffice",
    "children": [
      { "id": "trade-orders", "label": "Trade Orders", "path": "/trade-orders" },
      { "id": "non-trade-orders", "label": "Non-Trade Orders", "path": "/non-trade-orders" }
    ]
  },
  {
    "id": "users",
    "label": "Users",
    "icon": "Group",
    "path": "/users",
    "module": "auth"
  }
]
```

> **Важно:** Config Service фильтрует по permissions на стороне сервера. Фронтенд **не делает** проверку `useHasPermission()` для пунктов меню -- он рисует ровно то, что пришло.

### Маппинг иконок

Sidebar получает имя иконки как строку (`"Dashboard"`, `"People"`) и преобразует в React-компонент через `iconMap`:

```typescript
// packages/ui-kit/src/icons.ts
import {
  Dashboard, People, AccountBalance, ShowChart,
  Receipt, SwapHoriz, Group, Shield,
  Assessment, History, Settings,
} from '@mui/icons-material'
import type { ComponentType } from 'react'

export const iconMap: Record<string, ComponentType> = {
  Dashboard,
  People,
  AccountBalance,
  ShowChart,
  Receipt,
  SwapHoriz,
  Group,
  Shield,
  Assessment,
  History,
  Settings,
}
```

Новые иконки добавляются по мере появления новых модулей. Если иконка не найдена в `iconMap`, рендерится fallback-иконка (например `Circle`).

### Компонент MainLayout

```typescript
// packages/ui-kit/src/layouts/MainLayout.tsx
import { useMenu } from '../api/configApi'
import { iconMap } from '../icons'
import { SidebarItem } from './SidebarItem'
import { SIDEBAR_COLORS } from '../theme'

export function MainLayout() {
  const { data: menuItems = [], isLoading } = useMenu()
  const [collapsed, setCollapsed] = useState(
    () => localStorage.getItem('sidebarCollapsed') === 'true'
  )

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh' }}>
      <Sidebar collapsed={collapsed}>
        <Logo collapsed={collapsed} />

        {menuItems.map(item => (
          <SidebarItem
            key={item.id}
            label={item.label}
            icon={iconMap[item.icon] ?? CircleIcon}
            path={item.path}
            collapsed={collapsed}
            children={item.children}
          />
        ))}

        <CollapseToggle collapsed={collapsed} onToggle={setCollapsed} />
        <UserSection collapsed={collapsed} />
      </Sidebar>

      <Box component="main" sx={{ flex: 1, overflow: 'auto' }}>
        <ErrorBoundary>
          <Outlet />
        </ErrorBoundary>
      </Box>
    </Box>
  )
}
```

### Кэширование меню

```typescript
// packages/ui-kit/src/api/configApi.ts
export const useMenu = () =>
  useQuery<MenuItem[]>({
    queryKey: ['config', 'menu'],
    queryFn: () => apiClient.get('/config/menu').then(r => r.data),
    staleTime: 5 * 60 * 1000,   // 5 минут
    gcTime: 10 * 60 * 1000,     // 10 минут
  })
```

Меню кэшируется на 5 минут. При смене роли пользователя (редкая операция) -- достаточно обновить страницу или вызвать `queryClient.invalidateQueries(['config', 'menu'])`.

---

## Конфигурация для разных проектов

Каждый проект (app) работает с **своим экземпляром Config Service** (или одним Config Service с разными конфигами по идентификатору проекта).

### Проект A: Backoffice (все модули)

```yaml
# config/projects/backoffice/menu.yaml
menu:
  - { id: dashboard, label: Dashboard, icon: Dashboard, path: /, module: backoffice }
  - { id: clients, label: Clients, icon: People, path: /clients, module: backoffice, permissions: [clients.read] }
  - { id: accounts, label: Accounts, icon: AccountBalance, path: /accounts, module: backoffice, permissions: [accounts.read] }
  # ... полное меню (backoffice + auth + reports)
```

```typescript
// apps/backoffice/src/main.tsx
createApp({
  modules: [backofficeModule, authModule, reportsModule],
})
```

### Проект B: Соседний проект (только auth + свои страницы)

```yaml
# config/projects/other/menu.yaml
menu:
  - { id: dashboard, label: Dashboard, icon: Dashboard, path: /, module: other }
  - { id: analytics, label: Analytics, icon: BarChart, path: /analytics, module: other }
  - { id: users, label: Users, icon: Group, path: /users, module: auth, permissions: [users.read] }
  - { id: roles, label: Roles, icon: Shield, path: /roles, module: auth, permissions: [roles.read] }
```

```typescript
// apps/other-project/src/main.tsx
createApp({
  modules: [otherProjectModule, authModule],
  // reports-module не подключён — даже если конфиг вернёт пункт reports, роут не найдётся → 404
})
```

### Проект C: Только управление пользователями

```yaml
# config/projects/auth-standalone/menu.yaml
menu:
  - { id: users, label: Users, icon: Group, path: /users, module: auth, permissions: [users.read] }
  - { id: roles, label: Roles, icon: Shield, path: /roles, module: auth, permissions: [roles.read] }
```

```typescript
// apps/auth-standalone/src/main.tsx
createApp({
  modules: [authModule],
})
```

---

## Сборка и деплой

### pnpm workspaces

```yaml
# pnpm-workspace.yaml
packages:
  - 'packages/*'
  - 'apps/*'
```

### Turborepo

```json
// turbo.json
{
  "$schema": "https://turbo.build/schema.json",
  "tasks": {
    "build": {
      "dependsOn": ["^build"],
      "outputs": ["dist/**"]
    },
    "dev": {
      "dependsOn": ["^build"],
      "persistent": true,
      "cache": false
    },
    "test": {
      "dependsOn": ["^build"]
    },
    "lint": {}
  }
}
```

`turbo run build` автоматически определяет порядок: сначала `ui-kit`, затем `auth-module`, затем `apps/*`. Кэширует результаты -- если `ui-kit` не менялся, не пересобирается.

### Dockerfile (каждый app -- отдельный образ)

```dockerfile
# apps/backoffice/Dockerfile
FROM node:20-alpine AS deps
WORKDIR /app
RUN corepack enable && corepack prepare pnpm@latest --activate
COPY pnpm-workspace.yaml pnpm-lock.yaml package.json ./
COPY packages/ui-kit/package.json packages/ui-kit/
COPY packages/auth-module/package.json packages/auth-module/
COPY packages/reports-module/package.json packages/reports-module/
COPY apps/backoffice/package.json apps/backoffice/
RUN pnpm install --frozen-lockfile

FROM node:20-alpine AS build
WORKDIR /app
RUN corepack enable && corepack prepare pnpm@latest --activate
COPY --from=deps /app/node_modules ./node_modules
COPY packages/ packages/
COPY apps/backoffice/ apps/backoffice/
COPY pnpm-workspace.yaml turbo.json package.json ./
RUN pnpm turbo build --filter=@broker/backoffice

FROM nginx:alpine
COPY apps/backoffice/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/apps/backoffice/dist /usr/share/nginx/html
RUN chown -R nginx:nginx /usr/share/nginx/html
USER nginx
EXPOSE 8080
```

### CI -- path-based триггеры

```yaml
# .github/workflows/deploy-web-backoffice.yml
name: Deploy Backoffice Web
on:
  push:
    branches: [main]
    paths:
      - 'frontend/packages/ui-kit/**'
      - 'frontend/packages/auth-module/**'
      - 'frontend/packages/reports-module/**'
      - 'frontend/apps/backoffice/**'
      - 'frontend/pnpm-lock.yaml'

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: pnpm/action-setup@v4
      - uses: actions/setup-node@v4
        with:
          node-version: 20
          cache: pnpm
          cache-dependency-path: frontend/pnpm-lock.yaml
      - run: pnpm install --frozen-lockfile
        working-directory: frontend
      - run: pnpm turbo build --filter=@broker/backoffice
        working-directory: frontend
      - run: docker build -f apps/backoffice/Dockerfile -t broker/web-backoffice:${{ github.sha }} .
        working-directory: frontend
      - run: docker push broker/web-backoffice:${{ github.sha }}
```

```yaml
# .github/workflows/deploy-web-other.yml
name: Deploy Other Project Web
on:
  push:
    branches: [main]
    paths:
      - 'frontend/packages/ui-kit/**'
      - 'frontend/packages/auth-module/**'
      - 'frontend/apps/other-project/**'
      - 'frontend/pnpm-lock.yaml'
```

Изменение в `ui-kit` триггерит пересборку **всех** app. Изменение только в `apps/backoffice/` триггерит сборку **только** backoffice.

### Docker Compose (полный стек)

```yaml
# docker-compose.yml (дополнение к существующим сервисам)
services:
  web-backoffice:
    build:
      context: frontend
      dockerfile: apps/backoffice/Dockerfile
    container_name: broker-web-backoffice
    restart: unless-stopped
    ports:
      - "3000:8080"
    depends_on:
      api:
        condition: service_healthy

  web-other:
    build:
      context: frontend
      dockerfile: apps/other-project/Dockerfile
    container_name: broker-web-other
    restart: unless-stopped
    ports:
      - "3001:8080"
    depends_on:
      api:
        condition: service_healthy
```

### Docker-образы в registry

```
registry.example.com/broker/web-backoffice:1.2.3
registry.example.com/broker/web-backoffice:latest
registry.example.com/broker/web-other:1.0.0
registry.example.com/broker/web-other:latest
registry.example.com/broker/web-reports:1.0.0   # если деплоится отдельно
```

Каждая площадка тянет только нужные образы.

---

## Разработка

### Локальный запуск одного модуля

```bash
cd frontend

# Запустить только backoffice (+ зависимости ui-kit, auth-module)
pnpm turbo dev --filter=@broker/backoffice

# Запустить только auth-standalone
pnpm turbo dev --filter=@broker/auth-standalone

# Запустить все apps параллельно
pnpm turbo dev
```

### Добавление нового модуля

1. Создать пакет:
```bash
mkdir -p packages/new-module/src/{pages,api}
```

2. `packages/new-module/package.json`:
```json
{
  "name": "@broker/new-module",
  "version": "0.0.1",
  "private": true,
  "main": "src/index.ts",
  "dependencies": {
    "@broker/ui-kit": "workspace:*"
  }
}
```

3. Экспортировать `ModuleDefinition`:
```typescript
// packages/new-module/src/index.ts
import type { ModuleDefinition } from '@broker/ui-kit'

export const newModule: ModuleDefinition = {
  name: 'new-module',
  routes: [
    { path: '/new-feature', lazy: () => import('./pages/NewFeaturePage') },
  ],
}
```

4. Подключить в нужном app:
```typescript
// apps/backoffice/src/main.tsx
import { newModule } from '@broker/new-module'

createApp({
  modules: [backofficeModule, authModule, reportsModule, newModule],
})
```

5. Добавить пункт в `menu.yaml` Config Service:
```yaml
  - id: new-feature
    label: New Feature
    icon: Star
    path: /new-feature
    module: new-module
    permissions: [new-feature.read]
```

### Тестирование

Каждый пакет имеет свои тесты:

```bash
# Тесты ui-kit
pnpm turbo test --filter=@broker/ui-kit

# Тесты auth-module
pnpm turbo test --filter=@broker/auth-module

# Тесты конкретного app
pnpm turbo test --filter=@broker/backoffice

# Все тесты
pnpm turbo test
```

Стек тестирования единый: Vitest + React Testing Library + MSW. Общие тестовые утилиты (`renderWithProviders`, MSW handlers) выносятся в `packages/ui-kit/test/` или в отдельный пакет `packages/test-utils/`.

---

## Миграция с текущей структуры

### Текущее состояние

```
frontend/
├── src/
│   ├── api/            # types.ts (1120 строк), hooks.ts (830 строк), client.ts
│   ├── auth/           # AuthContext, useAuth, usePermission, RequireAuth
│   ├── components/     # 14 общих компонентов
│   ├── hooks/          # useDebounce, useConfirm
│   ├── layouts/        # MainLayout (sidebar захардкожен)
│   ├── pages/          # 20+ страниц (все модули вместе)
│   ├── theme/          # createAppTheme, ThemeContext
│   ├── utils/          # exportToExcel, validateFields, extractErrorMessage
│   └── main.tsx
```

### План миграции

**Фаза 1: Инициализация монорепо**

1. Инициализировать pnpm workspaces и Turborepo
2. Создать `packages/ui-kit/` и перенести:
   - `src/components/*` → `packages/ui-kit/src/components/`
   - `src/theme/*` → `packages/ui-kit/src/theme/`
   - `src/hooks/*` → `packages/ui-kit/src/hooks/`
   - `src/utils/*` → `packages/ui-kit/src/utils/`
   - `src/auth/*` → `packages/ui-kit/src/auth/`
   - `src/api/client.ts` → `packages/ui-kit/src/api/client.ts`
   - `src/layouts/MainLayout.tsx` → `packages/ui-kit/src/layouts/`
3. Текущий `frontend/src/` → `apps/backoffice/src/`
4. Заменить относительные импорты на `@broker/ui-kit`
5. Проверить: `pnpm turbo build && pnpm turbo test` -- всё должно работать как раньше

**Фаза 2: Выделение auth-module**

1. Создать `packages/auth-module/`
2. Перенести auth-страницы из `apps/backoffice/src/pages/`:
   - `LoginPage.tsx`, `UsersPage.tsx`, `UserDetailsPage.tsx`, `UserDialogs.tsx`
   - `RolesPage.tsx`, `RoleDetailsPage.tsx`, `RoleDialogs.tsx`
   - `settings/ProfileTab.tsx`
3. Перенести auth-хуки и типы из `apps/backoffice/src/api/`:
   - Вырезать из `hooks.ts`: `useUsers`, `useRoles`, `usePermissions`, `useLogin`, `useMe`...
   - Вырезать из `types.ts`: `UserDto`, `RoleDto`, `PermissionDto`, `AuthResponse`...
4. Экспортировать `authModule: ModuleDefinition`
5. В `apps/backoffice/` подключить `@broker/auth-module`

**Фаза 3: Динамический sidebar**

1. Добавить `useMenu()` хук в `@broker/ui-kit`
2. Переписать `MainLayout` -- убрать захардкоженные пункты, получать из API
3. Добавить `iconMap` и fallback для неизвестных иконок
4. Настроить YAML-конфиг в Config Service
5. Добавить эндпоинт `GET /api/v1/config/menu`

**Фаза 4: Новые модули**

1. `@broker/reports-module` -- при реализации Report Service
2. `@broker/gateway-module` -- при реализации Gateway Admin UI
3. Каждый подключается к нужным apps через `createApp({ modules: [...] })`

---

## Технологический стек

| Компонент | Технология | Версия |
|-----------|------------|--------|
| Package manager | pnpm | 9.x |
| Monorepo orchestrator | Turborepo | 2.x |
| Build tool | Vite | 5.x |
| UI framework | React | 18.3 |
| Language | TypeScript | 5.x (strict) |
| Component library | MUI | 6.x |
| Data grid | MUI X DataGrid | 7.x |
| Server state | TanStack React Query | 5.x |
| Routing | React Router | 6.x |
| HTTP client | Axios | 1.x |
| Notifications | notistack | 3.x |
| Charts | Recharts | 3.x |
| Excel export | ExcelJS | 4.x |
| Testing | Vitest + RTL + MSW | latest |
| Linting | ESLint 9 (flat config) | 9.x |
| Container | nginx:alpine (non-root) | latest |
| CI orchestration | GitHub Actions | - |

---

## Гарантии единого стиля

| Механизм | Что обеспечивает |
|----------|------------------|
| `@broker/ui-kit` | Единая тема, цвета, шрифты, компоненты |
| `createAppTheme()` | Одна функция создания темы для всех apps |
| `SIDEBAR_COLORS` | Единые токены тёмного sidebar |
| `MainLayout` | Одинаковый sidebar layout везде |
| `FilteredDataGrid` | Одинаковые гриды с фильтрами |
| `PageContainer` | Одинаковая обёртка страниц |
| `ConfirmDialog` | Одинаковые диалоги подтверждения |
| peerDependencies | Одна версия MUI, React, React Query во всех apps |
| `tsconfig.base.json` | Одинаковые настройки TypeScript |
| `eslint.config.js` (root) | Одинаковые правила линтинга |

Разработчик нового модуля **не может** случайно использовать другую тему или нарисовать sidebar иначе -- всё приходит из `@broker/ui-kit`.
