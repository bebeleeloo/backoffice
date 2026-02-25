import { Card, CardContent, ToggleButton, ToggleButtonGroup, Typography } from "@mui/material";
import LightModeIcon from "@mui/icons-material/LightMode";
import DarkModeIcon from "@mui/icons-material/DarkMode";
import SettingsBrightnessIcon from "@mui/icons-material/SettingsBrightness";
import { useThemeMode, type ThemePreference } from "../../theme/ThemeContext";

export function AppearanceTab() {
  const { preference, setPreference } = useThemeMode();

  return (
    <Card variant="outlined">
      <CardContent>
        <Typography variant="subtitle1" gutterBottom>Theme</Typography>
        <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
          Choose how the application looks. Select "System" to automatically match your operating system setting.
        </Typography>
        <ToggleButtonGroup
          value={preference}
          exclusive
          onChange={(_, value: ThemePreference | null) => {
            if (value) setPreference(value);
          }}
        >
          <ToggleButton value="light" sx={{ px: 3, gap: 1 }}>
            <LightModeIcon fontSize="small" />
            Light
          </ToggleButton>
          <ToggleButton value="dark" sx={{ px: 3, gap: 1 }}>
            <DarkModeIcon fontSize="small" />
            Dark
          </ToggleButton>
          <ToggleButton value="system" sx={{ px: 3, gap: 1 }}>
            <SettingsBrightnessIcon fontSize="small" />
            System
          </ToggleButton>
        </ToggleButtonGroup>
      </CardContent>
    </Card>
  );
}
