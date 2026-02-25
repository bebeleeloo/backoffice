import { useState } from "react";
import { Outlet, useNavigate, useLocation } from "react-router-dom";
import {
  AppBar, Box, Divider, Drawer, IconButton,
  List, ListItemButton, ListItemIcon, ListItemText, Toolbar,
  Typography, Button,
} from "@mui/material";
import MenuIcon from "@mui/icons-material/Menu";
import DashboardIcon from "@mui/icons-material/Dashboard";
import PeopleIcon from "@mui/icons-material/People";
import GroupsIcon from "@mui/icons-material/Groups";
import AccountBalanceIcon from "@mui/icons-material/AccountBalance";
import SecurityIcon from "@mui/icons-material/Security";
import HistoryIcon from "@mui/icons-material/History";
import SettingsIcon from "@mui/icons-material/Settings";
import ShowChartIcon from "@mui/icons-material/ShowChart";
import LogoutIcon from "@mui/icons-material/Logout";
import { useAuth } from "../auth/useAuth";
import { useHasPermission } from "../auth/usePermission";
import { ErrorBoundary } from "../components/ErrorBoundary";

const DRAWER_WIDTH = 260;

export function MainLayout() {
  const [mobileOpen, setMobileOpen] = useState(false);
  const navigate = useNavigate();
  const location = useLocation();
  const { user, logout } = useAuth();

  const canViewClients = useHasPermission("clients.read");
  const canViewAccounts = useHasPermission("accounts.read");
  const canViewInstruments = useHasPermission("instruments.read");
  const canViewUsers = useHasPermission("users.read");
  const canViewRoles = useHasPermission("roles.read");
  const canViewAudit = useHasPermission("audit.read");

  const menuItems = [
    { label: "Dashboard", path: "/", icon: <DashboardIcon />, visible: true },
    { label: "Clients", path: "/clients", icon: <GroupsIcon />, visible: canViewClients },
    { label: "Accounts", path: "/accounts", icon: <AccountBalanceIcon />, visible: canViewAccounts },
    { label: "Instruments", path: "/instruments", icon: <ShowChartIcon />, visible: canViewInstruments },
    { label: "Users", path: "/users", icon: <PeopleIcon />, visible: canViewUsers },
    { label: "Roles", path: "/roles", icon: <SecurityIcon />, visible: canViewRoles },
    { label: "Audit Log", path: "/audit", icon: <HistoryIcon />, visible: canViewAudit },
    { label: "Settings", path: "/settings", icon: <SettingsIcon />, visible: true },
  ];

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  const drawer = (
    <Box sx={{ display: "flex", flexDirection: "column", height: "100%" }}>
      <Toolbar>
        <Typography variant="h6" noWrap>
          Broker BO
        </Typography>
      </Toolbar>
      <Divider />
      <List sx={{ flexGrow: 1 }}>
        {menuItems
          .filter((item) => item.visible)
          .map((item) => (
            <ListItemButton
              key={item.path}
              selected={item.path === "/" ? location.pathname === "/" : location.pathname.startsWith(item.path)}
              onClick={() => {
                navigate(item.path);
                setMobileOpen(false);
              }}
            >
              <ListItemIcon>{item.icon}</ListItemIcon>
              <ListItemText primary={item.label} />
            </ListItemButton>
          ))}
      </List>
      <Divider />
      <Box sx={{ p: 2 }}>
        <Typography variant="body2" color="text.secondary" noWrap data-testid="sidebar-username">{user?.fullName || user?.username}</Typography>
        <Button size="small" startIcon={<LogoutIcon />} onClick={handleLogout} sx={{ mt: 1 }}>
          Logout
        </Button>
      </Box>
    </Box>
  );

  return (
    <Box sx={{ display: "flex", height: { xs: "100dvh", md: "100vh" }, overflow: "hidden" }}>

      <AppBar
        position="fixed"
        sx={{
          width: { md: `calc(100% - ${DRAWER_WIDTH}px)` },
          ml: { md: `${DRAWER_WIDTH}px` },
        }}
      >
        <Toolbar>
          <IconButton
            color="inherit"
            edge="start"
            onClick={() => setMobileOpen(!mobileOpen)}
            sx={{ mr: 2, display: { md: "none" } }}
          >
            <MenuIcon />
          </IconButton>
          <Typography variant="h6" noWrap sx={{ flexGrow: 1 }}>
            Broker Backoffice
          </Typography>
        </Toolbar>
      </AppBar>

      <Box
        component="nav"
        sx={{ width: { md: DRAWER_WIDTH }, flexShrink: { md: 0 } }}
      >
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={() => setMobileOpen(false)}
          ModalProps={{ keepMounted: true }}
          sx={{
            display: { xs: "block", md: "none" },
            "& .MuiDrawer-paper": { boxSizing: "border-box", width: DRAWER_WIDTH },
          }}
        >
          {drawer}
        </Drawer>

        <Drawer
          variant="permanent"
          sx={{
            display: { xs: "none", md: "block" },
            "& .MuiDrawer-paper": { boxSizing: "border-box", width: DRAWER_WIDTH },
          }}
          open
        >
          {drawer}
        </Drawer>
      </Box>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          display: "flex",
          flexDirection: "column",
          minHeight: 0,
          width: { md: `calc(100% - ${DRAWER_WIDTH}px)` },
        }}
      >
        <Toolbar />
        <Box sx={{ display: "flex", flexDirection: "column", flexGrow: 1, minHeight: 0, overflowY: "auto" }}>
          <ErrorBoundary>
            <Outlet />
          </ErrorBoundary>
        </Box>
      </Box>
    </Box>
  );
}
