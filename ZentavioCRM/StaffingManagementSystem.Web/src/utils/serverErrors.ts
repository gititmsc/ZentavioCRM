/**
 * Maps a server-returned field-error dictionary (ApiResponse<T>.fieldErrors — see apiHelpers.ts)
 * onto react-hook-form's setError, so the offending field gets its own red border + inline
 * message instead of only a generic top-of-form banner.
 */
import type { FieldValues, Path, UseFormSetError } from "react-hook-form";

/**
 * ASP.NET Core's error-dictionary keys aren't guaranteed to match this app's camelCase field
 * names exactly or consistently — plain DataAnnotations failures and JSON-deserialization
 * exceptions can key errors differently, and a nested/complex property would arrive as a
 * JSONPath-ish "$.budget" rather than a bare "budget". Matching is therefore case-insensitive
 * and takes only the last "."-separated segment after stripping a leading "$".
 *
 * Returns the set of this form's fields that were actually matched and highlighted, so the
 * caller can still surface anything left over (a field the form doesn't know about) as a banner
 * instead of silently dropping it.
 */
export function applyServerFieldErrors<T extends FieldValues>(
  setError: UseFormSetError<T>,
  fieldErrors: Record<string, string[]> | undefined,
  knownFields: readonly (keyof T & string)[]
): Set<string> {
  const matched = new Set<string>();
  if (!fieldErrors) return matched;

  const byLowerName = new Map(knownFields.map((field) => [field.toLowerCase(), field]));

  for (const [rawKey, messages] of Object.entries(fieldErrors)) {
    if (!messages || messages.length === 0) continue;

    const normalizedKey = rawKey.replace(/^\$\.?/, "").split(".").pop() ?? rawKey;
    const field = byLowerName.get(normalizedKey.toLowerCase());
    if (!field) continue;

    setError(field as Path<T>, { type: "server", message: messages[0] });
    matched.add(field);
  }

  return matched;
}

/** True if every entry in fieldErrors was successfully mapped onto a known form field — i.e. nothing needs to fall back to a generic banner. */
export function allServerFieldErrorsMatched(
  fieldErrors: Record<string, string[]> | undefined,
  matched: Set<string>
): boolean {
  if (!fieldErrors) return true;
  return Object.keys(fieldErrors).length > 0 && matched.size >= Object.keys(fieldErrors).length;
}
