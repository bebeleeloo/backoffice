import { http, HttpResponse } from "msw";
import { buildCountryList } from "../../factories";

const BASE = "/api/v1";

export const countriesHandlers = [
  http.get(`${BASE}/countries`, () =>
    HttpResponse.json(buildCountryList()),
  ),
];
