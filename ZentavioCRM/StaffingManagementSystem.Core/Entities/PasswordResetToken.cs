namespace ZentavioCRM.Core.Entities
{
    /// <summary>
    /// A short-lived, single-use token e-mailed to a user who requested a password reset via
    /// "Forgot Password?". Only the SHA-256 hash of the raw token is ever stored, matching
    /// <see cref="RefreshToken"/>'s handling — the raw value exists only in the email link.
    /// </summary>
    public class PasswordResetToken
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public string TokenHash { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        /// <summary>Set once this token has been used to actually reset the password — null means still usable. Checked instead of deleting the row, so a repeat click on the same emailed link gets a clear "already used" outcome rather than a silent no-op.</summary>
        public DateTime? UsedAtUtc { get; set; }

        public bool IsActive => UsedAtUtc is null && ExpiresAtUtc > DateTime.UtcNow;
    }
}
