using System.ComponentModel.DataAnnotations;

namespace ZentavioCRM.Core.DTOs.Platform
{
    /// <summary>
    /// Public, unauthenticated self-service signup — a new company provisioning its own tenant
    /// without a platform admin doing it for them via POST /api/platform/tenants. Deliberately has
    /// no PlanTier field: every self-service signup lands on Trial, full stop — SignupController
    /// maps this into a normal <see cref="ProvisionTenantRequest"/> with PlanTier forced to Trial
    /// server-side, so there's no client-controlled field that could request a paid tier for free.
    /// </summary>
    public class TenantSignupRequest
    {
        [Required(ErrorMessage = "Company name is required.")]
        [MaxLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        /// <summary>Lowercase letters, digits and hyphens only, e.g. "acme". Becomes acme.zentaviocrm.com.</summary>
        [Required(ErrorMessage = "Subdomain is required.")]
        [RegularExpression("^[a-z0-9][a-z0-9-]{1,61}[a-z0-9]$", ErrorMessage = "Subdomain must be lowercase letters, digits and hyphens only.")]
        public string Subdomain { get; set; } = string.Empty;

        [Required(ErrorMessage = "Your first name is required.")]
        [MaxLength(100)]
        public string AdminFirstName { get; set; } = string.Empty;

        [MaxLength(100)]
        public string AdminLastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Your email is required.")]
        [EmailAddress]
        public string AdminEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "A password is required.")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters.")]
        public string AdminPassword { get; set; } = string.Empty;
    }
}
