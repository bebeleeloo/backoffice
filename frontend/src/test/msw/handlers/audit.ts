import { http, HttpResponse } from "msw";
import { buildAuditLogDto, buildPagedResult } from "../../factories";

const BASE = "/api/v1";

const auditLogs = [
  buildAuditLogDto({ id: "a1", userName: "alice", action: "Create", entityType: "User", method: "POST", path: "/api/v1/users", statusCode: 201, isSuccess: true }),
  buildAuditLogDto({ id: "a2", userName: "bob", action: "Update", entityType: "Role", method: "PUT", path: "/api/v1/roles/r1", statusCode: 200, isSuccess: true }),
  buildAuditLogDto({ id: "a3", userName: "carol", action: "Delete", entityType: "Client", method: "DELETE", path: "/api/v1/clients/c1", statusCode: 500, isSuccess: false }),
];

export const auditHandlers = [
  http.get(`${BASE}/audit`, () =>
    HttpResponse.json(buildPagedResult(auditLogs)),
  ),

  http.get(`${BASE}/audit/:id`, ({ params }) => {
    const log = auditLogs.find((l) => l.id === params.id);
    if (!log) return new HttpResponse(null, { status: 404 });
    return HttpResponse.json(log);
  }),
];
