import { Typography, Paper } from "@mui/material";
import { PageContainer } from "../components/PageContainer";

export function SettingsPage() {
  return (
    <PageContainer title="Settings">
      <Paper sx={{ p: 3 }}>
        <Typography color="text.secondary">
          Application settings will appear here.
        </Typography>
      </Paper>
    </PageContainer>
  );
}
