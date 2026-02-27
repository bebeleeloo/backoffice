import { Box, Card, CardActionArea, Typography, Skeleton } from "@mui/material";
import PeopleIcon from "@mui/icons-material/People";
import AccountBalanceIcon from "@mui/icons-material/AccountBalance";
import ReceiptLongIcon from "@mui/icons-material/ReceiptLong";
import AdminPanelSettingsIcon from "@mui/icons-material/AdminPanelSettings";
import { useNavigate } from "react-router-dom";
import {
  PieChart, Pie, Cell, Tooltip, Legend, ResponsiveContainer,
  BarChart, Bar, XAxis, YAxis, CartesianGrid,
} from "recharts";
import { useDashboardStats } from "../api/hooks";
import { PageContainer } from "../components/PageContainer";
import { STAT_GRADIENTS } from "../theme";

const STATUS_COLORS: Record<string, string> = {
  Active: "#10B981",
  Blocked: "#EF4444",
  PendingKyc: "#F59E0B",
  Closed: "#6B7280",
  Suspended: "#F59E0B",
  New: "#06B6D4",
  PendingApproval: "#F59E0B",
  Approved: "#0D9488",
  Rejected: "#EF4444",
  InProgress: "#06B6D4",
  PartiallyFilled: "#F59E0B",
  Filled: "#10B981",
  Completed: "#10B981",
  Cancelled: "#6B7280",
  Failed: "#EF4444",
  Trade: "#0D9488",
  NonTrade: "#8B5CF6",
};

const TYPE_COLORS = [
  "#0D9488", "#14B8A6", "#059669", "#10B981", "#06B6D4",
  "#0891B2", "#8B5CF6", "#F59E0B", "#EF4444", "#6B7280",
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
  gradientIndex: number;
}

function StatCard({ title, total, breakdown, icon, href, loading, gradientIndex }: StatCardProps) {
  const navigate = useNavigate();
  return (
    <Card
      sx={{
        background: STAT_GRADIENTS[gradientIndex % STAT_GRADIENTS.length],
        color: "#FFFFFF",
        borderRadius: 3,
        boxShadow: "0 4px 16px rgba(13, 148, 136, 0.2)",
        transition: "transform 200ms, box-shadow 200ms",
        "&:hover": {
          transform: "translateY(-2px)",
          boxShadow: "0 8px 24px rgba(13, 148, 136, 0.3)",
        },
      }}
    >
      <CardActionArea onClick={() => navigate(href)} sx={{ p: 2.5 }}>
        <Box sx={{ display: "flex", alignItems: "center", gap: 2 }}>
          <Box sx={{ display: "flex", bgcolor: "rgba(255,255,255,0.2)", borderRadius: 2, p: 1 }}>
            {icon}
          </Box>
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Typography variant="body2" sx={{ color: "rgba(255,255,255,0.8)" }}>{title}</Typography>
            {loading ? (
              <Skeleton width={60} height={36} sx={{ bgcolor: "rgba(255,255,255,0.2)" }} />
            ) : (
              <Typography variant="h4" fontWeight={700}>{total ?? 0}</Typography>
            )}
            <Typography variant="caption" sx={{ color: "rgba(255,255,255,0.7)" }} noWrap>
              {loading ? <Skeleton width={120} sx={{ bgcolor: "rgba(255,255,255,0.2)" }} /> : breakdownText(breakdown)}
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
    <Card sx={{ p: 2, height: "100%", borderRadius: 3 }}>
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
    <Card sx={{ p: 2, height: "100%", borderRadius: 3 }}>
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
      <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", sm: "repeat(2, 1fr)", lg: "repeat(4, 1fr)" }, gap: 2, mb: 3 }}>
        <StatCard
          title="Clients"
          total={data?.totalClients}
          breakdown={data?.clientsByStatus}
          icon={<PeopleIcon sx={{ fontSize: 36 }} />}
          href="/clients"
          loading={isLoading}
          gradientIndex={0}
        />
        <StatCard
          title="Accounts"
          total={data?.totalAccounts}
          breakdown={data?.accountsByStatus}
          icon={<AccountBalanceIcon sx={{ fontSize: 36 }} />}
          href="/accounts"
          loading={isLoading}
          gradientIndex={1}
        />
        <StatCard
          title="Orders"
          total={data?.totalOrders}
          breakdown={data?.ordersByCategory}
          icon={<ReceiptLongIcon sx={{ fontSize: 36 }} />}
          href="/trade-orders"
          loading={isLoading}
          gradientIndex={2}
        />
        <StatCard
          title="Users"
          total={data?.totalUsers}
          breakdown={data ? { Active: data.activeUsers, Inactive: data.totalUsers - data.activeUsers } : undefined}
          icon={<AdminPanelSettingsIcon sx={{ fontSize: 36 }} />}
          href="/users"
          loading={isLoading}
          gradientIndex={3}
        />
      </Box>

      <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", md: "1fr 1fr" }, gap: 2 }}>
        <StatusPie title="Clients by Status" data={data?.clientsByStatus} loading={isLoading} />
        <StatusPie title="Accounts by Status" data={data?.accountsByStatus} loading={isLoading} />
        <StatusPie title="Orders by Status" data={data?.ordersByStatus} loading={isLoading} />
        <TypeBar title="Orders by Category" data={data?.ordersByCategory} loading={isLoading} />
      </Box>
    </PageContainer>
  );
}
