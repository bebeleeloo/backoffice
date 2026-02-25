import { useState } from "react";
import {
  Box, Button, Card, CardContent, Chip, TextField, Typography,
} from "@mui/material";
import { enqueueSnackbar } from "notistack";
import { useAuth } from "../../auth/useAuth";
import { useChangePassword, useUpdateProfile } from "../../api/hooks";
import { validateEmail, type FieldErrors } from "../../utils/validateFields";

export function ProfileTab() {
  const { user, refreshProfile } = useAuth();

  const [fullName, setFullName] = useState(user?.fullName ?? "");
  const [email, setEmail] = useState(user?.email ?? "");
  const [errors, setErrors] = useState<FieldErrors>({});

  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");

  const updateProfile = useUpdateProfile();
  const changePassword = useChangePassword();

  const handleProfileSave = async () => {
    const errs: FieldErrors = { email: validateEmail(email) };
    if (Object.values(errs).some(Boolean)) { setErrors(errs); return; }
    try {
      await updateProfile.mutateAsync({ fullName: fullName || undefined, email });
      await refreshProfile();
    } catch { /* handled by MutationCache */ }
  };

  const handlePasswordChange = async () => {
    if (newPassword !== confirmPassword) {
      enqueueSnackbar("Passwords do not match", { variant: "error" });
      return;
    }
    try {
      await changePassword.mutateAsync({ currentPassword, newPassword });
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
    } catch { /* handled by MutationCache */ }
  };

  return (
    <Box sx={{ display: "grid", gridTemplateColumns: { xs: "1fr", md: "1fr 1fr" }, gap: 2 }}>
      {/* Left column */}
      <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
        {/* Profile Info */}
        <Card variant="outlined">
          <CardContent>
            <Typography variant="subtitle1" fontWeight={600} gutterBottom>Profile Info</Typography>
            <Box sx={{ display: "grid", gridTemplateColumns: "120px 1fr", gap: 1, alignItems: "center" }}>
              <Typography variant="body2" color="text.secondary">Username</Typography>
              <Typography variant="body2">{user?.username}</Typography>
              <Typography variant="body2" color="text.secondary">Roles</Typography>
              <Box sx={{ display: "flex", gap: 0.5, flexWrap: "wrap" }}>
                {user?.roles.map((r) => <Chip key={r} label={r} size="small" />)}
              </Box>
            </Box>
          </CardContent>
        </Card>

        {/* Edit Profile */}
        <Card variant="outlined">
          <CardContent>
            <Typography variant="subtitle1" fontWeight={600} gutterBottom>Edit Profile</Typography>
            <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
              <TextField
                label="Full Name"
                value={fullName}
                onChange={(e) => setFullName(e.target.value)}
                size="small"
              />
              <TextField
                label="Email"
                value={email}
                onChange={(e) => { setEmail(e.target.value); setErrors((prev) => ({ ...prev, email: undefined })); }}
                size="small"
                required
                error={!!errors.email}
                helperText={errors.email}
              />
              <Button
                variant="contained"
                onClick={handleProfileSave}
                disabled={updateProfile.isPending}
                sx={{ alignSelf: "flex-start" }}
              >
                Save
              </Button>
            </Box>
          </CardContent>
        </Card>
      </Box>

      {/* Right column */}
      <Card variant="outlined" sx={{ alignSelf: "flex-start" }}>
        <CardContent>
          <Typography variant="subtitle1" fontWeight={600} gutterBottom>Change Password</Typography>
          <Box sx={{ display: "flex", flexDirection: "column", gap: 2 }}>
            <TextField
              label="Current Password"
              type="password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              size="small"
            />
            <TextField
              label="New Password"
              type="password"
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              size="small"
            />
            <TextField
              label="Confirm New Password"
              type="password"
              value={confirmPassword}
              onChange={(e) => setConfirmPassword(e.target.value)}
              size="small"
            />
            <Button
              variant="contained"
              onClick={handlePasswordChange}
              disabled={changePassword.isPending || !currentPassword || !newPassword || !confirmPassword}
              sx={{ alignSelf: "flex-start" }}
            >
              Change Password
            </Button>
          </Box>
        </CardContent>
      </Card>
    </Box>
  );
}
