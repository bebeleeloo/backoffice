import { http, HttpResponse } from "msw";
import { buildRoleDto, buildPagedResult } from "../../factories";

const BASE = "/api/v1";

const roles = [
  buildRoleDto({ id: "r1", name: "Admin", description: "Full access", isSystem: true, permissions: ["users.read", "roles.read"] }),
  buildRoleDto({ id: "r2", name: "Manager", description: "Manage users", isSystem: false, permissions: ["users.read"] }),
  buildRoleDto({ id: "r3", name: "Viewer", description: "Read only", isSystem: false, permissions: [] }),
];

export const rolesHandlers = [
  http.get(`${BASE}/roles`, () =>
    HttpResponse.json(buildPagedResult(roles)),
  ),

  http.post(`${BASE}/roles`, () =>
    HttpResponse.json(buildRoleDto(), { status: 201 }),
  ),

  http.put(`${BASE}/roles/:id`, () =>
    HttpResponse.json(buildRoleDto()),
  ),

  http.delete(`${BASE}/roles/:id`, () =>
    new HttpResponse(null, { status: 204 }),
  ),

  http.put(`${BASE}/roles/:id/permissions`, () =>
    HttpResponse.json(buildRoleDto()),
  ),
];
