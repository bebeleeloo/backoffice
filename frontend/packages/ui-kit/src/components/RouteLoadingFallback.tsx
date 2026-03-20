import { Box, CircularProgress } from "@mui/material";

export function RouteLoadingFallback() {
  return (
    <Box sx={{ display: "flex", justifyContent: "center", alignItems: "center", height: "100%", minHeight: 200 }}>
      <CircularProgress />
    </Box>
  );
}
