import type { ReactNode } from "react";
import { Box, Typography } from "@mui/material";
import { ThemeProvider } from "@mui/material/styles";
import { listTheme } from "../theme";
import { Breadcrumbs, type BreadcrumbItem } from "./Breadcrumbs";

interface PageContainerProps {
  title: string;
  actions?: ReactNode;
  /** @deprecated Use subheaderLeft / subheaderRight instead. */
  subheader?: ReactNode;
  /** Primary control (search field) — stretches to fill available width. */
  subheaderLeft?: ReactNode;
  /** Secondary controls (filters, selects) — fixed width, right-aligned. */
  subheaderRight?: ReactNode;
  children: ReactNode;
  /** Use "list" for journal/grid pages — applies compact control sizing via scoped theme. */
  variant?: "default" | "list";
  breadcrumbs?: BreadcrumbItem[];
}

export function PageContainer({
  title,
  actions,
  subheader,
  subheaderLeft,
  subheaderRight,
  children,
  variant = "default",
  breadcrumbs,
}: PageContainerProps) {
  const isList = variant === "list";
  const hasSubheader = subheader || subheaderLeft || subheaderRight;

  const content = (
    <Box
      sx={{
        display: "flex",
        flexDirection: "column",
        ...(isList
          ? { flexGrow: 1, minHeight: 0, overflow: "hidden" }
          : {}),
        width: "100%",
        px: 2,
        pt: isList ? 1 : 1.5,
        pb: isList ? 1 : 1.5,
        gap: isList ? 1 : 1.5,
      }}
    >
      {breadcrumbs && <Breadcrumbs items={breadcrumbs} />}
      <Box
        sx={{
          display: "flex",
          justifyContent: "space-between",
          alignItems: "center",
          flexShrink: 0,
          minHeight: 32,
        }}
      >
        <Typography
          variant="h6"
          component="h1"
          sx={{ lineHeight: 1.2, fontSize: isList ? "1.125rem" : undefined }}
        >
          {title}
        </Typography>
        {actions && <Box sx={{ display: "flex", gap: 1, alignItems: "center" }}>{actions}</Box>}
      </Box>
      {hasSubheader && (
        <Box sx={{ display: "flex", gap: 1.5, alignItems: "center", flexShrink: 0, flexWrap: "wrap" }}>
          {subheader}
          {subheaderLeft && (
            <Box sx={{ flexGrow: 1, minWidth: 200, display: "flex" }}>
              {subheaderLeft}
            </Box>
          )}
          {subheaderRight && (
            <Box sx={{ flexShrink: 0, display: "flex", gap: 1.5, alignItems: "center" }}>
              {subheaderRight}
            </Box>
          )}
        </Box>
      )}
      {children}
    </Box>
  );

  if (isList) {
    return <ThemeProvider theme={listTheme}>{content}</ThemeProvider>;
  }

  return content;
}
