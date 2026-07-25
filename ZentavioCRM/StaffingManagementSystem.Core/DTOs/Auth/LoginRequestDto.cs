using System.ComponentModel.DataAnnotations;

namespace ZentavioCRM.Core.DTOs.Auth
{
    /// <summary>
    /// Payload for POST /api/auth/login.
    /// </summary>
    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Enter a valid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
