import { useState } from "react";
import { Tabs, Tab, Box } from "@mui/material";
import { PageContainer } from "../components/PageContainer";
import { ProfileTab } from "./settings/ProfileTab";
import { ReferenceDataTab } from "./settings/ReferenceDataTab";
import { useHasPermission } from "../auth/usePermission";

export function SettingsPage() {
  const canManageSettings = useHasPermission("settings.manage");
  const [tab, setTab] = useState(0);

  return (
    <PageContainer title="Settings">
      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 2 }}>
        <Tab label="Profile" />
        {canManageSettings && <Tab label="Reference Data" />}
      </Tabs>
      <Box>
        {tab === 0 && <ProfileTab />}
        {tab === 1 && canManageSettings && <ReferenceDataTab />}
      </Box>
    </PageContainer>
  );
}
