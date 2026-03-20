// Theme
export { createAppTheme, createAppListTheme, SIDEBAR_COLORS, STAT_GRADIENTS } from "./theme";
export { AppThemeProvider, useThemeMode, useListTheme, ListThemeContext } from "./theme/ThemeContext";
export type { ThemePreference } from "./theme/ThemeContext";

// Auth
export { AuthProvider } from "./auth/AuthContext";
export { AuthContext } from "./auth/context";
export type { AuthState } from "./auth/context";
export { useAuth } from "./auth/useAuth";
export { useHasPermission } from "./auth/usePermission";
export { RequireAuth } from "./auth/RequireAuth";

// API
export { apiClient } from "./api/client";
export { cleanParams, useEntityChanges, useAllEntityChanges } from "./api/hooks";
export { useMenu } from "./api/configApi";
export type { MenuItem } from "./api/configApi";

// Icons
export { iconMap, FallbackIcon } from "./icons";
export type {
  AuthResponse,
  UserProfile,
  PagedResult,
  PagedParams,
  CountryDto,
  FieldChangeDto,
  EntityChangeGroupDto,
  OperationDto,
  EntityChangesParams,
  GlobalOperationDto,
  AllEntityChangesParams,
  AuditLogDto,
  AuditParams,
} from "./api/types";

// Components
export { PageContainer } from "./components/PageContainer";
export { Breadcrumbs } from "./components/Breadcrumbs";
export type { BreadcrumbItem } from "./components/Breadcrumbs";
export { DetailField } from "./components/DetailField";
export { ConfirmDialog } from "./components/ConfirmDialog";
export { UserAvatar } from "./components/UserAvatar";
export { ErrorBoundary } from "./components/ErrorBoundary";
export { ExportButton } from "./components/ExportButton";
export { GlobalSearchBar } from "./components/GlobalSearchBar";
export { RouteLoadingFallback } from "./components/RouteLoadingFallback";
export { EntityHistoryDialog } from "./components/EntityHistoryDialog";
export { AuditDetailDialog } from "./components/AuditDetailDialog";
export { FieldRow, ChangeGroup } from "./components/ChangeHistoryComponents";
export { CHANGE_TYPE_COLORS, getFieldLabel, getEntityTypeLabel } from "./components/changeHistoryUtils";

// Grid
export {
  FilteredDataGrid,
  FilterRowProvider,
  CustomColumnHeaders,
  InlineTextFilter,
  CompactMultiSelect,
  CompactCountrySelect,
  DateRangePopover,
  NumericRangePopover,
  InlineBooleanFilter,
} from "./components/grid";

// Hooks
export { useDebounce } from "./hooks/useDebounce";
export { useConfirm } from "./hooks/useConfirm";

// Utils
export { exportToExcel } from "./utils/exportToExcel";
export type { ExcelColumn } from "./utils/exportToExcel";
export { extractErrorMessage } from "./utils/extractErrorMessage";
export { validateRequired, validateEmail } from "./utils/validateFields";
export type { FieldErrors } from "./utils/validateFields";

// Navigation
export { NavigationProvider } from "./navigation/NavigationContext";
export { useAppNavigation } from "./navigation/useAppNavigation";

// Layouts
export { MainLayout } from "./layouts/MainLayout";
