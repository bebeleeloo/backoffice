import { useState } from "react";
import {
  Alert, Box, Button, Card, CardContent, Chip, TextField, Typography,
} from "@mui/material";
import { useAuth } from "../../auth/useAuth";
import { useChangePassword, useUpdateProfile } from "../../api/hooks";

export function ProfileTab() {
  const { user, refreshProfile } = useAuth();

  const [fullName, setFullName] = useState(user?.fullName ?? "");
  const [email, setEmail] = useState(user?.email ?? "");
  const [profileMsg, setProfileMsg] = useState<{ type: "success" | "error"; text: string } | null>(null);

  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [pwdMsg, setPwdMsg] = useState<{ type: "success" | "error"; text: string } | null>(null);

  const updateProfile = useUpdateProfile();
  const changePassword = useChangePassword();

  const handleProfileSave = async () => {
    setProfileMsg(null);
    try {
      await updateProfile.mutateAsync({ fullName: fullName || undefined, email });
      await refreshProfile();
      setProfileMsg({ type: "success", text: "Profile updated" });
    } catch (e: unknown) {
      const msg = e instanceof Error ? e.message : "Failed to update profile";
      setProfileMsg({ type: "error", text: msg });
    }
  };

  const handlePasswordChange = async () => {
    setPwdMsg(null);
    if (newPassword !== confirmPassword) {
      setPwdMsg({ type: "error", text: "Passwords do not match" });
      return;
    }
    try {
      await changePassword.mutateAsync({ currentPassword, newPassword });
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
      setPwdMsg({ type: "success", text: "Password changed successfully" });
    } catch {
      setPwdMsg({ type: "error", text: "Failed to change password. Check your current password." });
    }
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
            {profileMsg && <Alert severity={profileMsg.type} sx={{ mb: 2 }}>{profileMsg.text}</Alert>}
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
                onChange={(e) => setEmail(e.target.value)}
                size="small"
                required
              />
              <Button
                variant="contained"
                onClick={handleProfileSave}
                disabled={updateProfile.isPending || !email}
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
          {pwdMsg && <Alert severity={pwdMsg.type} sx={{ mb: 2 }}>{pwdMsg.text}</Alert>}
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
