namespace ZentavioCRM.Core.Entities.Platform
{
    /// <summary>
    /// A free-text note platform admins leave for each other on a tenant — internal context that
    /// doesn't belong in the audit log (which records actions, not commentary). Append-only, like
    /// <see cref="PlatformAuditLog"/> and <see cref="TenantPayment"/>: no edit/delete for v1, so
    /// the history of who-said-what stays intact.
    /// </summary>
    public class TenantNote
    {
        public Guid Id { get; set; }

        public Guid TenantId { get; set; }

        public Tenant? Tenant { get; set; }

        public string Note { get; set; } = string.Empty;

        /// <summary>Null if the admin account was later deleted.</summary>
        public Guid? CreatedByAdminId { get; set; }

        public PlatformAdmin? CreatedByAdmin { get; set; }

        public DateTime CreatedAtUtc { get; set; }
    }
}
