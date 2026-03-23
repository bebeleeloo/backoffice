import { useState } from "react";
import { Tabs, Tab, Box } from "@mui/material";
import { PageContainer, useHasPermission } from "@broker/ui-kit";
import { ProfileTab } from "@broker/auth-module";
import { AppearanceTab } from "./settings/AppearanceTab";
import { ReferenceDataTab } from "./settings/ReferenceDataTab";

export function SettingsPage() {
  const canManageSettings = useHasPermission("settings.manage");
  const [tab, setTab] = useState("profile");

  return (
    <PageContainer title="Settings">
      <Tabs value={tab} onChange={(_, v) => setTab(v)} sx={{ mb: 2 }}>
        <Tab label="Profile" value="profile" />
        <Tab label="Appearance" value="appearance" />
        {canManageSettings && <Tab label="Reference Data" value="reference" />}
      </Tabs>
      <Box>
        {tab === "profile" && <ProfileTab />}
        {tab === "appearance" && <AppearanceTab />}
        {tab === "reference" && canManageSettings && <ReferenceDataTab />}
      </Box>
    </PageContainer>
  );
}
