namespace ZentavioCRM.Core.Enums
{
    /// <summary>
    /// Lifecycle state of a <see cref="Entities.Platform.Tenant"/> in the Platform database.
    /// </summary>
    public enum TenantStatus
    {
        /// <summary>Database is being created/seeded — not yet safe to resolve traffic to.</summary>
        Provisioning = 1,
        Active = 2,
        Suspended = 3,
        Failed = 4,
        /// <summary>Reversible lock-out: a platform admin stopped this tenant. No data is touched —
        /// the tenant's database still exists untouched, exactly like <see cref="Suspended"/>, just
        /// reached via a different admin action. Can be moved back to <see cref="Active"/> the same
        /// way a Suspended tenant can.</summary>
        Terminated = 5,
    }
}
