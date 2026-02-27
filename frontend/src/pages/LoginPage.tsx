import { useState } from "react";
import { useNavigate, useLocation } from "react-router-dom";
import { Alert, Box, Button, TextField, Typography } from "@mui/material";
import { useAuth } from "../auth/useAuth";

export function LoginPage() {
  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const from = (location.state as { from?: { pathname: string } })?.from?.pathname || "/";

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError("");
    setLoading(true);
    try {
      await login(username, password);
      navigate(from, { replace: true });
    } catch (err: unknown) {
      const msg =
        (err as { response?: { data?: { title?: string } } })?.response?.data?.title
        || (err as Error)?.message
        || "Invalid username or password";
      setError(msg);
    } finally {
      setLoading(false);
    }
  };

  return (
    <Box sx={{ display: "flex", minHeight: "100vh" }}>
      {/* Left branding panel — hidden on mobile */}
      <Box
        sx={{
          display: { xs: "none", md: "flex" },
          flexDirection: "column",
          justifyContent: "center",
          alignItems: "center",
          width: "45%",
          background: "linear-gradient(135deg, #0F172A 0%, #0D9488 100%)",
          position: "relative",
          overflow: "hidden",
          px: 6,
          "&::before": {
            content: '""',
            position: "absolute",
            top: -100,
            right: -100,
            width: 400,
            height: 400,
            borderRadius: "50%",
            background: "rgba(13, 148, 136, 0.15)",
          },
          "&::after": {
            content: '""',
            position: "absolute",
            bottom: -50,
            left: -50,
            width: 300,
            height: 300,
            borderRadius: "50%",
            background: "rgba(5, 150, 105, 0.1)",
          },
        }}
      >
        <Box component="img" src="/logo.svg" alt="Logo" sx={{ width: 80, height: 80, mb: 3, position: "relative", zIndex: 1 }} />
        <Typography variant="h3" sx={{ color: "#FFFFFF", fontWeight: 700, mb: 1, position: "relative", zIndex: 1, textAlign: "center" }}>
          Broker Backoffice
        </Typography>
        <Typography variant="h6" sx={{ color: "rgba(255,255,255,0.7)", fontWeight: 400, textAlign: "center", position: "relative", zIndex: 1 }}>
          Internal Management System
        </Typography>
      </Box>

      {/* Right form panel */}
      <Box
        sx={{
          flex: 1,
          display: "flex",
          flexDirection: "column",
          justifyContent: "center",
          alignItems: "center",
          px: { xs: 3, sm: 6 },
          bgcolor: "background.default",
        }}
      >
        {/* Mobile-only logo */}
        <Box sx={{ display: { xs: "flex", md: "none" }, flexDirection: "column", alignItems: "center", mb: 4 }}>
          <Box component="img" src="/logo.svg" alt="Logo" sx={{ width: 56, height: 56, mb: 1 }} />
          <Typography variant="h5" fontWeight={700}>Broker Backoffice</Typography>
        </Box>

        <Box sx={{ maxWidth: 400, width: "100%" }}>
          <Typography variant="h4" fontWeight={700} gutterBottom>
            Welcome back
          </Typography>
          <Typography variant="body1" color="text.secondary" sx={{ mb: 4 }}>
            Sign in to your account to continue
          </Typography>
          {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
          <Box component="form" onSubmit={handleSubmit}>
            <TextField fullWidth label="Username" value={username} onChange={(e) => setUsername(e.target.value)} margin="normal" autoFocus />
            <TextField fullWidth label="Password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} margin="normal" />
            <Button
              fullWidth type="submit" variant="contained" size="large"
              sx={{ mt: 3, py: 1.5, fontSize: "1rem", fontWeight: 600 }}
              disabled={loading || !username || !password}
            >
              {loading ? "Signing in..." : "Sign In"}
            </Button>
          </Box>
        </Box>
      </Box>
    </Box>
  );
}
