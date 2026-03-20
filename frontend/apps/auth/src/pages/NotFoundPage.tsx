import { Box, Typography, Button } from "@mui/material";

export function NotFoundPage() {
  return (
    <Box
      sx={{
        display: "flex",
        flexDirection: "column",
        alignItems: "center",
        justifyContent: "center",
        flexGrow: 1,
        gap: 2,
        p: 3,
      }}
    >
      <Typography variant="h1" sx={{ fontSize: "6rem", fontWeight: 700, color: "text.secondary" }}>
        404
      </Typography>
      <Typography variant="h5" color="text.secondary">
        Page not found
      </Typography>
      <Button variant="contained" onClick={() => (window.location.href = "/")}>
        Go to Dashboard
      </Button>
    </Box>
  );
}
