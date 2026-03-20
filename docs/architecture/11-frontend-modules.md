# 11. Модульная архитектура фронтенда

## Назначение

Модульная архитектура разделяет фронтенд на **независимые пакеты** (UI-модули) и **3 отдельных SPA-приложения**, которые:

1. **Разрабатываются независимо** -- разные команды работают над разными модулями в одном монорепозитории
2. **Деплоятся вместе** -- один `Dockerfile.web` собирает все 3 SPA, nginx раздаёт по path
3. **Переиспользуются между проектами** -- auth-модуль подключается к любому проекту как npm-зависимость
4. **Сохраняют единый стиль** -- общая дизайн-система, тема, компоненты через `@broker/ui-kit`
5. **Управляются через Config Service** -- sidebar, видимость пунктов меню, доступы -- приходят с сервера

### Зачем нужно

| Проблема | Решение |
|----------|---------|
| Весь фронтенд -- один бандл, деплоится целиком | 3 SPA (backoffice, auth, config), каждое -- отдельный Vite-бандл |
| Auth UI нужен на соседнем проекте | `@broker/auth-module` подключается как npm-зависимость |
| Sidebar захардкожен, для скрытия пункта нужен деплой | Sidebar конфигурируется через Config Service, по ролям |
| Разные команды конфликтуют в одной кодовой базе | Изолированные пакеты в monorepo, чёткие границы |
| Новый проект -- заново создавать тему, компоненты, авторизацию | `@broker/ui-kit` -- подключил и получил всё |

---

## Реализованная архитектура

```
                    ┌─────────────────────────────────────────┐
                    │          pnpm monorepo + Turborepo       │
                    │                                         │
  ┌─────────────────┤  packages/ (shared)                     │
  │                 │  ├── ui-kit/          @broker/ui-kit     │
  │                 │  └── auth-module/     @broker/auth-module│
  │                 │                                         │
  │                 │  apps/ (3 SPA)                           │
  │                 │  ├── backoffice/      Бизнес-страницы    │
  │                 │  ├── auth/            Login + Users + Roles│
  │                 │  └── config/          Config Admin UI    │
  │                 └─────────────────────────────────────────┘
  │
  │                        nginx
  │                          │
  │          ┌───────────────┼───────────────┐
  │          │               │               │
  │    /login,/users,   /config/*      / (всё остальное)
  │    /roles → auth     → config      → backoffice
  │       SPA              SPA              SPA
  │
  │  Все 3 SPA делят один localStorage (один origin)
  └──────────────────────────────────────────────────
```

Каждое SPA -- самостоятельное React-приложение со своим `main.tsx`, `router/index.tsx` и Vite-конфигом. Общая инфраструктура (тема, компоненты, auth, API client) приходит из `@broker/ui-kit`.

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
│   │   │   │   ├── NavigationProvider.tsx # Контекст кросс-SPA навигации
│   │   │   │   └── index.ts
│   │   │   ├── theme/
│   │   │   │   ├── index.ts             # createAppTheme, SIDEBAR_COLORS, STAT_GRADIENTS
│   │   │   │   └── ThemeContext.tsx      # AppThemeProvider, useThemeMode, useListTheme
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
│   │   │   ├── types.ts                 # MenuItem, общие типы
│   │   │   └── index.ts                 # Главный реэкспорт
│   │   ├── package.json
│   │   └── tsconfig.json
│   │
│   └── auth-module/                     # @broker/auth-module
│       ├── src/
│       │   ├── pages/
│       │   │   ├── LoginPage.tsx
│       │   │   ├── UsersPage.tsx
│       │   │   ├── UserDetailsPage.tsx
│       │   │   ├── UserDialogs.tsx
│       │   │   ├── RolesPage.tsx
│       │   │   ├── RoleDetailsPage.tsx
│       │   │   ├── RoleDialogs.tsx
│       │   │   └── ProfileTab.tsx
│       │   ├── api/
│       │   │   ├── types.ts             # UserDto, RoleDto, PermissionDto
│       │   │   └── hooks.ts            # useUsers, useRoles, useLogin, useMe...
│       │   ├── routes.ts               # Экспорт RouteObject[]
│       │   └── index.ts                # Экспорт модуля
│       ├── package.json
│       └── tsconfig.json
│
├── apps/
│   ├── backoffice/                      # Бизнес-страницы SPA
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
│   │   │   ├── router/
│   │   │   │   └── index.tsx           # Роутер с NavigationProvider
│   │   │   └── main.tsx                # Точка входа
│   │   ├── vite.config.ts
│   │   └── package.json
│   │
│   ├── auth/                            # Login + Users + Roles SPA
│   │   ├── src/
│   │   │   ├── router/
│   │   │   │   └── index.tsx           # Роутер с NavigationProvider
│   │   │   └── main.tsx
│   │   ├── vite.config.ts
│   │   └── package.json
│   │
│   └── config/                          # Config Admin SPA
│       ├── src/
│       │   ├── pages/
│       │   │   ├── MenuEditorPage.tsx
│       │   │   ├── EntityFieldsPage.tsx
│       │   │   └── UpstreamsPage.tsx
│       │   ├── router/
│       │   │   └── index.tsx
│       │   └── main.tsx
│       ├── vite.config.ts
│       └── package.json
│
├── pnpm-workspace.yaml
├── turbo.json
├── nginx.conf                           # Shared nginx: роутинг 3 SPA
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
| Layout | `MainLayout`, `SidebarItem`, `NavigationProvider`, `useAppNavigation` |
| Тема | `createAppTheme`, `createAppListTheme`, `AppThemeProvider`, `useThemeMode`, `useListTheme`, `SIDEBAR_COLORS`, `STAT_GRADIENTS` |
| Auth | `AuthProvider`, `useAuth`, `useHasPermission`, `RequireAuth` |
| API | `apiClient` (Axios), `useMenu`, `useEntityConfig` |
| Хуки | `useDebounce`, `useConfirm` |
| Утилиты | `exportToExcel`, `extractErrorMessage`, `validateRequired`, `validateEmail` |
| Типы | `MenuItem`, `PagedResult`, `FieldErrors` |
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

