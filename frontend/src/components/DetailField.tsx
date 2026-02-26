import type { ReactNode } from "react";
import { Box, Typography } from "@mui/material";

export function DetailField({ label, value }: { label: string; value: ReactNode }) {
  if (value === null || value === undefined || value === "") return null;
  return (
    <Box sx={{ minWidth: { xs: 140, sm: 180 } }}>
      <Typography variant="caption" color="text.secondary">{label}</Typography>
      <Typography variant="body2">{value}</Typography>
    </Box>
  );
}
