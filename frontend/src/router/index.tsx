import { createBrowserRouter } from "react-router-dom";
import { MainLayout } from "../layouts/MainLayout";
import { DashboardPage } from "../pages/DashboardPage";
import { SettingsPage } from "../pages/SettingsPage";
import { LoginPage } from "../pages/LoginPage";
import { UsersPage } from "../pages/UsersPage";
import { RolesPage } from "../pages/RolesPage";
import { AuditPage } from "../pages/AuditPage";
import { ClientsPage } from "../pages/ClientsPage";
import { ClientDetailsPage } from "../pages/ClientDetailsPage";
import { AccountsPage } from "../pages/AccountsPage";
import { AccountDetailsPage } from "../pages/AccountDetailsPage";
import { InstrumentsPage } from "../pages/InstrumentsPage";
import { InstrumentDetailsPage } from "../pages/InstrumentDetailsPage";
import { RequireAuth } from "../auth/RequireAuth";

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
      { index: true, element: <DashboardPage /> },
      { path: "clients", element: <ClientsPage /> },
      { path: "clients/:id", element: <ClientDetailsPage /> },
      { path: "accounts", element: <AccountsPage /> },
      { path: "accounts/:id", element: <AccountDetailsPage /> },
      { path: "instruments", element: <InstrumentsPage /> },
      { path: "instruments/:id", element: <InstrumentDetailsPage /> },
      { path: "users", element: <UsersPage /> },
      { path: "roles", element: <RolesPage /> },
      { path: "audit", element: <AuditPage /> },
      { path: "settings", element: <SettingsPage /> },
    ],
  },
]);
