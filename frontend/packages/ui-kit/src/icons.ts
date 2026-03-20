import type { ComponentType } from "react";
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
import ReceiptIcon from "@mui/icons-material/Receipt";
import AccountBalanceWalletIcon from "@mui/icons-material/AccountBalanceWallet";
import AdminPanelSettingsIcon from "@mui/icons-material/AdminPanelSettings";
import MenuBookIcon from "@mui/icons-material/MenuBook";
import ViewColumnIcon from "@mui/icons-material/ViewColumn";
import CloudIcon from "@mui/icons-material/Cloud";
import FolderIcon from "@mui/icons-material/Folder";

export const iconMap: Record<string, ComponentType> = {
  Dashboard: DashboardIcon,
  People: PeopleIcon,
  Groups: GroupsIcon,
  AccountBalance: AccountBalanceIcon,
  Security: SecurityIcon,
  History: HistoryIcon,
  Settings: SettingsIcon,
  ShowChart: ShowChartIcon,
  ReceiptLong: ReceiptLongIcon,
  SwapHoriz: SwapHorizIcon,
  Assignment: AssignmentIcon,
  Receipt: ReceiptIcon,
  AccountBalanceWallet: AccountBalanceWalletIcon,
  AdminPanelSettings: AdminPanelSettingsIcon,
  Menu: MenuBookIcon,
  ViewColumn: ViewColumnIcon,
  Cloud: CloudIcon,
};

export const FallbackIcon = FolderIcon;
