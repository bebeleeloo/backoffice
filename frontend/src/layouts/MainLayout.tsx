import { useState } from "react";
import { Outlet, useNavigate, useLocation } from "react-router-dom";
import {
  AppBar, Box, Collapse, Divider, Drawer, IconButton,
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
import ReceiptLongIcon from "@mui/icons-material/ReceiptLong";
import SwapHorizIcon from "@mui/icons-material/SwapHoriz";
import AssignmentIcon from "@mui/icons-material/Assignment";
import ExpandLess from "@mui/icons-material/ExpandLess";
import ExpandMore from "@mui/icons-material/ExpandMore";
import LogoutIcon from "@mui/icons-material/Logout";
import ReceiptIcon from "@mui/icons-material/Receipt";
import AccountBalanceWalletIcon from "@mui/icons-material/AccountBalanceWallet";
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
  const canViewOrders = useHasPermission("orders.read");
  const canViewTransactions = useHasPermission("transactions.read");
  const canViewUsers = useHasPermission("users.read");
  const canViewRoles = useHasPermission("roles.read");
  const canViewAudit = useHasPermission("audit.read");

  const isOrdersPath = location.pathname.startsWith("/trade-orders") || location.pathname.startsWith("/non-trade-orders");
  const [ordersOpen, setOrdersOpen] = useState(isOrdersPath);

  const isTransactionsPath = location.pathname.startsWith("/trade-transactions") || location.pathname.startsWith("/non-trade-transactions");
  const [transactionsOpen, setTransactionsOpen] = useState(isTransactionsPath);

  const menuItems = [
    { label: "Dashboard", path: "/", icon: <DashboardIcon />, visible: true },
    { label: "Clients", path: "/clients", icon: <GroupsIcon />, visible: canViewClients },
    { label: "Accounts", path: "/accounts", icon: <AccountBalanceIcon />, visible: canViewAccounts },
    { label: "Instruments", path: "/instruments", icon: <ShowChartIcon />, visible: canViewInstruments },
  ];

  const menuItemsAfterOrders = [
    { label: "Users", path: "/users", icon: <PeopleIcon />, visible: canViewUsers },
    { label: "Roles", path: "/roles", icon: <SecurityIcon />, visible: canViewRoles },
    { label: "Audit Log", path: "/audit", icon: <HistoryIcon />, visible: canViewAudit },
    { label: "Settings", path: "/settings", icon: <SettingsIcon />, visible: true },
  ];

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  const renderMenuItem = (item: { label: string; path: string; icon: React.ReactNode }) => (
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
  );

  const drawer = (
    <Box sx={{ display: "flex", flexDirection: "column", height: "100%" }}>
      <Toolbar sx={{ gap: 1.5 }}>
        <Box component="img" src="/logo.svg" alt="Logo" sx={{ width: 32, height: 32 }} />
        <Typography variant="h6" noWrap>
          Broker BO
        </Typography>
      </Toolbar>
      <Divider />
      <List sx={{ flexGrow: 1 }}>
        {menuItems.filter((item) => item.visible).map(renderMenuItem)}

        {canViewOrders && (
          <>
            <ListItemButton onClick={() => setOrdersOpen(!ordersOpen)} selected={isOrdersPath}>
              <ListItemIcon><AssignmentIcon /></ListItemIcon>
              <ListItemText primary="Orders" />
              {ordersOpen ? <ExpandLess /> : <ExpandMore />}
            </ListItemButton>
            <Collapse in={ordersOpen} timeout="auto" unmountOnExit>
              <List component="div" disablePadding>
                <ListItemButton
                  sx={{ pl: 4 }}
                  selected={location.pathname.startsWith("/trade-orders")}
                  onClick={() => { navigate("/trade-orders"); setMobileOpen(false); }}
                >
                  <ListItemIcon><ReceiptLongIcon /></ListItemIcon>
                  <ListItemText primary="Trade Orders" />
                </ListItemButton>
                <ListItemButton
                  sx={{ pl: 4 }}
                  selected={location.pathname.startsWith("/non-trade-orders")}
                  onClick={() => { navigate("/non-trade-orders"); setMobileOpen(false); }}
                >
                  <ListItemIcon><SwapHorizIcon /></ListItemIcon>
                  <ListItemText primary="Non-Trade Orders" />
                </ListItemButton>
              </List>
            </Collapse>
          </>
        )}

        {canViewTransactions && (
          <>
            <ListItemButton onClick={() => setTransactionsOpen(!transactionsOpen)} selected={isTransactionsPath}>
              <ListItemIcon><ReceiptIcon /></ListItemIcon>
              <ListItemText primary="Transactions" />
              {transactionsOpen ? <ExpandLess /> : <ExpandMore />}
            </ListItemButton>
            <Collapse in={transactionsOpen} timeout="auto" unmountOnExit>
              <List component="div" disablePadding>
                <ListItemButton
                  sx={{ pl: 4 }}
                  selected={location.pathname.startsWith("/trade-transactions")}
                  onClick={() => { navigate("/trade-transactions"); setMobileOpen(false); }}
                >
                  <ListItemIcon><ReceiptLongIcon /></ListItemIcon>
                  <ListItemText primary="Trade Transactions" />
                </ListItemButton>
                <ListItemButton
                  sx={{ pl: 4 }}
                  selected={location.pathname.startsWith("/non-trade-transactions")}
                  onClick={() => { navigate("/non-trade-transactions"); setMobileOpen(false); }}
                >
                  <ListItemIcon><AccountBalanceWalletIcon /></ListItemIcon>
                  <ListItemText primary="Non-Trade Transactions" />
                </ListItemButton>
              </List>
            </Collapse>
          </>
        )}

        {menuItemsAfterOrders.filter((item) => item.visible).map(renderMenuItem)}
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
