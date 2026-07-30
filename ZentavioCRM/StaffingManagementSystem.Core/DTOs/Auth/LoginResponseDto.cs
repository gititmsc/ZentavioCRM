namespace ZentavioCRM.Core.DTOs.Auth
{
    /// <summary>
    /// Result returned to the client after a successful login.
    /// </summary>
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        /// <summary>Long-lived opaque token used to silently obtain a new access token via POST /api/auth/refresh once this one expires.</summary>
        public string RefreshToken { get; set; } = string.Empty;

        public DateTime RefreshTokenExpiresAtUtc { get; set; }

        public UserDto User { get; set; } = new();
    }
}
