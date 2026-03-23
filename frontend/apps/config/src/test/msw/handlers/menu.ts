import { http, HttpResponse } from "msw";
import { buildMenuItemConfig } from "../../factories";

const BASE = "/api/v1";

const menuItems = [
  buildMenuItemConfig({ id: "dashboard", label: "Dashboard", icon: "Dashboard", path: "/" }),
  buildMenuItemConfig({
    id: "clients",
    label: "Clients",
    icon: "People",
    path: "/clients",
    permissions: ["clients.read"],
  }),
  buildMenuItemConfig({
    id: "orders",
    label: "Orders",
    icon: "ShoppingCart",
    path: undefined,
    children: [
      buildMenuItemConfig({ id: "trade-orders", label: "Trade Orders", icon: "TrendingUp", path: "/orders/trade" }),
      buildMenuItemConfig({ id: "non-trade-orders", label: "Non-Trade Orders", icon: "SwapHoriz", path: "/orders/non-trade" }),
    ],
  }),
];

export const menuHandlers = [
  http.get(`${BASE}/config/menu/raw`, () =>
    HttpResponse.json(menuItems),
  ),

  http.put(`${BASE}/config/menu`, () =>
    HttpResponse.json(null, { status: 200 }),
  ),
];
