import { useState } from "react";
import { Outlet, useNavigate, useLocation } from "react-router-dom";
import {
  Box, Collapse, Divider, Drawer, IconButton,
  List, ListItemButton, ListItemIcon, ListItemText,
  Typography, Button, Tooltip,
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
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import { useAuth } from "../auth/useAuth";
import { useHasPermission } from "../auth/usePermission";
import { ErrorBoundary } from "../components/ErrorBoundary";
import { SIDEBAR_COLORS } from "../theme";

const DRAWER_WIDTH_EXPANDED = 260;
const DRAWER_WIDTH_COLLAPSED = 72;
const SIDEBAR_STORAGE_KEY = "sidebarCollapsed";
const TRANSITION = "width 200ms cubic-bezier(0.4, 0, 0.2, 1)";

export function MainLayout() {
  const [mobileOpen, setMobileOpen] = useState(false);
  const [collapsed, setCollapsed] = useState(() => localStorage.getItem(SIDEBAR_STORAGE_KEY) === "true");
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

  const drawerWidth = collapsed ? DRAWER_WIDTH_COLLAPSED : DRAWER_WIDTH_EXPANDED;

  const toggleCollapsed = () => {
    setCollapsed((prev) => {
      const next = !prev;
      localStorage.setItem(SIDEBAR_STORAGE_KEY, String(next));
      return next;
    });
  };

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

  const itemSx = (isActive: boolean) => ({
    mx: 1,
    mb: 0.5,
    borderRadius: 1.5,
    minHeight: 44,
    justifyContent: collapsed ? "center" : "flex-start",
    px: collapsed ? 0 : 2,
    color: SIDEBAR_COLORS.text,
    "&.Mui-selected": {
      bgcolor: SIDEBAR_COLORS.bgActiveAlpha,
      color: SIDEBAR_COLORS.textActive,
      borderLeft: isActive ? `3px solid ${SIDEBAR_COLORS.activeIndicator}` : undefined,
      "&:hover": { bgcolor: SIDEBAR_COLORS.bgActiveAlpha },
    },
    "&:hover": { bgcolor: SIDEBAR_COLORS.bgHover },
  });

  const iconSx = { color: "inherit", minWidth: collapsed ? 0 : 40, justifyContent: "center" };

  const renderMenuItem = (item: { label: string; path: string; icon: React.ReactNode }) => {
    const isActive = item.path === "/" ? location.pathname === "/" : location.pathname.startsWith(item.path);
    return (
      <ListItemButton
        key={item.path}
        selected={isActive}
        onClick={() => { navigate(item.path); setMobileOpen(false); }}
        sx={itemSx(isActive)}
      >
        <Tooltip title={collapsed ? item.label : ""} placement="right" arrow>
          <ListItemIcon sx={iconSx}>{item.icon}</ListItemIcon>
        </Tooltip>
        {!collapsed && <ListItemText primary={item.label} primaryTypographyProps={{ fontSize: "0.875rem" }} />}
      </ListItemButton>
    );
  };

  const renderSubMenu = (
    label: string,
    icon: React.ReactNode,
    isPath: boolean,
    open: boolean,
    setOpen: (v: boolean) => void,
    defaultPath: string,
    children: { label: string; path: string; icon: React.ReactNode }[],
  ) => (
    <>
      <ListItemButton
        onClick={() => {
          if (collapsed) { navigate(defaultPath); setMobileOpen(false); }
          else setOpen(!open);
        }}
        selected={isPath}
        sx={itemSx(isPath)}
      >
        <Tooltip title={collapsed ? label : ""} placement="right" arrow>
          <ListItemIcon sx={iconSx}>{icon}</ListItemIcon>
        </Tooltip>
        {!collapsed && <ListItemText primary={label} primaryTypographyProps={{ fontSize: "0.875rem" }} />}
        {!collapsed && (open ? <ExpandLess sx={{ color: SIDEBAR_COLORS.textMuted }} /> : <ExpandMore sx={{ color: SIDEBAR_COLORS.textMuted }} />)}
      </ListItemButton>
      {!collapsed && (
        <Collapse in={open} timeout="auto" unmountOnExit>
          <List component="div" disablePadding>
            {children.map((child) => {
              const childActive = location.pathname.startsWith(child.path);
              return (
                <ListItemButton
                  key={child.path}
                  sx={{ ...itemSx(childActive), pl: 5 }}
                  selected={childActive}
                  onClick={() => { navigate(child.path); setMobileOpen(false); }}
                >
                  <ListItemIcon sx={iconSx}>{child.icon}</ListItemIcon>
                  <ListItemText primary={child.label} primaryTypographyProps={{ fontSize: "0.8125rem" }} />
                </ListItemButton>
              );
            })}
          </List>
        </Collapse>
      )}
    </>
  );

  const drawerContent = (isCollapsed: boolean) => (
    <Box sx={{ display: "flex", flexDirection: "column", height: "100%" }}>
      {/* Header */}
      <Box sx={{
        display: "flex",
        alignItems: "center",
        height: 64,
        px: isCollapsed ? 0 : 2,
        justifyContent: isCollapsed ? "center" : "flex-start",
        gap: 1.5,
        flexShrink: 0,
      }}>
        <Box component="img" src="/logo.svg" alt="Logo" sx={{ width: 32, height: 32, flexShrink: 0 }} />
        {!isCollapsed && (
          <Typography variant="h6" noWrap sx={{ color: SIDEBAR_COLORS.logo, fontWeight: 700 }}>
            Broker BO
          </Typography>
        )}
      </Box>

      {/* Collapse toggle (desktop only) */}
      <Box sx={{
        display: { xs: "none", md: "flex" },
        justifyContent: isCollapsed ? "center" : "flex-end",
        px: 1,
        pb: 1,
      }}>
        <IconButton
          onClick={toggleCollapsed}
          size="small"
          sx={{ color: SIDEBAR_COLORS.textMuted, "&:hover": { color: SIDEBAR_COLORS.text } }}
        >
          {isCollapsed ? <ChevronRightIcon /> : <ChevronLeftIcon />}
        </IconButton>
      </Box>

      <Divider sx={{ borderColor: SIDEBAR_COLORS.divider }} />

      {/* Menu */}
      <List sx={{ flexGrow: 1, py: 1, overflowY: "auto", overflowX: "hidden" }}>
        {menuItems.filter((item) => item.visible).map(renderMenuItem)}

        {canViewOrders && renderSubMenu(
          "Orders", <AssignmentIcon />, isOrdersPath, ordersOpen, setOrdersOpen, "/trade-orders",
          [
            { label: "Trade Orders", path: "/trade-orders", icon: <ReceiptLongIcon /> },
            { label: "Non-Trade Orders", path: "/non-trade-orders", icon: <SwapHorizIcon /> },
          ],
        )}

        {canViewTransactions && renderSubMenu(
          "Transactions", <ReceiptIcon />, isTransactionsPath, transactionsOpen, setTransactionsOpen, "/trade-transactions",
          [
            { label: "Trade Transactions", path: "/trade-transactions", icon: <ReceiptLongIcon /> },
            { label: "Non-Trade Transactions", path: "/non-trade-transactions", icon: <AccountBalanceWalletIcon /> },
          ],
        )}

        {menuItemsAfterOrders.filter((item) => item.visible).map(renderMenuItem)}
      </List>

      <Divider sx={{ borderColor: SIDEBAR_COLORS.divider }} />

      {/* User section */}
      <Box sx={{
        p: isCollapsed ? 1.5 : 2,
        display: "flex",
        alignItems: "center",
        gap: 1.5,
        justifyContent: isCollapsed ? "center" : "flex-start",
        flexShrink: 0,
      }}>
        <Tooltip title={isCollapsed ? `${user?.fullName || user?.username}` : ""} placement="right" arrow>
          <Box sx={{
            width: 36,
            height: 36,
            borderRadius: "50%",
            bgcolor: SIDEBAR_COLORS.bgActiveAlpha,
            border: `2px solid ${SIDEBAR_COLORS.activeIndicator}`,
            display: "flex",
            alignItems: "center",
            justifyContent: "center",
            color: SIDEBAR_COLORS.textActive,
            fontSize: "0.875rem",
            fontWeight: 600,
            flexShrink: 0,
          }}>
            {(user?.fullName || user?.username || "U").charAt(0).toUpperCase()}
          </Box>
        </Tooltip>
        {!isCollapsed && (
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Typography variant="body2" noWrap sx={{ color: SIDEBAR_COLORS.text, fontWeight: 500 }} data-testid="sidebar-username">
              {user?.fullName || user?.username}
            </Typography>
            <Button
              size="small"
              startIcon={<LogoutIcon />}
              onClick={handleLogout}
              sx={{
                mt: 0.5, color: SIDEBAR_COLORS.textMuted, textTransform: "none", p: 0, minWidth: 0,
                "&:hover": { color: SIDEBAR_COLORS.text, bgcolor: "transparent" },
              }}
            >
              Logout
            </Button>
          </Box>
        )}
      </Box>
    </Box>
  );

  return (
    <Box sx={{ display: "flex", height: { xs: "100dvh", md: "100vh" }, overflow: "hidden" }}>

      {/* Mobile hamburger */}
      <IconButton
        onClick={() => setMobileOpen(!mobileOpen)}
        sx={{
          display: { xs: "flex", md: "none" },
          position: "fixed",
          top: 12,
          left: 12,
          zIndex: (theme) => theme.zIndex.drawer + 1,
          bgcolor: "background.paper",
          boxShadow: 2,
          "&:hover": { bgcolor: "action.hover" },
        }}
      >
        <MenuIcon />
      </IconButton>

      <Box
        component="nav"
        sx={{ width: { md: drawerWidth }, flexShrink: { md: 0 }, transition: TRANSITION }}
      >
        {/* Mobile drawer — always expanded */}
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={() => setMobileOpen(false)}
          ModalProps={{ keepMounted: true }}
          sx={{
            display: { xs: "block", md: "none" },
            "& .MuiDrawer-paper": {
              boxSizing: "border-box",
              width: DRAWER_WIDTH_EXPANDED,
              bgcolor: SIDEBAR_COLORS.bg,
              borderRight: "none",
              color: SIDEBAR_COLORS.text,
            },
          }}
        >
          {drawerContent(false)}
        </Drawer>

        {/* Desktop drawer — collapsible */}
        <Drawer
          variant="permanent"
          sx={{
            display: { xs: "none", md: "block" },
            "& .MuiDrawer-paper": {
              boxSizing: "border-box",
              width: drawerWidth,
              transition: TRANSITION,
              overflowX: "hidden",
              bgcolor: SIDEBAR_COLORS.bg,
              borderRight: "none",
              color: SIDEBAR_COLORS.text,
            },
          }}
          open
        >
          {drawerContent(collapsed)}
        </Drawer>
      </Box>

      <Box
        component="main"
        sx={{
          flexGrow: 1,
          display: "flex",
          flexDirection: "column",
          minHeight: 0,
          width: { md: `calc(100% - ${drawerWidth}px)` },
          transition: TRANSITION,
        }}
      >
        <Box sx={{ display: "flex", flexDirection: "column", flexGrow: 1, minHeight: 0, overflowY: "auto" }}>
          <ErrorBoundary>
            <Outlet />
          </ErrorBoundary>
        </Box>
      </Box>
    </Box>
  );
}
