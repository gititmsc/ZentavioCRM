namespace ZentavioCRM.Core.Configuration
{
    /// <summary>
    /// Strongly typed binding of the "Jwt" configuration section.
    /// </summary>
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = string.Empty;

        public string Audience { get; set; } = string.Empty;

        public string SecretKey { get; set; } = string.Empty;

        public int AccessTokenExpiryMinutes { get; set; } = 15;

        public int RefreshTokenExpiryDays { get; set; } = 7;
    }
}
