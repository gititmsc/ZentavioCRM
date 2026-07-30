namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// A long-lived credential that lets the frontend silently obtain a new short-lived JWT access
    /// token (see <see cref="Configuration.JwtSettings.AccessTokenExpiryMinutes"/>, 15 minutes) without
    /// forcing the user to log in again every time it expires. Only the SHA-256 hash of the raw token
    /// is ever stored — the raw value is returned to the client once, at issue time, and never
    /// persisted anywhere server-side (same principle as a password hash).
    ///
    /// Rotated on every use: <see cref="Services.IAuthService"/>.RefreshAsync issues a brand new
    /// refresh token and revokes this one (stamping <see cref="RevokedAtUtc"/> and
    /// <see cref="ReplacedByTokenHash"/>), so a refresh token is single-use. Presenting an
    /// already-revoked or expired token is rejected the same way as any unrecognized token —
    /// <see cref="ReplacedByTokenHash"/> records the lineage for forensics/support, but no
    /// automatic action (e.g. revoking the whole chain) is taken on reuse; that would need to be
    /// added deliberately if theft-response behavior is required.
    /// </summary>
    public class RefreshToken
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string TokenHash { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        /// <summary>Set when this token is used to refresh (rotated away) or explicitly revoked (logout, password change/reset). Null means still active.</summary>
        public DateTime? RevokedAtUtc { get; set; }

        /// <summary>When rotated, the hash of the token that replaced this one — lets a reuse of this now-revoked token be recognized as suspicious rather than a harmless retry.</summary>
        public string? ReplacedByTokenHash { get; set; }

        public bool IsActive => RevokedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;
    }
}
