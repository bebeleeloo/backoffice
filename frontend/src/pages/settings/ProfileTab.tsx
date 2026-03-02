import { useRef, useState } from "react";
import {
  Box, Button, Card, CardContent, Chip, TextField, Typography,
} from "@mui/material";
import PhotoCameraIcon from "@mui/icons-material/PhotoCamera";
import DeleteIcon from "@mui/icons-material/Delete";
import { enqueueSnackbar } from "notistack";
import { useAuth } from "../../auth/useAuth";
import { useChangePassword, useUpdateProfile, useUploadMyPhoto, useDeleteMyPhoto } from "../../api/hooks";
import { UserAvatar } from "../../components/UserAvatar";
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
  const uploadPhoto = useUploadMyPhoto();
  const deletePhoto = useDeleteMyPhoto();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handlePhotoUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    if (file.size > 2 * 1024 * 1024) {
      enqueueSnackbar("Photo must be 2 MB or less", { variant: "error" });
      return;
    }
    try {
      await uploadPhoto.mutateAsync(file);
      await refreshProfile();
    } catch { /* handled by MutationCache */ }
    if (fileInputRef.current) fileInputRef.current.value = "";
  };

  const handlePhotoDelete = async () => {
    try {
      await deletePhoto.mutateAsync();
      await refreshProfile();
    } catch { /* handled by MutationCache */ }
  };

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
        {/* Photo */}
        <Card variant="outlined">
          <CardContent sx={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 1.5 }}>
            <UserAvatar
              userId={user?.id ?? ""}
              name={user?.fullName || user?.username || "U"}
              hasPhoto={user?.hasPhoto ?? false}
              size={96}
            />
            <Box sx={{ display: "flex", gap: 1 }}>
              <Button
                size="small"
                variant="outlined"
                startIcon={<PhotoCameraIcon />}
                onClick={() => fileInputRef.current?.click()}
                disabled={uploadPhoto.isPending}
              >
                Change Photo
              </Button>
              {user?.hasPhoto && (
                <Button
                  size="small"
                  color="error"
                  startIcon={<DeleteIcon />}
                  onClick={handlePhotoDelete}
                  disabled={deletePhoto.isPending}
                >
                  Remove
                </Button>
              )}
            </Box>
            <input
              ref={fileInputRef}
              type="file"
              accept="image/jpeg,image/png,image/gif,image/webp"
              hidden
              onChange={handlePhotoUpload}
            />
          </CardContent>
        </Card>

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
