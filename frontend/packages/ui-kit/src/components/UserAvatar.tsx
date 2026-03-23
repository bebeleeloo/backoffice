import { Avatar, type SxProps, type Theme } from "@mui/material";
import { useMemo } from "react";

interface UserAvatarProps {
  userId: string;
  name: string;
  hasPhoto: boolean;
  size?: number;
  sx?: SxProps<Theme>;
}

export function UserAvatar({ userId, name, hasPhoto, size = 36, sx }: UserAvatarProps) {
  const src = useMemo(
    () => (hasPhoto ? `/api/v1/users/${userId}/photo` : undefined),
    [hasPhoto, userId],
  );

  return (
    <Avatar
      src={src}
      sx={{ width: size, height: size, fontSize: size * 0.45, ...(sx ?? {}) } as SxProps<Theme>}
    >
      {(name || "U").charAt(0).toUpperCase()}
    </Avatar>
  );
}
