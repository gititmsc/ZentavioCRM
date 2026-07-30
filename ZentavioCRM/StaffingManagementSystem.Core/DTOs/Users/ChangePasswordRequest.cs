using System.ComponentModel.DataAnnotations;

namespace ZentavioCRM.Core.DTOs.Users
{
    /// <summary>Self-service password change — requires proof of the current password.</summary>
    public class ChangePasswordRequest
    {
        [Required]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
