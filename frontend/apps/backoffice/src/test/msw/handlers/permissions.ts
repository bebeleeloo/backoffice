import { http, HttpResponse } from "msw";
import type { PermissionDto } from "@/api/types";

const BASE = "/api/v1";

const permissions: PermissionDto[] = [
  { id: "p1", code: "users.read", name: "Read Users", description: null, group: "Users" },
  { id: "p2", code: "users.create", name: "Create Users", description: null, group: "Users" },
  { id: "p3", code: "users.update", name: "Update Users", description: null, group: "Users" },
  { id: "p4", code: "users.delete", name: "Delete Users", description: null, group: "Users" },
  { id: "p5", code: "roles.read", name: "Read Roles", description: null, group: "Roles" },
  { id: "p6", code: "roles.create", name: "Create Roles", description: null, group: "Roles" },
  { id: "p7", code: "roles.update", name: "Update Roles", description: null, group: "Roles" },
  { id: "p8", code: "roles.delete", name: "Delete Roles", description: null, group: "Roles" },
  { id: "p9", code: "clients.read", name: "Read Clients", description: null, group: "Clients" },
  { id: "p10", code: "clients.create", name: "Create Clients", description: null, group: "Clients" },
  { id: "p11", code: "clients.update", name: "Update Clients", description: null, group: "Clients" },
  { id: "p12", code: "clients.delete", name: "Delete Clients", description: null, group: "Clients" },
  { id: "p13", code: "audit.read", name: "Read Audit", description: null, group: "Audit" },
  { id: "p14", code: "permissions.read", name: "Read Permissions", description: null, group: "Permissions" },
];

export const permissionsHandlers = [
  http.get(`${BASE}/permissions`, () =>
    HttpResponse.json(permissions),
  ),
];
