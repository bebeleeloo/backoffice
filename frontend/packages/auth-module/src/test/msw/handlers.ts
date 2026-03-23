import { http, HttpResponse } from "msw";
import { buildUserDto, buildRoleDto, buildPagedResult, buildPermissionDto, buildUserProfile } from "../factories";

const BASE = "/api/v1";

const users = [
  buildUserDto({ id: "u1", username: "alice", email: "alice@test.com", fullName: "Alice Smith", isActive: true, roles: ["Admin"] }),
  buildUserDto({ id: "u2", username: "bob", email: "bob@test.com", fullName: "Bob Jones", isActive: true, roles: ["Manager"] }),
  buildUserDto({ id: "u3", username: "carol", email: "carol@test.com", fullName: "Carol White", isActive: false, roles: ["Viewer"] }),
];

const roles = [
  buildRoleDto({ id: "r1", name: "Admin", description: "Full access", isSystem: true, permissions: ["users.read", "roles.read"] }),
  buildRoleDto({ id: "r2", name: "Manager", description: "Manage users", isSystem: false, permissions: ["users.read"] }),
  buildRoleDto({ id: "r3", name: "Viewer", description: "Read only", isSystem: false, permissions: [] }),
];

const permissions = [
  buildPermissionDto({ id: "p1", code: "users.read", name: "Read Users", group: "Users" }),
  buildPermissionDto({ id: "p2", code: "users.create", name: "Create Users", group: "Users" }),
  buildPermissionDto({ id: "p3", code: "users.update", name: "Update Users", group: "Users" }),
  buildPermissionDto({ id: "p4", code: "users.delete", name: "Delete Users", group: "Users" }),
  buildPermissionDto({ id: "p5", code: "roles.read", name: "Read Roles", group: "Roles" }),
  buildPermissionDto({ id: "p6", code: "roles.create", name: "Create Roles", group: "Roles" }),
  buildPermissionDto({ id: "p7", code: "roles.update", name: "Update Roles", group: "Roles" }),
  buildPermissionDto({ id: "p8", code: "roles.delete", name: "Delete Roles", group: "Roles" }),
  buildPermissionDto({ id: "p9", code: "audit.read", name: "Read Audit", group: "Audit" }),
  buildPermissionDto({ id: "p10", code: "permissions.read", name: "Read Permissions", group: "Permissions" }),
];

export const handlers = [
  // Auth
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

  http.post(`${BASE}/auth/change-password`, () =>
    new HttpResponse(null, { status: 200 }),
  ),

  http.put(`${BASE}/auth/profile`, () =>
    HttpResponse.json(buildUserProfile()),
  ),

  http.put(`${BASE}/auth/photo`, () =>
    new HttpResponse(null, { status: 200 }),
  ),

  http.delete(`${BASE}/auth/photo`, () =>
    new HttpResponse(null, { status: 204 }),
  ),

  // Users
  http.get(`${BASE}/users`, () =>
    HttpResponse.json(buildPagedResult(users)),
  ),

  http.get(`${BASE}/users/:id`, ({ params }) => {
    const user = users.find((u) => u.id === params.id);
    return user ? HttpResponse.json(user) : new HttpResponse(null, { status: 404 });
  }),

  http.post(`${BASE}/users`, () =>
    HttpResponse.json(buildUserDto(), { status: 201 }),
  ),

  http.put(`${BASE}/users/:id`, () =>
    HttpResponse.json(buildUserDto()),
  ),

  http.delete(`${BASE}/users/:id`, () =>
    new HttpResponse(null, { status: 204 }),
  ),

  http.post(`${BASE}/users/:id/reset-password`, () =>
    new HttpResponse(null, { status: 200 }),
  ),

  // Roles
  http.get(`${BASE}/roles`, () =>
    HttpResponse.json(buildPagedResult(roles)),
  ),

  http.get(`${BASE}/roles/:id`, ({ params }) => {
    const role = roles.find((r) => r.id === params.id);
    return role ? HttpResponse.json(role) : new HttpResponse(null, { status: 404 });
  }),

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

  // Permissions
  http.get(`${BASE}/permissions`, () =>
    HttpResponse.json(permissions),
  ),

  // Entity changes (for EntityHistoryDialog)
  http.get(`${BASE}/entity-changes`, () =>
    HttpResponse.json(buildPagedResult([])),
  ),
];
