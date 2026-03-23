import { http, HttpResponse } from "msw";
import { buildPermissionDto } from "../../factories";

const BASE = "/api/v1";

const permissions = [
  buildPermissionDto({ code: "clients.read", name: "Read Clients", group: "clients" }),
  buildPermissionDto({ code: "clients.create", name: "Create Clients", group: "clients" }),
  buildPermissionDto({ code: "accounts.read", name: "Read Accounts", group: "accounts" }),
  buildPermissionDto({ code: "orders.read", name: "Read Orders", group: "orders" }),
  buildPermissionDto({ code: "audit.read", name: "Read Audit", group: "audit" }),
  buildPermissionDto({ code: "settings.manage", name: "Manage Settings", group: "settings" }),
];

export const permissionsHandlers = [
  http.get(`${BASE}/permissions`, () =>
    HttpResponse.json(permissions),
  ),
];
