// A small, fixed palette of on-brand gradients — picked deterministically from the name so the
// same tenant/admin always gets the same color, without needing to store one.
const GRADIENTS = [
  "linear-gradient(135deg, #2563eb, #0e7490)",
  "linear-gradient(135deg, #7c3aed, #2563eb)",
  "linear-gradient(135deg, #0e7490, #16a34a)",
  "linear-gradient(135deg, #d97706, #dc2626)",
  "linear-gradient(135deg, #0f2345, #2563eb)",
  "linear-gradient(135deg, #be185d, #7c3aed)",
];

function hashString(value: string): number {
  let hash = 0;
  for (let i = 0; i < value.length; i++) {
    hash = (hash << 5) - hash + value.charCodeAt(i);
    hash |= 0;
  }
  return Math.abs(hash);
}

function getInitials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "?";
  if (parts.length === 1) return parts[0].slice(0, 2).toUpperCase();
  return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
}

interface AvatarProps {
  name: string;
  size?: number;
}

/** Initials avatar with a deterministic gradient — used anywhere a tenant or admin needs a
 * visual anchor in a list without a real uploaded photo. */
export function Avatar({ name, size = 38 }: AvatarProps) {
  const gradient = GRADIENTS[hashString(name) % GRADIENTS.length];
  return (
    <span
      className="avatar-chip"
      style={{ width: size, height: size, background: gradient, fontSize: size * 0.38 }}
    >
      {getInitials(name)}
    </span>
  );
}
