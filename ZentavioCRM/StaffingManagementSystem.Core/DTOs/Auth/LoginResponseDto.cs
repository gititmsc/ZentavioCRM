namespace ZentavioCRM.Core.DTOs.Auth
{
    /// <summary>
    /// Result returned to the client after a successful login.
    /// </summary>
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        public UserDto User { get; set; } = new();
    }
}
