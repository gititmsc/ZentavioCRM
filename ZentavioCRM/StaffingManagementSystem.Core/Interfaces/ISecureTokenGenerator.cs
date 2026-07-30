namespace ZentavioCRM.Core.Interfaces
{
    /// <summary>
    /// Generates and hashes opaque, high-entropy tokens for things like refresh tokens and
    /// password-reset links. The raw token is handed to the client/emailed once and never stored —
    /// only <see cref="Hash"/> of it is persisted, so a database leak alone can't be used to
    /// impersonate a session or reset a password (same principle as <see cref="IPasswordHasher"/>).
    /// </summary>
    public interface ISecureTokenGenerator
    {
        /// <summary>A new random, URL-safe raw token (not persisted anywhere in this form).</summary>
        string GenerateRawToken();

        /// <summary>Deterministic SHA-256 hash of a raw token, for lookup/comparison against stored hashes.</summary>
        string Hash(string rawToken);
    }
}