---

## Архитектура SPA-приложений

### Каждое SPA -- самостоятельное React-приложение

Вместо единого `createApp()` с `ModuleDefinition`, каждое SPA имеет свой собственный `main.tsx` с идентичным стеком провайдеров и свой `router/index.tsx`.

```typescript
// apps/backoffice/src/main.tsx (аналогично для auth и config)
import { createRoot } from 'react-dom/client'
import { AppThemeProvider, AuthProvider } from '@broker/ui-kit'
import { QueryClientProvider } from '@tanstack/react-query'
import { RouterProvider } from 'react-router-dom'
import { SnackbarProvider } from 'notistack'
import { router } from './router'

const root = createRoot(document.getElementById('root')!)
root.render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <AppThemeProvider>
        <SnackbarProvider maxSnack={3} anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}>
          <AuthProvider>
            <RouterProvider router={router} />
          </AuthProvider>
        </SnackbarProvider>
      </AppThemeProvider>
    </QueryClientProvider>
  </StrictMode>
)
```

Каждое SPA оборачивает `MainLayout` в `NavigationProvider` со списком своих внутренних путей:

```typescript
// apps/backoffice/src/router/index.tsx
import { NavigationProvider, RequireAuth, MainLayout } from '@broker/ui-kit'

const internalPaths = [
  '/', '/clients', '/accounts', '/instruments',
  '/trade-orders', '/non-trade-orders',
  '/trade-transactions', '/non-trade-transactions',
  '/audit', '/settings',
]

export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  {
    path: '/',
    element: (
      <RequireAuth>
        <NavigationProvider internalPaths={internalPaths}>
          <MainLayout />
        </NavigationProvider>
      </RequireAuth>
    ),
    children: [
      { index: true, lazy: () => import('../pages/DashboardPage') },
      { path: 'clients', lazy: () => import('../pages/ClientsPage') },
      // ... остальные бизнес-роуты
    ],
  },
])
```

```typescript
// apps/auth/src/router/index.tsx
const internalPaths = ['/login', '/users', '/roles']

export const router = createBrowserRouter([
  { path: '/login', element: <LoginPage /> },
  {
    path: '/',
    element: (
      <RequireAuth>
        <NavigationProvider internalPaths={internalPaths}>
          <MainLayout />
        </NavigationProvider>
      </RequireAuth>
    ),
    children: [
      { path: 'users', lazy: () => import('../pages/UsersPage') },
      { path: 'users/:id', lazy: () => import('../pages/UserDetailsPage') },
      { path: 'roles', lazy: () => import('../pages/RolesPage') },
      { path: 'roles/:id', lazy: () => import('../pages/RoleDetailsPage') },
    ],
  },
])
```

---

## Кросс-SPA навигация

