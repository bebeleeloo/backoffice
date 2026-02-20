# ADR-0002: Аутентификация -- JWT с ротацией refresh-токенов

**Статус:** Принято
**Дата:** 2026-02-17

## Контекст

Нужен механизм аутентификации для SPA-фронтенда, работающего с REST API. Рассмотрены варианты: session cookies, JWT, OAuth2.

## Решение

JWT Bearer tokens с ротацией refresh-токенов:
- Access token (30 мин) содержит permissions как claims
- Refresh token (7 дней) хранится в БД в виде SHA256-хэша
- При обновлении старый refresh token отзывается, выдаётся новый (rotation)
- Reuse detection: повторное использование отозванного токена отзывает ВСЕ токены пользователя

Токены хранятся в `localStorage` на frontend.

## Обоснование

- JWT позволяет stateless-проверку авторизации (permissions в claims)
- Ротация refresh-токенов защищает от кражи (короткое окно эксплуатации)
- Reuse detection обнаруживает компрометацию token chain

## Ограничения

- `localStorage` уязвим к XSS-атакам (в отличие от httpOnly cookies)
- В production рекомендуется перейти на httpOnly cookies с SameSite=Strict

## Последствия

- Frontend должен реализовать автоматическое обновление токена при 401
- Backend хранит refresh-токены в БД (дополнительная нагрузка при каждом refresh)
- При масштабировании: секрет JWT должен быть одинаковым на всех инстансах
