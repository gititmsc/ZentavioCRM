import { useEffect, useState } from "react";
import { userService } from "@/services/userService";

function initialsOf(fullName: string): string {
  const parts = fullName.trim().split(/\s+/);
  return parts
    .slice(0, 2)
    .map((part) => part[0]?.toUpperCase() ?? "")
    .join("");
}

interface UserAvatarProps {
  userId: string;
  fullName: string;
  /** Pass false to skip the photo fetch entirely (known-no-photo, e.g. from a UserDto's hasProfilePhoto). Omit/true attempts the fetch and falls back to initials on 404/error. */
  hasProfilePhoto?: boolean;
  size?: number;
  className?: string;
  /** Bump this (e.g. AuthContext's avatarVersion) to force a refetch after the photo changes elsewhere — userId/fullName alone won't change when only the photo bytes do. */
  version?: number | string;
}

/** Renders a user's uploaded avatar (fetched as an authenticated blob, since <img src> can't carry the Bearer token), or an initials circle if they have none. */
export function UserAvatar({ userId, fullName, hasProfilePhoto, size = 36, className, version }: UserAvatarProps) {
  const [photoUrl, setPhotoUrl] = useState<string | null>(null);

  useEffect(() => {
    if (hasProfilePhoto === false) {
      setPhotoUrl(null);
      return;
    }

    let objectUrl: string | null = null;
    let cancelled = false;

    (async () => {
      const url = await userService.getPhotoBlobUrl(userId);
      if (cancelled) {
        if (url) URL.revokeObjectURL(url);
        return;
      }
      objectUrl = url;
      setPhotoUrl(url);
    })();

    return () => {
      cancelled = true;
      if (objectUrl) URL.revokeObjectURL(objectUrl);
    };
  }, [userId, hasProfilePhoto, version]);

  const style = { width: size, height: size, minWidth: size, fontSize: Math.max(11, size * 0.4) };

  if (photoUrl) {
    return (
      <img
        src={photoUrl}
        alt={fullName}
        className={`rounded-circle ${className ?? ""}`}
        style={{ ...style, objectFit: "cover" }}
      />
    );
  }

  return (
    <div
      className={`rounded-circle bg-secondary-subtle text-secondary-emphasis d-flex align-items-center justify-content-center fw-semibold ${className ?? ""}`}
      style={style}
    >
      {initialsOf(fullName)}
    </div>
  );
}
