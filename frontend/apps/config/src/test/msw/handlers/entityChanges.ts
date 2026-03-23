import { http, HttpResponse } from "msw";

const BASE = "/api/v1";

export const entityChangesHandlers = [
  http.get(`${BASE}/entity-changes`, () =>
    HttpResponse.json({
      items: [],
      totalCount: 0,
      page: 1,
      pageSize: 25,
      totalPages: 0,
    }),
  ),
];
