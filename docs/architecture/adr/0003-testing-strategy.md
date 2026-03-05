# ADR-0003: Стратегия тестирования -- smoke-тесты вместо полного рендеринга

**Статус:** Принято (обновлено 2026-03-03)
**Дата:** 2026-02-20

## Контекст

Frontend-тесты, включающие рендеринг полных страниц с MUI DataGrid в jsdom-окружении (Vitest), приводили к OOM (>4 GB V8 heap). Причина: каждый тестовый файл создаёт изолированный VM-контекст, а тяжёлые зависимости (@mui/material, @mui/x-data-grid, @mui/x-date-pickers) компилируются заново для каждого контекста.

## Решение

1. **Default suite (`npm test`) включает unit-тесты и лёгкие smoke-тесты страниц**
2. **Include pattern:** `src/{hooks,auth,lib,utils,test}/**/*.test.{ts,tsx}`
3. **Тяжёлые page-тесты с полным рендерингом DataGrid удалены** — заменены smoke-тестами, которые проверяют структуру страницы (заголовок, кнопки, permission gating) без рендеринга DataGrid с данными
4. **setupTests.ts:** jest-dom + cleanup + минимальные polyfills (matchMedia, scrollTo, confirm, crypto.randomUUID)
5. **Тестовая инфраструктура активно используется:** MSW handlers, factories, renderWithProviders

## Альтернативы рассмотренные и отклонённые

| Подход | Причина отказа |
|--------|---------------|
| Увеличение heap (`--max-old-space-size`) | Маскирует проблему, не решает |
| `isolate: false` | Ломает DOM-состояние между тестами |
| happy-dom вместо jsdom | Не решает OOM (проблема в V8, не в DOM) |
| Pool threads вместо forks | Не решает накопление модулей |

## Последствия

- `npm test` выполняется за ~6 секунд, 119 frontend-тестов (12 файлов)
- Все 9 list-страниц покрыты smoke-тестами (title, search bar, create button permission, export)
- Компоненты (ConfirmDialog, ErrorBoundary, UserAvatar, PageContainer) покрыты unit-тестами
- Для E2E критических путей рекомендуется Playwright
