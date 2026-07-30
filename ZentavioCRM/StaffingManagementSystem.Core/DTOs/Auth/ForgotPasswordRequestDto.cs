using System.ComponentModel.DataAnnotations;

namespace ZentavioCRM.Core.DTOs.Auth
{
    public class ForgotPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
