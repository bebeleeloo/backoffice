# 07. Стратегия тестирования

## Текущее состояние

### Frontend

**Фреймворк:** Vitest 2.x + React Testing Library 16 + jsdom

**Default suite (`npm test`):** только быстрые unit-тесты

| Файл | Тестов | Что тестирует |
|------|--------|---------------|
| `src/hooks/useDebounce.test.tsx` | 5 | Хук debounce: задержка, сброс таймера, custom delay |
| `src/auth/usePermission.test.ts` | 3 | Хук useHasPermission: наличие/отсутствие permission |
| **Итого** | **8** | **~600ms** |

**Include pattern:** `src/{hooks,auth,lib,utils}/**/*.test.{ts,tsx}`

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
- `Broker.Backoffice.Tests.Unit` -- unit-тесты
- `Broker.Backoffice.Tests.Integration` -- интеграционные тесты

> Содержимое backend-тестов не анализировалось детально в рамках данного аудита.

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
   - CRUD клиента
   - Назначение permissions роли

4. **Рассмотреть API-тесты** (Supertest / dotnet integration tests) для проверки backend без UI

### Долгосрочные (60-90 дней)

5. **Contract testing** (Pact) между frontend и backend
6. **Visual regression** (Chromatic / Percy) для UI-компонентов
