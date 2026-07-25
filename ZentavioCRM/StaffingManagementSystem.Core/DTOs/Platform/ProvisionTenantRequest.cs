using System.ComponentModel.DataAnnotations;

namespace ZentavioCRM.Core.DTOs.Platform
{
    public class ProvisionTenantRequest
    {
        [Required(ErrorMessage = "Company name is required.")]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>Lowercase letters, digits and hyphens only, e.g. "acme". Becomes acme.zentaviocrm.com.</summary>
        [Required(ErrorMessage = "Subdomain is required.")]
        [RegularExpression("^[a-z0-9][a-z0-9-]{1,61}[a-z0-9]$", ErrorMessage = "Subdomain must be lowercase letters, digits and hyphens only.")]
        public string Subdomain { get; set; } = string.Empty;

        [Required(ErrorMessage = "Admin first name is required.")]
        [MaxLength(100)]
        public string AdminFirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string AdminLastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Admin email is required.")]
        [EmailAddress]
        public string AdminEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Admin password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string AdminPassword { get; set; } = string.Empty;
    }
}
