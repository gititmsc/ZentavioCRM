using System.ComponentModel.DataAnnotations;

namespace ZentavioCRM.Core.DTOs.Users
{
    /// <summary>Admin-initiated password reset for another user — no current-password proof required.</summary>
    public class AdminResetPasswordRequest
    {
        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
