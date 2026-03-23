import { useState, useMemo, createElement } from "react";
import { Outlet, useLocation } from "react-router-dom";
import {
  Box, Collapse, Divider, Drawer, IconButton,
  List, ListItemButton, ListItemIcon, ListItemText,
  Typography, Button, Tooltip, Skeleton,
} from "@mui/material";
import MenuIcon from "@mui/icons-material/Menu";
import ExpandLess from "@mui/icons-material/ExpandLess";
import ExpandMore from "@mui/icons-material/ExpandMore";
import LogoutIcon from "@mui/icons-material/Logout";
import ChevronLeftIcon from "@mui/icons-material/ChevronLeft";
import ChevronRightIcon from "@mui/icons-material/ChevronRight";
import { useAuth } from "../auth/useAuth";
import { ErrorBoundary } from "../components/ErrorBoundary";
import { UserAvatar } from "../components/UserAvatar";
import { SIDEBAR_COLORS } from "../theme";
import { useMenu } from "../api/configApi";
import type { MenuItem } from "../api/configApi";
import { iconMap, FallbackIcon } from "../icons";
import { useAppNavigation } from "../navigation/useAppNavigation";

const DRAWER_WIDTH_EXPANDED = 260;
const DRAWER_WIDTH_COLLAPSED = 72;
const SIDEBAR_STORAGE_KEY = "sidebarCollapsed";
const TRANSITION = "width 200ms cubic-bezier(0.4, 0, 0.2, 1)";

function resolveIcon(name: string) {
  return createElement(iconMap[name] ?? FallbackIcon);
}

export function MainLayout() {
  const [mobileOpen, setMobileOpen] = useState(false);
  const [collapsed, setCollapsed] = useState(() => localStorage.getItem(SIDEBAR_STORAGE_KEY) === "true");
  const { navigateTo } = useAppNavigation();
  const location = useLocation();
  const { user, logout } = useAuth();
  const { data: menuItems = [], isLoading: menuLoading } = useMenu();

  // Track which submenus are open, keyed by item id
  const [openSubmenus, setOpenSubmenus] = useState<Record<string, boolean>>({});

  // Auto-open submenus based on current path
  const activeSubmenus = useMemo(() => {
    const result: Record<string, boolean> = {};
    for (const item of menuItems) {
      if (item.children?.length) {
        const isActive = item.children.some((child) =>
          child.path && location.pathname.startsWith(child.path)
        );
        if (isActive) result[item.id] = true;
      }
    }
    return result;
  }, [menuItems, location.pathname]);

  const isSubmenuOpen = (id: string) => openSubmenus[id] ?? activeSubmenus[id] ?? false;

  const toggleSubmenu = (id: string) => {
    setOpenSubmenus((prev) => ({
      ...prev,
      [id]: !(prev[id] ?? activeSubmenus[id] ?? false),
    }));
  };

  const drawerWidth = collapsed ? DRAWER_WIDTH_COLLAPSED : DRAWER_WIDTH_EXPANDED;

  const toggleCollapsed = () => {
    setCollapsed((prev) => {
      const next = !prev;
      localStorage.setItem(SIDEBAR_STORAGE_KEY, String(next));
      return next;
    });
  };

  const handleLogout = () => {
    logout();
    window.location.href = "/login";
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

  const renderMenuItem = (item: MenuItem) => {
    if (!item.path) return null;
    const isActive = item.path === "/" ? location.pathname === "/" : location.pathname.startsWith(item.path);
    return (
      <ListItemButton
        key={item.id}
        selected={isActive}
        onClick={() => { navigateTo(item.path!); setMobileOpen(false); }}
        sx={itemSx(isActive)}
      >
        <Tooltip title={collapsed ? item.label : ""} placement="right" arrow>
          <ListItemIcon sx={iconSx}>{resolveIcon(item.icon)}</ListItemIcon>
        </Tooltip>
        {!collapsed && <ListItemText primary={item.label} primaryTypographyProps={{ fontSize: "0.875rem" }} />}
      </ListItemButton>
    );
  };

  const renderSubMenu = (item: MenuItem) => {
    const children = item.children ?? [];
    const isPath = children.some((child) => child.path && location.pathname.startsWith(child.path));
    const open = isSubmenuOpen(item.id);
    const defaultPath = children[0]?.path ?? "/";

    return (
      <Box key={item.id}>
        <ListItemButton
          onClick={() => {
            if (collapsed) { navigateTo(defaultPath); setMobileOpen(false); }
            else toggleSubmenu(item.id);
          }}
          selected={isPath}
          sx={itemSx(isPath)}
        >
          <Tooltip title={collapsed ? item.label : ""} placement="right" arrow>
            <ListItemIcon sx={iconSx}>{resolveIcon(item.icon)}</ListItemIcon>
          </Tooltip>
          {!collapsed && <ListItemText primary={item.label} primaryTypographyProps={{ fontSize: "0.875rem" }} />}
          {!collapsed && (open ? <ExpandLess sx={{ color: SIDEBAR_COLORS.textMuted }} /> : <ExpandMore sx={{ color: SIDEBAR_COLORS.textMuted }} />)}
        </ListItemButton>
        {!collapsed && (
          <Collapse in={open} timeout="auto" unmountOnExit>
            <List component="div" disablePadding>
              {children.map((child) => {
                const childActive = child.path ? location.pathname.startsWith(child.path) : false;
                return (
                  <ListItemButton
                    key={child.id}
                    sx={{ ...itemSx(childActive), pl: 5 }}
                    selected={childActive}
                    onClick={() => { if (child.path) navigateTo(child.path); setMobileOpen(false); }}
                  >
                    <ListItemIcon sx={iconSx}>{resolveIcon(child.icon)}</ListItemIcon>
                    <ListItemText primary={child.label} primaryTypographyProps={{ fontSize: "0.8125rem" }} />
                  </ListItemButton>
                );
              })}
            </List>
          </Collapse>
        )}
      </Box>
    );
  };

  const renderMenuSkeleton = () => (
    <>
      {Array.from({ length: 8 }).map((_, i) => (
        <Box key={i} sx={{ mx: 1, mb: 0.5, px: collapsed ? 0 : 2, display: "flex", alignItems: "center", gap: 1.5, height: 44, justifyContent: collapsed ? "center" : "flex-start" }}>
          <Skeleton variant="circular" width={24} height={24} sx={{ bgcolor: SIDEBAR_COLORS.bgHover }} />
          {!collapsed && <Skeleton variant="text" width={120} sx={{ bgcolor: SIDEBAR_COLORS.bgHover }} />}
        </Box>
      ))}
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
        {menuLoading ? renderMenuSkeleton() : menuItems.map((item) =>
          item.children?.length ? renderSubMenu(item) : renderMenuItem(item)
        )}
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
          <Box sx={{ flexShrink: 0 }}>
            <UserAvatar
              userId={user?.id ?? ""}
              name={user?.fullName || user?.username || "U"}
              hasPhoto={user?.hasPhoto ?? false}
              size={36}
              sx={{ border: `2px solid ${SIDEBAR_COLORS.activeIndicator}` }}
            />
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
