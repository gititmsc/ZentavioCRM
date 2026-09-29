namespace ZentavioCRM.Core.Enums
{
    /// <summary>
    /// Subscription tier of a <see cref="Entities.Platform.Tenant"/>. Drives the default usage
    /// limits (<see cref="Configuration.PlanTierDefaults"/>) applied when a tenant is provisioned
    /// or its tier is changed — each Tenant row then stores its own MaxUsers/MaxStorageMB/
    /// MaxRecords copy so a platform admin can override any one of them for a specific tenant
    /// without inventing a whole new tier.
    /// </summary>
    public enum PlanTier
    {
        Trial = 1,
        Starter = 2,
        Professional = 3,
        Enterprise = 4,
    }
}
