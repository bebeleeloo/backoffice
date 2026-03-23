/* eslint-disable react-refresh/only-export-components */
import React, { Suspense } from "react";
import { createBrowserRouter, Navigate } from "react-router-dom";
import { MainLayout, RequireAuth, RouteLoadingFallback, NavigationProvider } from "@broker/ui-kit";
import { NotFoundPage } from "../pages/NotFoundPage";

const MenuEditorPage = React.lazy(() => import("../pages/MenuEditorPage").then((m) => ({ default: m.MenuEditorPage })));
const EntityFieldsPage = React.lazy(() => import("../pages/EntityFieldsPage").then((m) => ({ default: m.EntityFieldsPage })));
const UpstreamsPage = React.lazy(() => import("../pages/UpstreamsPage").then((m) => ({ default: m.UpstreamsPage })));

function withSuspense(Component: React.LazyExoticComponent<React.ComponentType>) {
  return (
    <Suspense fallback={<RouteLoadingFallback />}>
      <Component />
    </Suspense>
  );
}

const INTERNAL_PATHS = ["/config"];

export const router = createBrowserRouter([
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
      { path: "config", children: [
        { index: true, element: <Navigate to="/config/menu" replace /> },
        { path: "menu", element: withSuspense(MenuEditorPage) },
        { path: "entities", element: withSuspense(EntityFieldsPage) },
        { path: "upstreams", element: withSuspense(UpstreamsPage) },
      ]},
      { path: "*", element: <NotFoundPage /> },
    ],
  },
]);
