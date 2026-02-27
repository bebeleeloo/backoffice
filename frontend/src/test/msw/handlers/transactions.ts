import { http, HttpResponse } from "msw";
import {
  buildTradeTransactionListItemDto,
  buildNonTradeTransactionListItemDto,
  buildPagedResult,
} from "../../factories";

const BASE = "/api/v1";

const tradeTransactions = [
  buildTradeTransactionListItemDto({ id: "tt1", transactionNumber: "TT-20260101-ABCD1234", status: "Settled", side: "Buy" }),
  buildTradeTransactionListItemDto({ id: "tt2", transactionNumber: "TT-20260102-EFGH5678", status: "Pending", side: "Sell" }),
  buildTradeTransactionListItemDto({ id: "tt3", transactionNumber: "TT-20260103-IJKL9012", status: "Failed", side: "Buy" }),
];

const nonTradeTransactions = [
  buildNonTradeTransactionListItemDto({ id: "ntt1", transactionNumber: "NTT-20260101-ABCD1234", status: "Settled" }),
  buildNonTradeTransactionListItemDto({ id: "ntt2", transactionNumber: "NTT-20260102-EFGH5678", status: "Pending" }),
];

export const transactionsHandlers = [
  // Trade Transactions
  http.get(`${BASE}/trade-transactions/by-order/:orderId`, () =>
    HttpResponse.json(tradeTransactions),
  ),

  http.get(`${BASE}/trade-transactions/:id`, ({ params }) => {
    const t = tradeTransactions.find((tt) => tt.id === params.id);
    if (!t) return new HttpResponse(null, { status: 404 });
    return HttpResponse.json({
      ...t, instrumentId: "ins1", instrumentName: "Apple Inc.",
      orderId: null, comment: null, currencyId: null,
    });
  }),

  http.get(`${BASE}/trade-transactions`, () =>
    HttpResponse.json(buildPagedResult(tradeTransactions)),
  ),

  http.post(`${BASE}/trade-transactions`, () =>
    HttpResponse.json({ id: "tt-new" }, { status: 201 }),
  ),

  http.put(`${BASE}/trade-transactions/:id`, () =>
    HttpResponse.json({ id: "tt-updated" }),
  ),

  http.delete(`${BASE}/trade-transactions/:id`, () =>
    new HttpResponse(null, { status: 204 }),
  ),

  // Non-Trade Transactions
  http.get(`${BASE}/non-trade-transactions`, () =>
    HttpResponse.json(buildPagedResult(nonTradeTransactions)),
  ),

  http.get(`${BASE}/non-trade-transactions/by-order/:orderId`, () =>
    HttpResponse.json(nonTradeTransactions),
  ),

  http.get(`${BASE}/non-trade-transactions/:id`, ({ params }) => {
    const t = nonTradeTransactions.find((ntt) => ntt.id === params.id);
    if (!t) return new HttpResponse(null, { status: 404 });
    return HttpResponse.json({
      ...t, currencyId: "cur1", instrumentId: null, instrumentName: null,
      orderId: null, comment: null, description: null,
    });
  }),

  http.post(`${BASE}/non-trade-transactions`, () =>
    HttpResponse.json({ id: "ntt-new" }, { status: 201 }),
  ),

  http.put(`${BASE}/non-trade-transactions/:id`, () =>
    HttpResponse.json({ id: "ntt-updated" }),
  ),

  http.delete(`${BASE}/non-trade-transactions/:id`, () =>
    new HttpResponse(null, { status: 204 }),
  ),

  // Stub handlers for filter dropdowns and dialogs
  http.get(`${BASE}/accounts`, ({ request }) => {
    const url = new URL(request.url);
    if (url.searchParams.get("pageSize") === "200") {
      return HttpResponse.json(buildPagedResult([
        { id: "acc1", number: "ACC-001" },
        { id: "acc2", number: "ACC-002" },
      ]));
    }
    return HttpResponse.json(buildPagedResult([]));
  }),

  http.get(`${BASE}/instruments`, ({ request }) => {
    const url = new URL(request.url);
    if (url.searchParams.get("pageSize") === "200") {
      return HttpResponse.json(buildPagedResult([
        { id: "ins1", symbol: "AAPL", name: "Apple Inc." },
        { id: "ins2", symbol: "GOOGL", name: "Alphabet Inc." },
      ]));
    }
    return HttpResponse.json(buildPagedResult([]));
  }),

  http.get(`${BASE}/trade-orders`, () =>
    HttpResponse.json(buildPagedResult([
      { id: "to1", orderNumber: "TO-001" },
      { id: "to2", orderNumber: "TO-002" },
    ])),
  ),

  http.get(`${BASE}/non-trade-orders`, () =>
    HttpResponse.json(buildPagedResult([
      { id: "nto1", orderNumber: "NTO-001" },
      { id: "nto2", orderNumber: "NTO-002" },
    ])),
  ),

  http.get(`${BASE}/currencies`, () =>
    HttpResponse.json([
      { id: "cur1", code: "USD", name: "US Dollar", symbol: "$", isActive: true },
      { id: "cur2", code: "EUR", name: "Euro", symbol: "€", isActive: true },
    ]),
  ),
];