### Проблема

3 SPA работают на одном домене, но каждое -- отдельное React-приложение со своим React Router. Переход между SPA (например, из backoffice в `/users`) невозможен через `react-router-dom` `navigate()` -- нужна полная перезагрузка страницы.

### Решение: NavigationProvider

`NavigationProvider` в `@broker/ui-kit` предоставляет контекст навигации. Каждое SPA передаёт массив `internalPaths` -- пути, которые это SPA обрабатывает.

```typescript
// packages/ui-kit/src/layouts/NavigationProvider.tsx
interface NavigationContextValue {
  navigateTo: (path: string) => void
  isInternalPath: (path: string) => boolean
}

export function NavigationProvider({ internalPaths, children }) {
  const navigate = useNavigate()

  const navigateTo = useCallback((path: string) => {
    // Проверяем, обрабатывается ли путь текущим SPA
    const isInternal = internalPaths.some(p =>
      path === p || path.startsWith(p + '/')
    )

    if (isInternal) {
      navigate(path)         // React Router — без перезагрузки
    } else {
      window.location.href = path  // Полная навигация — другое SPA
    }
  }, [internalPaths, navigate])

  return (
    <NavigationContext.Provider value={{ navigateTo, isInternalPath }}>
      {children}
    </NavigationContext.Provider>
  )
}

export const useAppNavigation = () => useContext(NavigationContext)
```

### Где используется

| Место | Навигация |
|-------|-----------|
| Sidebar (все пункты меню) | `navigateTo(item.path)` -- автоматически выбирает React Router или window.location |
| Logout | `window.location.href = '/login'` -- всегда полная навигация |
| RequireAuth (неавторизованный) | `window.location.href = '/login?returnTo=...'` |
| Ссылки внутри страниц | `<Link to="...">` (React Router) для внутренних ссылок |

### Общий localStorage

Все 3 SPA работают на одном origin (`localhost:3000` или production domain), поэтому делят один `localStorage`:
- `accessToken`, `refreshToken` -- auth-токены
- `themeMode` -- тема (light/dark/system)
- `sidebarCollapsed` -- состояние sidebar

Логин в auth SPA → токен доступен во всех SPA. Logout → удаление токена → все SPA становятся неавторизованными.

---

## Динамический Sidebar

### Конфигурация меню через Config Service

Sidebar **не содержит** захардкоженных пунктов меню. Структура, видимость и порядок определяются в YAML-конфиге на стороне Config Service (встроен в API Gateway) и отдаются по API в зависимости от роли пользователя.

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

Config Service (встроен в API Gateway) читает YAML, фильтрует пункты по permissions из JWT-токена текущего пользователя и возвращает только разрешённые:

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
import { useAppNavigation } from './NavigationProvider'
import { SIDEBAR_COLORS } from '../theme'

