import { http, HttpResponse } from "msw";
import { buildEntityConfig, buildEntityMetadataDto } from "../../factories";

const BASE = "/api/v1";

const entities = [
  buildEntityConfig({
    name: "Client",
    fields: [
      { name: "id", roles: ["*"] },
      { name: "displayName", roles: ["*"] },
      { name: "email", roles: ["Manager", "Operator"] },
      { name: "status", roles: ["*"] },
    ],
  }),
  buildEntityConfig({
    name: "Account",
    fields: [
      { name: "id", roles: ["*"] },
      { name: "accountNumber", roles: ["*"] },
      { name: "status", roles: ["Manager"] },
    ],
  }),
  buildEntityConfig({
    name: "Instrument",
    fields: [
      { name: "id", roles: ["*"] },
      { name: "symbol", roles: ["*"] },
    ],
  }),
];

const metadata = [
  buildEntityMetadataDto({ name: "Client", fields: ["id", "displayName", "email", "status", "clientType", "phone"] }),
  buildEntityMetadataDto({ name: "Account", fields: ["id", "accountNumber", "status", "clearerName"] }),
  buildEntityMetadataDto({ name: "Instrument", fields: ["id", "symbol", "instrumentType", "exchangeCode"] }),
];

export const entitiesHandlers = [
  http.get(`${BASE}/config/entities/raw`, () =>
    HttpResponse.json(entities),
  ),

  http.put(`${BASE}/config/entities`, () =>
    HttpResponse.json(null, { status: 200 }),
  ),

  http.get(`${BASE}/entity-metadata`, () =>
    HttpResponse.json(metadata),
  ),

  http.get(`${BASE}/auth/entity-metadata`, () =>
    HttpResponse.json([
      buildEntityMetadataDto({ name: "User", fields: ["id", "username", "email", "fullName"] }),
    ]),
  ),
];
