import { Box, Card, CardActionArea, Typography, Skeleton } from "@mui/material";
import PeopleIcon from "@mui/icons-material/People";
import AccountBalanceIcon from "@mui/icons-material/AccountBalance";
import ShowChartIcon from "@mui/icons-material/ShowChart";
import AdminPanelSettingsIcon from "@mui/icons-material/AdminPanelSettings";
import { useNavigate } from "react-router-dom";
import {
  PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer,
  BarChart, Bar, XAxis, YAxis, CartesianGrid,
} from "recharts";
import { useDashboardStats } from "../api/hooks";
import { PageContainer } from "../components/PageContainer";

const STATUS_COLORS: Record<string, string> = {
  Active: "#4caf50",
  Blocked: "#f44336",
  PendingKyc: "#ff9800",
  Closed: "#9e9e9e",
  Suspended: "#ff9800",
  Inactive: "#9e9e9e",
  Delisted: "#f44336",
};

const TYPE_COLORS = [
  "#1565c0", "#42a5f5", "#0d47a1", "#5c6bc0", "#7e57c2",
  "#26a69a", "#66bb6a", "#ffa726", "#ef5350", "#8d6e63",
];

function toChartData(map: Record<string, number> | undefined) {
  if (!map) return [];
  return Object.entries(map).map(([name, value]) => ({ name, value }));
}

function breakdownText(map: Record<string, number> | undefined) {
  if (!map) return "";
  return Object.entries(map)
    .map(([k, v]) => `${v} ${k.replace(/([a-z])([A-Z])/g, "$1 $2")}`)
    .join(", ");
}

interface StatCardProps {
  title: string;
  total: number | undefined;
  breakdown: Record<string, number> | undefined;
  icon: React.ReactNode;
  href: string;
  loading: boolean;
}

function StatCard({ title, total, breakdown, icon, href, loading }: StatCardProps) {
  const navigate = useNavigate();
  return (
    <Card variant="outlined">
      <CardActionArea onClick={() => navigate(href)} sx={{ p: 2.5 }}>
        <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
          <Box sx={{ color: "primary.main", display: "flex" }}>{icon}</Box>
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Typography variant="body2" color="text.secondary">{title}</Typography>
            {loading ? (
              <Skeleton width={60} height={36} />
            ) : (
              <Typography variant="h4" fontWeight={600}>{total ?? 0}</Typography>
            )}
            <Typography variant="caption" color="text.secondary" noWrap>
              {loading ? <Skeleton width={120} /> : breakdownText(breakdown)}
            </Typography>
          </Box>
        </Box>
      </CardActionArea>
    </Card>
  );
}

interface StatusPieProps {
  title: string;
  data: Record<string, number> | undefined;
  loading: boolean;
}

function StatusPie({ title, data, loading }: StatusPieProps) {
  const chartData = toChartData(data);
  return (
    <Card variant="outlined" sx={{ p: 2, height: "100%" }}>
      <Typography variant="subtitle2" gutterBottom>{title}</Typography>
      {loading ? (
        <Skeleton variant="circular" width={180} height={180} sx={{ mx: "auto", my: 2 }} />
      ) : chartData.length === 0 ? (
        <Typography color="text.secondary" sx={{ textAlign: "center", py: 8 }}>No data</Typography>
      ) : (
        <ResponsiveContainer width="100%" height={240}>
          <PieChart>
            <Pie
              data={chartData}
              dataKey="value"
              nameKey="name"
              cx="50%"
              cy="50%"
              outerRadius={80}
              label={({ name, percent }) => `${name} ${((percent ?? 0) * 100).toFixed(0)}%`}
              labelLine={false}
              fontSize={12}
            >
              {chartData.map((entry) => (
                <Cell key={entry.name} fill={STATUS_COLORS[entry.name] ?? "#90a4ae"} />
              ))}
            </Pie>
            <Tooltip />
            <Legend />
          </PieChart>
        </ResponsiveContainer>
      )}
    </Card>
  );
}

interface TypeBarProps {
  title: string;
  data: Record<string, number> | undefined;
  loading: boolean;
}

function TypeBar({ title, data, loading }: TypeBarProps) {
  const chartData = toChartData(data);
  return (
    <Card variant="outlined" sx={{ p: 2, height: "100%" }}>
      <Typography variant="subtitle2" gutterBottom>{title}</Typography>
      {loading ? (
        <Skeleton variant="rectangular" height={220} sx={{ my: 1 }} />
      ) : chartData.length === 0 ? (
        <Typography color="text.secondary" sx={{ textAlign: "center", py: 8 }}>No data</Typography>
      ) : (
        <ResponsiveContainer width="100%" height={240}>
          <BarChart data={chartData} margin={{ top: 5, right: 20, bottom: 5, left: 0 }}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis dataKey="name" fontSize={11} interval={0} angle={-30} textAnchor="end" height={60} />
            <YAxis allowDecimals={false} fontSize={12} />
            <Tooltip />
            <Bar dataKey="value" radius={[4, 4, 0, 0]}>
              {chartData.map((_, i) => (
                <Cell key={i} fill={TYPE_COLORS[i % TYPE_COLORS.length]} />
              ))}
            </Bar>
          </BarChart>
        </ResponsiveContainer>
      )}
    </Card>
  );
}

export function DashboardPage() {
  const { data, isLoading } = useDashboardStats();

  return (
    <PageContainer title="Dashboard">
      <Box sx={{ display: "grid", gridTemplateColumns: "repeat(4, 1fr)", gap: 2, mb: 3 }}>
        <StatCard
          title="Clients"
          total={data?.totalClients}
          breakdown={data?.clientsByStatus}
          icon={<PeopleIcon sx={{ fontSize: 36 }} />}
          href="/clients"
          loading={isLoading}
        />
        <StatCard
          title="Accounts"
          total={data?.totalAccounts}
          breakdown={data?.accountsByStatus}
          icon={<AccountBalanceIcon sx={{ fontSize: 36 }} />}
          href="/accounts"
          loading={isLoading}
        />
        <StatCard
          title="Instruments"
          total={data?.totalInstruments}
          breakdown={data?.instrumentsByStatus}
          icon={<ShowChartIcon sx={{ fontSize: 36 }} />}
          href="/instruments"
          loading={isLoading}
        />
        <StatCard
          title="Users"
          total={data?.totalUsers}
          breakdown={data ? { Active: data.activeUsers, Inactive: data.totalUsers - data.activeUsers } : undefined}
          icon={<AdminPanelSettingsIcon sx={{ fontSize: 36 }} />}
          href="/users"
          loading={isLoading}
        />
      </Box>

      <Box sx={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 2 }}>
        <StatusPie title="Clients by Status" data={data?.clientsByStatus} loading={isLoading} />
        <StatusPie title="Accounts by Status" data={data?.accountsByStatus} loading={isLoading} />
        <TypeBar title="Instruments by Type" data={data?.instrumentsByType} loading={isLoading} />
        <StatusPie title="Instruments by Status" data={data?.instrumentsByStatus} loading={isLoading} />
      </Box>
    </PageContainer>
  );
}