export function MainLayout() {
  const { data: menuItems = [], isLoading } = useMenu()
  const { navigateTo } = useAppNavigation()
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
            onNavigate={navigateTo}
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

## Конфигурация для разных проектов (будущее)

Архитектура позволяет подключать `@broker/ui-kit` и `@broker/auth-module` к другим проектам как npm-зависимости. Для этого нужен собственный Config Service с другим `menu.yaml`.

Пример: соседний проект, которому нужно только управление пользователями:

```typescript
// other-project/src/main.tsx
import { AppThemeProvider, AuthProvider, MainLayout, NavigationProvider } from '@broker/ui-kit'
import { authRoutes } from '@broker/auth-module'

// Подключить auth-роуты + свои собственные
```

Каждый проект работает с **своим экземпляром Config Service** (или одним Config Service с разными конфигами по идентификатору проекта). Эта возможность запланирована на будущее.

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

### Единый Dockerfile.web

Все 3 SPA собираются одним `Dockerfile.web`. nginx раздаёт по path:

```dockerfile
# Dockerfile.web (упрощённо)
FROM node:20-alpine AS deps
WORKDIR /app
RUN corepack enable && corepack prepare pnpm@latest --activate
COPY frontend/pnpm-workspace.yaml frontend/pnpm-lock.yaml frontend/package.json ./
COPY frontend/packages/ packages/
COPY frontend/apps/ apps/
RUN pnpm install --frozen-lockfile

FROM node:20-alpine AS build
WORKDIR /app
RUN corepack enable && corepack prepare pnpm@latest --activate
COPY --from=deps /app/ ./
COPY frontend/ .
RUN pnpm turbo build

FROM nginx:alpine
COPY frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/apps/backoffice/dist /usr/share/nginx/html/backoffice
COPY --from=build /app/apps/auth/dist /usr/share/nginx/html/auth
COPY --from=build /app/apps/config/dist /usr/share/nginx/html/config
RUN chown -R nginx:nginx /usr/share/nginx/html
USER nginx
EXPOSE 8080
```

### nginx роутинг 3 SPA

```nginx
# frontend/nginx.conf (ключевые location-блоки)

# Auth SPA: login, users, roles
location ~ ^/(login|users|roles) {
    root /usr/share/nginx/html/auth;
    try_files $uri /index.html;
}

# Config SPA: /config/*
location /config {
    root /usr/share/nginx/html/config;
    try_files $uri /index.html;
}

# API proxy → Gateway
location /api/ {
    proxy_pass http://broker-gateway:8090;
}

# Backoffice SPA: всё остальное (default)
location / {
    root /usr/share/nginx/html/backoffice;
    try_files $uri /index.html;
}
```

### Docker Compose

```yaml
# docker-compose.yml
services:
  web:
    build:
      context: .
      dockerfile: Dockerfile.web
    container_name: broker-web
    restart: unless-stopped
    ports:
      - "3000:8080"
    depends_on:
      gateway:
        condition: service_healthy
```

Один контейнер `broker-web` обслуживает все 3 SPA.

---

## Разработка

### Локальный запуск

```bash
cd frontend

# Запустить только backoffice (+ зависимости ui-kit, auth-module)
pnpm turbo dev --filter=@broker/backoffice

# Запустить только auth
pnpm turbo dev --filter=@broker/auth

# Запустить все apps параллельно
pnpm turbo dev
```

### Добавление нового SPA

1. Создать приложение:
```bash
mkdir -p apps/new-app/src/{pages,router}
```

2. `apps/new-app/package.json`:
```json
{
  "name": "@broker/new-app",
  "version": "0.0.1",
  "private": true,
  "dependencies": {
    "@broker/ui-kit": "workspace:*"
  }
}
```

3. Создать `main.tsx` с провайдерами (скопировать из существующего app).

4. Создать `router/index.tsx` с `NavigationProvider`:
```typescript
import { NavigationProvider, RequireAuth, MainLayout } from '@broker/ui-kit'

const internalPaths = ['/new-feature', '/new-other']

export const router = createBrowserRouter([
  {
    path: '/',
    element: (
      <RequireAuth>
        <NavigationProvider internalPaths={internalPaths}>
          <MainLayout />
        </NavigationProvider>
      </RequireAuth>
    ),
    children: [
      { path: 'new-feature', lazy: () => import('../pages/NewFeaturePage') },
    ],
  },
])
```

5. Добавить location-блок в `nginx.conf`:
```nginx
location /new-feature {
    root /usr/share/nginx/html/new-app;
    try_files $uri /index.html;
}
```

6. Добавить COPY в `Dockerfile.web`:
```dockerfile
COPY --from=build /app/apps/new-app/dist /usr/share/nginx/html/new-app
```

7. Добавить пункт в `menu.yaml` Config Service:
```yaml
  - id: new-feature
    label: New Feature
    icon: Star
    path: /new-feature
    module: new-app
    permissions: [new-feature.read]
```

### Тестирование

Каждый пакет имеет свои тесты:

```bash
# Тесты ui-kit
pnpm turbo test --filter=@broker/ui-kit

# Тесты конкретного app
pnpm turbo test --filter=@broker/backoffice

# Все тесты
pnpm turbo test
```

Стек тестирования единый: Vitest + React Testing Library + MSW. Общие тестовые утилиты (`renderWithProviders`, MSW handlers) выносятся в `packages/ui-kit/test/` или в отдельный пакет `packages/test-utils/`.

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
| `NavigationProvider` | Бесшовная навигация между SPA |
| `FilteredDataGrid` | Одинаковые гриды с фильтрами |
| `PageContainer` | Одинаковая обёртка страниц |
| `ConfirmDialog` | Одинаковые диалоги подтверждения |
| peerDependencies | Одна версия MUI, React, React Query во всех apps |
| `tsconfig.base.json` | Одинаковые настройки TypeScript |
| `eslint.config.js` (root) | Одинаковые правила линтинга |

Разработчик нового модуля **не может** случайно использовать другую тему или нарисовать sidebar иначе -- всё приходит из `@broker/ui-kit`.
