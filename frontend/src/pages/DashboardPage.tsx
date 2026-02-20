import { Typography, Paper } from "@mui/material";
import { PageContainer } from "../components/PageContainer";

export function DashboardPage() {
  return (
    <PageContainer title="Dashboard">
      <Paper sx={{ p: 3 }}>
        <Typography color="text.secondary">
          Broker Backoffice is running. Add business entity pages here.
        </Typography>
      </Paper>
    </PageContainer>
  );
}
