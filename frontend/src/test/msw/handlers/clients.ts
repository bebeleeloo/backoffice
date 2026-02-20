import { http, HttpResponse } from "msw";
import { buildClientListItemDto, buildPagedResult } from "../../factories";

const BASE = "/api/v1";

const clients = [
  buildClientListItemDto({ id: "c1", displayName: "John Doe", email: "john@test.com", status: "Active", clientType: "Individual" }),
  buildClientListItemDto({ id: "c2", displayName: "Acme Corp", email: "acme@test.com", status: "Blocked", clientType: "Corporate" }),
  buildClientListItemDto({ id: "c3", displayName: "Jane Roe", email: "jane@test.com", status: "PendingKyc", clientType: "Individual" }),
];

export const clientsHandlers = [
  http.get(`${BASE}/clients`, () =>
    HttpResponse.json(buildPagedResult(clients)),
  ),

  http.post(`${BASE}/clients`, () =>
    HttpResponse.json({ id: "c-new" }, { status: 201 }),
  ),

  http.put(`${BASE}/clients/:id`, () =>
    HttpResponse.json({ id: "c-updated" }),
  ),

  http.delete(`${BASE}/clients/:id`, () =>
    new HttpResponse(null, { status: 204 }),
  ),
];
