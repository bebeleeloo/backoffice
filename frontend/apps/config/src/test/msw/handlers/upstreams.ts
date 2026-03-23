import { http, HttpResponse } from "msw";

const BASE = "/api/v1";

const upstreams = {
  "core-api": {
    address: "http://api:8080",
    routes: ["/api/v1/clients", "/api/v1/accounts", "/api/v1/instruments"],
  },
  "auth-service": {
    address: "http://auth:8082",
    routes: ["/api/v1/auth", "/api/v1/users", "/api/v1/roles"],
  },
};

export const upstreamsHandlers = [
  http.get(`${BASE}/config/upstreams`, () =>
    HttpResponse.json(upstreams),
  ),

  http.put(`${BASE}/config/upstreams`, () =>
    HttpResponse.json(null, { status: 200 }),
  ),
];
