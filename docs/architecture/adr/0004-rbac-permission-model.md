# ADR-0004: Модель авторизации -- RBAC с permission overrides

**Статус:** Принято
**Дата:** 2026-02-17

## Контекст

Система требует гранулярного контроля доступа: разные операторы должны иметь разный набор прав.

## Решение

Трёхуровневая модель RBAC:

1. **Permissions** -- атомарные права (`users.read`, `clients.delete` и т.д.)
2. **Roles** -- наборы permissions (Administrator, Operator, Viewer и т.д.)
3. **User Permission Overrides** -- персональные исключения (allow/deny конкретного permission для конкретного пользователя)
4. **Data Scopes** -- задел для row-level security (scopeType + scopeValue)

### Вычисление эффективных прав

```
effective_permissions =
  (union of all role permissions)
  + (user overrides where IsAllowed = true)
  - (user overrides where IsAllowed = false)
```

### Реализация

- **Backend:** permissions зашиваются в JWT claims. Авторизация проверяется через `HasPermission` attribute + dynamic policy provider.
- **Frontend:** permissions доступны через `useHasPermission()` hook. UI-элементы скрываются при отсутствии нужного permission.

## Обоснование

- Roles упрощают массовое назначение прав
- Overrides позволяют тонкую настройку для отдельных пользователей
- Data Scopes подготавливают почву для row-level security

## Последствия

- При изменении прав роли все пользователи получат изменения при следующем refresh token
- Permissions кешируются в JWT -- изменения не моментальны (до 30 мин задержка)
- Data Scopes требуют дополнительной реализации в query layer
