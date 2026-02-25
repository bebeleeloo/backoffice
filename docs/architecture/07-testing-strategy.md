# 07. Стратегия тестирования

## Текущее состояние

### Frontend

**Фреймворк:** Vitest 2.x + React Testing Library 16 + jsdom

**Default suite (`npm test`):** быстрые unit-тесты и regression-тесты

| Файл | Тестов | Что тестирует |
|------|--------|---------------|
| `src/hooks/useDebounce.test.tsx` | 5 | Хук debounce: задержка, сброс таймера, custom delay |
| `src/hooks/useConfirm.test.ts` | 5 | Хук useConfirm: promise-based подтверждение |
| `src/auth/usePermission.test.ts` | 3 | Хук useHasPermission: наличие/отсутствие permission |
| `src/utils/validateFields.test.ts` | 17 | Валидаторы: validateRequired, validateEmail |
| `src/utils/extractErrorMessage.test.ts` | 10 | Парсинг ошибок: Axios, ProblemDetails, fallback |
| `src/test/edit-dialogs.test.tsx` | 4 | Regression: Edit-диалоги заполняют форму при закешированных данных |
| **Итого** | **44** | **~2.8s** |

**Include pattern:** `src/{hooks,auth,lib,utils,test}/**/*.test.{ts,tsx}`

**setupTests.ts** содержит только:
- `@testing-library/jest-dom/vitest` (матчеры)
- `cleanup()` в afterEach
- Минимальные jsdom-полифилы (matchMedia, scrollTo, confirm, crypto.randomUUID)

### Что было исключено и почему

Ранее в репозитории были тяжёлые page/integration тесты, которые вызывали **OOM** (>4 GB V8 heap) из-за:
- MUI DataGrid рендеринга в jsdom (виртуализация, layout measurements)
- Накопления памяти при загрузке тяжёлых модулей (@mui/material, @mui/x-data-grid, @mui/x-date-pickers) в изолированных VM-контекстах vitest

**Удалённые тесты:**
- `pages/UsersPage.test.tsx` -- полная страница с MSW, FilteredDataGrid, диалогами
- `pages/RolesPage.test.tsx` -- аналогично
- `pages/ClientsPage.test.tsx` -- аналогично
- `pages/AuditPage.test.tsx` -- аналогично
- `layouts/MainLayout.test.tsx` -- полный layout с роутингом
- `components/PageContainer.test.tsx` -- компонент с ThemeProvider
- `components/grid/GridFilterRow.test.tsx` -- inline фильтры
- `auth/RequireAuth.test.tsx` -- auth guard с рендерингом

### Инфраструктура тестирования (сохранена для будущего использования)

| Утилита | Путь | Назначение |
|---------|------|------------|
| renderWithProviders | `src/test/renderWithProviders.tsx` | Полный набор провайдеров (QueryClient, Theme, Auth, Router) |
| Factories | `src/test/factories/` | Билдеры тестовых данных (faker.js) |
| MSW handlers | `src/test/msw/` | Моки API-эндпоинтов |
| FilteredDataGrid mock | `src/test/mocks/FilteredDataGrid.tsx` | Лёгкий стаб для DataGrid |

### Backend

**Проекты тестов:**
- `Broker.Backoffice.Tests.Unit` -- xUnit + FluentAssertions
- `Broker.Backoffice.Tests.Integration` -- WebApplicationFactory + Testcontainers (SQL Server)

#### Unit-тесты (22 теста, ~34ms)

| Файл | Тестов | Что тестирует |
|------|--------|---------------|
| `DateTimeProviderTests.cs` | 2 | Провайдер текущего времени |
| `CorrelationIdAccessorTests.cs` | 3 | Accessor для correlation ID |
| `PermissionsTests.cs` | 4 | Наличие и уникальность permissions |
| `CreateAccountValidatorTests.cs` | 6 | Валидатор создания счёта (Number, Comment, ExternalId) |
| `CreateClientValidatorTests.cs` | 7 | Валидатор создания клиента (Email, Phone, ExternalId, Ssn, Address) |

#### Интеграционные тесты (33 теста, ~2s)

| Файл | Тестов | Что тестирует |
|------|--------|---------------|
| `HealthCheckTests.cs` | 2 | /health/live, /health/ready |
| `SwaggerTests.cs` | 1 | Доступность Swagger UI |
| `AuthTests.cs` | 5 | Логин, refresh, /me, невалидные credentials |
| `UsersTests.cs` | 6 | CRUD пользователей, дубликат username, аудит |
| `RolesTests.cs` | 3 | Список ролей, создание/удаление, защита системных |
| `AccountsTests.cs` | 8 | CRUD счетов, дубликат номера, невалидные данные |
| `ClientsTests.cs` | 8 | CRUD клиентов (Individual/Corporate), невалидный email, без адресов |

**Инфраструктура:** все интеграционные тесты используют общий `[Collection("Integration")]` с `ICollectionFixture<CustomWebApplicationFactory>`, что обеспечивает единый контейнер SQL Server на все тест-классы. Требуется Docker и .NET 8 SDK.

## Рекомендации

### Краткосрочные (0-30 дней)

1. **Добавить unit-тесты** для оставшихся хуков/утилит:
   - `src/api/hooks.ts` -- моки axios, проверка формирования params
   - `src/auth/useAuth.ts` -- логин/логаут логика
   - Любые утилитарные функции (cleanParams, форматирование)

2. **Запустить backend-тесты** в CI pipeline (`dotnet test`)

### Среднесрочные (30-60 дней)

3. **Рассмотреть Playwright/Cypress** для E2E-тестов критических путей:
   - Login flow
   - CRUD клиента / счёта
   - Назначение permissions роли

### Долгосрочные (60-90 дней)

4. **Contract testing** (Pact) между frontend и backend
5. **Visual regression** (Chromatic / Percy) для UI-компонентов
