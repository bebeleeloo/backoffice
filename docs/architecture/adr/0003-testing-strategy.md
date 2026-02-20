# ADR-0003: Стратегия тестирования -- unit-only default suite

**Статус:** Принято
**Дата:** 2026-02-20

## Контекст

Frontend-тесты, включающие рендеринг полных страниц с MUI DataGrid в jsdom-окружении (Vitest), приводили к OOM (>4 GB V8 heap). Причина: каждый тестовый файл создаёт изолированный VM-контекст, а тяжёлые зависимости (@mui/material, @mui/x-data-grid, @mui/x-date-pickers) компилируются заново для каждого контекста.

## Решение

1. **Default suite (`npm test`) включает только лёгкие unit-тесты:** hooks, auth-хуки, утилитарные функции
2. **Include pattern:** `src/{hooks,auth,lib,utils}/**/*.test.{ts,tsx}`
3. **Тяжёлые page/integration тесты удалены** из репозитория
4. **setupTests.ts упрощён:** только jest-dom + cleanup + минимальные polyfills (без MSW, без DataGrid моков)

## Альтернативы рассмотренные и отклонённые

| Подход | Причина отказа |
|--------|---------------|
| Увеличение heap (`--max-old-space-size`) | Маскирует проблему, не решает |
| `isolate: false` | Ломает DOM-состояние между тестами |
| happy-dom вместо jsdom | Не решает OOM (проблема в V8, не в DOM) |
| Pool threads вместо forks | Не решает накопление модулей |

## Последствия

- `npm test` выполняется < 1 секунды, 8 тестов
- Page-level тесты не покрыты -- рекомендуется E2E (Playwright) для критических путей
- Тестовая инфраструктура (MSW handlers, factories, renderWithProviders) сохранена для будущего использования
