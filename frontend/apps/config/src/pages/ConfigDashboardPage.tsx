import { Box, Card, CardContent, Typography, Button, CircularProgress } from "@mui/material";
import RefreshIcon from "@mui/icons-material/Refresh";
import MenuBookIcon from "@mui/icons-material/MenuBook";
import ViewColumnIcon from "@mui/icons-material/ViewColumn";
import CloudIcon from "@mui/icons-material/Cloud";
import { PageContainer, useAppNavigation } from "@broker/ui-kit";
import { useMenuRaw, useEntitiesRaw, useUpstreams, useReloadConfig } from "../api/hooks";

export function ConfigDashboardPage() {
  const { data: menu } = useMenuRaw();
  const { data: entities } = useEntitiesRaw();
  const { data: upstreams } = useUpstreams();
  const reloadConfig = useReloadConfig();
  const { navigateTo } = useAppNavigation();

  const countMenuItems = (items: typeof menu): number => {
    if (!items) return 0;
    return items.reduce((acc, item) => acc + 1 + countMenuItems(item.children ?? []), 0);
  };

  const stats = [
    { label: "Menu Items", count: countMenuItems(menu), icon: <MenuBookIcon sx={{ fontSize: 40 }} />, path: "/config/menu" },
    { label: "Entities", count: entities?.length ?? 0, icon: <ViewColumnIcon sx={{ fontSize: 40 }} />, path: "/config/entities" },
    { label: "Upstreams", count: upstreams ? Object.keys(upstreams).length : 0, icon: <CloudIcon sx={{ fontSize: 40 }} />, path: "/config/upstreams" },
  ];

  return (
    <PageContainer
      title="Configuration"
      actions={
        <Button
          variant="contained"
          startIcon={reloadConfig.isPending ? <CircularProgress size={18} color="inherit" /> : <RefreshIcon />}
          onClick={() => reloadConfig.mutate()}
          disabled={reloadConfig.isPending}
        >
          Reload Config
        </Button>
      }
    >
      <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "repeat(3, 1fr)" }, gap: 3 }}>
        {stats.map((stat) => (
          <Card
            key={stat.label}
            sx={{ cursor: "pointer", "&:hover": { boxShadow: 4 }, transition: "box-shadow 0.2s" }}
            onClick={() => navigateTo(stat.path)}
          >
            <CardContent sx={{ display: "flex", alignItems: "center", gap: 2, p: 3 }}>
              <Box sx={{ color: "primary.main" }}>{stat.icon}</Box>
              <Box>
                <Typography variant="h4" fontWeight={700}>{stat.count}</Typography>
                <Typography variant="body2" color="text.secondary">{stat.label}</Typography>
              </Box>
            </CardContent>
          </Card>
        ))}
      </Box>
    </PageContainer>
  );
}
