namespace ZentavioCRM.Core.Configuration
{
    /// <summary>
    /// Signing settings for Platform Admin session tokens — deliberately separate from
    /// <see cref="JwtSettings"/> (tenant users' tokens). Using a distinct secret/issuer/audience
    /// means a platform token can never be accepted where a tenant token is expected, or vice
    /// versa, even if a policy were misconfigured — the signature itself wouldn't validate.
    /// </summary>
    public class PlatformJwtSettings
    {
        public const string SectionName = "PlatformJwt";

        public string Issuer { get; set; } = string.Empty;

        public string Audience { get; set; } = string.Empty;

        public string SecretKey { get; set; } = string.Empty;

        public int AccessTokenExpiryMinutes { get; set; } = 480;
    }
}
