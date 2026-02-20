import { http, HttpResponse } from "msw";
import { buildUserDto, buildPagedResult } from "../../factories";

const BASE = "/api/v1";

const users = [
  buildUserDto({ id: "u1", username: "alice", email: "alice@test.com", fullName: "Alice Smith", isActive: true, roles: ["Admin"] }),
  buildUserDto({ id: "u2", username: "bob", email: "bob@test.com", fullName: "Bob Jones", isActive: true, roles: ["Manager"] }),
  buildUserDto({ id: "u3", username: "carol", email: "carol@test.com", fullName: "Carol White", isActive: false, roles: ["Viewer"] }),
];

export const usersHandlers = [
  http.get(`${BASE}/users`, () =>
    HttpResponse.json(buildPagedResult(users)),
  ),

  http.post(`${BASE}/users`, () =>
    HttpResponse.json(buildUserDto(), { status: 201 }),
  ),

  http.put(`${BASE}/users/:id`, () =>
    HttpResponse.json(buildUserDto()),
  ),

  http.delete(`${BASE}/users/:id`, () =>
    new HttpResponse(null, { status: 204 }),
  ),
];
