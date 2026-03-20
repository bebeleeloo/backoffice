import { useState } from "react";
import { Tabs, Tab, Box } from "@mui/material";
import { PageContainer, useHasPermission } from "@broker/ui-kit";
import { ProfileTab } from "@broker/auth-module";
import { AppearanceTab } from "./settings/AppearanceTab";
import { ReferenceDataTab } from "./settings/ReferenceDataTab";

export function SettingsPage() {
  const canManageSettings = useHasPermission("settings.manage");
  const [tab, setTab] = useState(0);

  return (
    <PageContainer title="Settings">
      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 2 }}>
        <Tab label="Profile" />
        <Tab label="Appearance" />
        {canManageSettings && <Tab label="Reference Data" />}
      </Tabs>
      <Box>
        {tab === 0 && <ProfileTab />}
        {tab === 1 && <AppearanceTab />}
        {tab === 2 && canManageSettings && <ReferenceDataTab />}
      </Box>
    </PageContainer>
  );
}
