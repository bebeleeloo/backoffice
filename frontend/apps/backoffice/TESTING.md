# Frontend Testing

## Stack

- **Vitest** — test runner (jsdom environment)
- **React Testing Library** — DOM queries and rendering
- **MSW v2** — API mocking at network level
- **@faker-js/faker** — test data generation

## Commands

```bash
npm test          # watch mode
npm run test:ci   # single run + coverage report
npm run build:ci  # test:ci → build (CI pipeline)
```

## Structure

```
src/test/
  setupTests.ts              # jest-dom, polyfills, MSW lifecycle
  renderWithProviders.tsx     # all providers (QueryClient, Theme, Auth, Router)
  factories/                  # data builders (buildUserDto, buildRoleDto, etc.)
  msw/
    server.ts                 # setupServer
    handlers.ts               # barrel import
    handlers/                 # per-resource handlers (auth, users, roles, etc.)
```

## MSW Handlers

Base URL: `/api/v1` (matches `apiClient.baseURL`).

Default handlers return small datasets (3 items). Each handler covers the endpoints
used by the corresponding page. All handlers are loaded by default via `setupTests.ts`.

To override a handler in a test (e.g. to spy on request params):

```ts
import { server } from "@/test/msw/server";

server.use(
  http.get("/api/v1/users", ({ request }) => {
    spy(Object.fromEntries(new URL(request.url).searchParams));
    return HttpResponse.json(buildPagedResult(users));
  }),
);
```

Unhandled requests fail with `onUnhandledRequest: "error"`.

## Adding a New Test

1. If your page hits a new API endpoint, add a handler in `src/test/msw/handlers/`
   and register it in `handlers.ts`.
2. Create a factory in `src/test/factories/` if you need new data shapes.
3. Use `renderWithProviders` — it sets up QueryClient, Theme, Auth, and Router.
4. For filter/debounce tests, use `vi.useFakeTimers({ shouldAdvanceTime: true })`
   and `vi.advanceTimersByTime(300)` after typing.

## Anti-Flake Rules

- `vi.useFakeTimers({ shouldAdvanceTime: true })` for debounce tests
- QueryClient with `retry: false`, `gcTime: 0`
- `server.resetHandlers()` + `localStorage.clear()` in afterEach (automatic via setupTests)
- Small datasets (3 items) to avoid DataGrid virtualization
- `await waitFor()` for all async assertions
- `restoreMocks: true` + `mockReset: true` + `clearMocks: true` in vitest config
