/* eslint-disable react-refresh/only-export-components */
import React, { Suspense } from "react";
import { createBrowserRouter } from "react-router-dom";
import { MainLayout, RequireAuth, RouteLoadingFallback, NavigationProvider } from "@broker/ui-kit";
import { LoginPage } from "@broker/auth-module";
import { NotFoundPage } from "../pages/NotFoundPage";

const UsersPage = React.lazy(() => import("@broker/auth-module").then((m) => ({ default: m.UsersTab })));
const RolesPage = React.lazy(() => import("@broker/auth-module").then((m) => ({ default: m.RolesTab })));
const RoleDetailsPage = React.lazy(() => import("@broker/auth-module").then((m) => ({ default: m.RoleDetailsPage })));

function withSuspense(Component: React.LazyExoticComponent<React.ComponentType>) {
  return (
    <Suspense fallback={<RouteLoadingFallback />}>
      <Component />
    </Suspense>
  );
}

const INTERNAL_PATHS = ["/login", "/users", "/roles"];

export const router = createBrowserRouter([
  { path: "/login", element: <LoginPage /> },
  {
    path: "/",
    element: (
      <RequireAuth>
        <NavigationProvider internalPaths={INTERNAL_PATHS}>
          <MainLayout />
        </NavigationProvider>
      </RequireAuth>
    ),
    children: [
      { path: "users", element: withSuspense(UsersPage) },
      { path: "roles", element: withSuspense(RolesPage) },
      { path: "roles/:id", element: withSuspense(RoleDetailsPage) },
      { path: "*", element: <NotFoundPage /> },
    ],
  },
]);
