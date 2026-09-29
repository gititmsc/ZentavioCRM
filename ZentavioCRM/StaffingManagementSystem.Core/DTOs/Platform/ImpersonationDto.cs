using System.ComponentModel.DataAnnotations;

namespace ZentavioCRM.Core.DTOs.Platform
{
    /// <summary>Optional context for the audit log entry — same pattern as
    /// <see cref="SuspendTenantRequest"/>/<see cref="StopTenantRequest"/>.</summary>
    public class ImpersonateTenantRequest
    {
        // Required (unlike Suspend/Stop's optional reason) — this is the single most
        // security-sensitive platform action, so the audit trail should never have to explain an
        // impersonation with no stated reason attached.
        [Required(ErrorMessage = "A reason is required to impersonate a tenant's admin user.")]
        [MaxLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Deliberately carries only an access token — no refresh token. A normal login can silently
    /// extend a session forever via refresh; an impersonation session is capped at whatever
    /// AccessTokenExpiryMinutes is (currently 15 minutes) and then hard-expires, forcing a fresh,
    /// separately-audited impersonate call to continue. That's a deliberate security property of
    /// this DTO's shape, not an oversight.
    /// </summary>
    public class ImpersonateTenantResponseDto
    {
        public string Token { get; set; } = string.Empty;

        public DateTime ExpiresAtUtc { get; set; }

        /// <summary>Needed by the caller to set the X-Tenant header when using this token.</summary>
        public string TenantSubdomain { get; set; } = string.Empty;

        public Guid ImpersonatedUserId { get; set; }

        public string ImpersonatedUserEmail { get; set; } = string.Empty;

        public string ImpersonatedUserFullName { get; set; } = string.Empty;
    }
}
