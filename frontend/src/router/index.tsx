/* eslint-disable react-refresh/only-export-components */
import React, { Suspense } from "react";
import { createBrowserRouter } from "react-router-dom";
import { MainLayout } from "../layouts/MainLayout";
import { LoginPage } from "../pages/LoginPage";
import { RequireAuth } from "../auth/RequireAuth";
import { NotFoundPage } from "../pages/NotFoundPage";
import { RouteLoadingFallback } from "../components/RouteLoadingFallback";

const DashboardPage = React.lazy(() => import("../pages/DashboardPage").then((m) => ({ default: m.DashboardPage })));
const ClientsPage = React.lazy(() => import("../pages/ClientsPage").then((m) => ({ default: m.ClientsPage })));
const ClientDetailsPage = React.lazy(() => import("../pages/ClientDetailsPage").then((m) => ({ default: m.ClientDetailsPage })));
const AccountsPage = React.lazy(() => import("../pages/AccountsPage").then((m) => ({ default: m.AccountsPage })));
const AccountDetailsPage = React.lazy(() => import("../pages/AccountDetailsPage").then((m) => ({ default: m.AccountDetailsPage })));
const InstrumentsPage = React.lazy(() => import("../pages/InstrumentsPage").then((m) => ({ default: m.InstrumentsPage })));
const InstrumentDetailsPage = React.lazy(() => import("../pages/InstrumentDetailsPage").then((m) => ({ default: m.InstrumentDetailsPage })));
const UsersPage = React.lazy(() => import("../pages/UsersPage").then((m) => ({ default: m.UsersPage })));
const RolesPage = React.lazy(() => import("../pages/RolesPage").then((m) => ({ default: m.RolesPage })));
const RoleDetailsPage = React.lazy(() => import("../pages/RoleDetailsPage").then((m) => ({ default: m.RoleDetailsPage })));
const TradeOrdersPage = React.lazy(() => import("../pages/TradeOrdersPage").then((m) => ({ default: m.TradeOrdersPage })));
const TradeOrderDetailsPage = React.lazy(() => import("../pages/TradeOrderDetailsPage").then((m) => ({ default: m.TradeOrderDetailsPage })));
const NonTradeOrdersPage = React.lazy(() => import("../pages/NonTradeOrdersPage").then((m) => ({ default: m.NonTradeOrdersPage })));
const NonTradeOrderDetailsPage = React.lazy(() => import("../pages/NonTradeOrderDetailsPage").then((m) => ({ default: m.NonTradeOrderDetailsPage })));
const TradeTransactionsPage = React.lazy(() => import("../pages/TradeTransactionsPage").then((m) => ({ default: m.TradeTransactionsPage })));
const TradeTransactionDetailsPage = React.lazy(() => import("../pages/TradeTransactionDetailsPage").then((m) => ({ default: m.TradeTransactionDetailsPage })));
const NonTradeTransactionsPage = React.lazy(() => import("../pages/NonTradeTransactionsPage").then((m) => ({ default: m.NonTradeTransactionsPage })));
const NonTradeTransactionDetailsPage = React.lazy(() => import("../pages/NonTradeTransactionDetailsPage").then((m) => ({ default: m.NonTradeTransactionDetailsPage })));
const AuditPage = React.lazy(() => import("../pages/AuditPage").then((m) => ({ default: m.AuditPage })));
const SettingsPage = React.lazy(() => import("../pages/SettingsPage").then((m) => ({ default: m.SettingsPage })));

function withSuspense(Component: React.LazyExoticComponent<React.ComponentType>) {
  return (
    <Suspense fallback={<RouteLoadingFallback />}>
      <Component />
    </Suspense>
  );
}

export const router = createBrowserRouter([
  { path: "/login", element: <LoginPage /> },
  {
    path: "/",
    element: (
      <RequireAuth>
        <MainLayout />
      </RequireAuth>
    ),
    children: [
      { index: true, element: withSuspense(DashboardPage) },
      { path: "clients", element: withSuspense(ClientsPage) },
      { path: "clients/:id", element: withSuspense(ClientDetailsPage) },
      { path: "accounts", element: withSuspense(AccountsPage) },
      { path: "accounts/:id", element: withSuspense(AccountDetailsPage) },
      { path: "instruments", element: withSuspense(InstrumentsPage) },
      { path: "instruments/:id", element: withSuspense(InstrumentDetailsPage) },
      { path: "trade-orders", element: withSuspense(TradeOrdersPage) },
      { path: "trade-orders/:id", element: withSuspense(TradeOrderDetailsPage) },
      { path: "non-trade-orders", element: withSuspense(NonTradeOrdersPage) },
      { path: "non-trade-orders/:id", element: withSuspense(NonTradeOrderDetailsPage) },
      { path: "trade-transactions", element: withSuspense(TradeTransactionsPage) },
      { path: "trade-transactions/:id", element: withSuspense(TradeTransactionDetailsPage) },
      { path: "non-trade-transactions", element: withSuspense(NonTradeTransactionsPage) },
      { path: "non-trade-transactions/:id", element: withSuspense(NonTradeTransactionDetailsPage) },
      { path: "users", element: withSuspense(UsersPage) },
      { path: "roles", element: withSuspense(RolesPage) },
      { path: "roles/:id", element: withSuspense(RoleDetailsPage) },
      { path: "audit", element: withSuspense(AuditPage) },
      { path: "settings", element: withSuspense(SettingsPage) },
      { path: "*", element: <NotFoundPage /> },
    ],
  },
]);
