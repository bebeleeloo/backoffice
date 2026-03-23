import { Tabs, Tab } from "@mui/material";
import { useLocation, useNavigate, Outlet } from "react-router-dom";
import { PageContainer, useHasPermission } from "@broker/ui-kit";

export function AuthenticationPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const canReadUsers = useHasPermission("users.read");
  const canReadRoles = useHasPermission("roles.read");

  const isRolesPath = location.pathname.startsWith("/roles");

  const tabs: { label: string; path: string; visible: boolean }[] = [
    { label: "Users", path: "/users", visible: canReadUsers },
    { label: "Roles", path: "/roles", visible: canReadRoles },
  ];

  const visibleTabs = tabs.filter((t) => t.visible);
  const tabIndex = visibleTabs.findIndex((t) =>
    isRolesPath ? t.path === "/roles" : t.path === "/users",
  );

  return (
    <PageContainer title="Authentication" variant="list">
      <Tabs
        value={tabIndex >= 0 ? tabIndex : 0}
        onChange={(_, v) => navigate(visibleTabs[v].path)}
        sx={{ mb: 1.5 }}
      >
        {visibleTabs.map((t) => (
          <Tab key={t.path} label={t.label} />
        ))}
      </Tabs>
      <Outlet />
    </PageContainer>
  );
}
