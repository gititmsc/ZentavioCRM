using System.ComponentModel.DataAnnotations;

namespace ZentavioCRM.Core.DTOs.Auth
{
    public class RefreshRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = string.Empty;
    }
}
