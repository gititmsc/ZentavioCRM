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
    }
}
