import { http, HttpResponse } from "msw";
import { buildUserProfile } from "../../factories";

const BASE = "/api/v1";

export const authHandlers = [
  http.get(`${BASE}/auth/me`, () =>
    HttpResponse.json(buildUserProfile()),
  ),

  http.post(`${BASE}/auth/login`, () =>
    HttpResponse.json({
      accessToken: "test-access-token",
      refreshToken: "test-refresh-token",
      expiresAt: new Date(Date.now() + 3600_000).toISOString(),
    }),
  ),

  http.post(`${BASE}/auth/refresh`, () =>
    HttpResponse.json({
      accessToken: "test-refreshed-access-token",
      refreshToken: "test-refreshed-refresh-token",
      expiresAt: new Date(Date.now() + 3600_000).toISOString(),
    }),
  ),
];
