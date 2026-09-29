namespace ZentavioCRM.Core.Entities.Platform
{
    /// <summary>
    /// A single history entry for a platform-level action (admin login, tenant provisioned,
    /// suspended, reactivated, stopped, impersonated). Separate from the tenant-side
    /// <see cref="AuditLog"/> table — this one lives in the Platform database and is never scoped
    /// to a single tenant's own audit trail. Mirrors AuditLog's "plain-English summary" shape
    /// rather than a field-by-field diff, and its "who did it" via a nullable FK + navigation
    /// property rather than a denormalized name/email string, for the same reasons.
    /// </summary>
    public class PlatformAuditLog
    {
        public Guid Id { get; set; }

        /// <summary>Null if the admin account was deleted after this entry was recorded.</summary>
        public Guid? PlatformAdminId { get; set; }

        public PlatformAdmin? PlatformAdmin { get; set; }

        /// <summary>"Login", "TenantProvisioned", "TenantSuspended", "TenantReactivated", "TenantStopped".</summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>Set for tenant-scoped actions (provision/suspend/reactivate/stop); null for
        /// account-level actions like Login.</summary>
        public Guid? TenantId { get; set; }

        public Tenant? Tenant { get; set; }

        public string Summary { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }
    }
}
