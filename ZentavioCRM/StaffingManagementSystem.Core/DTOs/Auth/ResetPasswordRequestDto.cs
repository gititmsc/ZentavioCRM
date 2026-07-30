using System.ComponentModel.DataAnnotations;

namespace ZentavioCRM.Core.DTOs.Auth
{
    public class ResetPasswordRequestDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
